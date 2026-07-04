# Plugin Statistics

`PluginSdk.Stats` lets a plugin **publish runtime statistics** — counters,
gauges and discrete values declared on a plain annotated class — as
**self-describing snapshots**. A consumer (the Quasar Agent, or another plugin)
picks a snapshot up, reads the schema that travels inside it, and rolls the
numbers up or charts them **without knowing the plugin's C# types**.

It is the telemetry sibling of the config system: where `PluginConfig` +
`ConfigSchema` turn an annotated class into a remotely-rendered editor, a stats
POCO + `StatsSchema` turn an annotated class into a remotely-rendered chart.
The whole API is `public static` — no `InternalsVisibleTo`, no registration
call — so a plugin publishes or consumes directly.

> **C# 14 syntax**, same as the rest of this handbook. Stats POCOs are plain
> auto-properties (`{ get; set; }`) — unlike config classes they need no
> `SetField` change-notification, because a snapshot is *captured* on demand
> rather than watched.

## The shape of a snapshot

A published value is always a `double`. Everything else is structure around it:

```
StatsSnapshot               // one publication from one provider
├── UtcTimestamp            // when it was captured (UTC)
└── Groups : List<StatGroup>          // one or more groups
    ├── Schema              // StatsSchemaData — carried INLINE (self-describing)
    │   ├── Name
    │   ├── LabelDescription
    │   └── Fields : List<StatFieldInfo>  // name, kind, unit, aggregation hints — ordered
    └── Instances : List<StatInstance>
        ├── Label           // which row (e.g. a Steam id), or null
        └── Values : double[]   // positionally parallel to Schema.Fields
```

A **group** is one schema plus the dynamically-sized set of rows captured for
it at that instant — e.g. one row per connected client. A snapshot may carry
several groups (say a single-row *server* group beside a multi-row *per-client*
group). Because each group carries its schema inline, a consumer that has never
seen the plugin can still label every column and pick the right aggregation.

## Declaring the stats POCO

Mark each numeric property with one of three kind attributes, and at most one
`string` property with `[StatLabel]`:

```csharp
using PluginSdk.Stats;

// One row per connected client.
public sealed class ClientBandwidth
{
    [StatLabel("Client Steam id")]
    public string SteamId { get; set; }

    [Counter("Packets sent to the client")]
    public long PacketsSent { get; set; }

    [Gauge("Server → client rate", Unit = "B/s")]
    public double DownBytesPerSec { get; set; }

    [Gauge("Client → server rate", Unit = "B/s")]
    public double UpBytesPerSec { get; set; }

    [Discrete("Connection quality bucket (0–4)")]
    public int QualityBucket { get; set; }
}
```

Any property type that converts to `double` is allowed — `bool` captures as
`0`/`1`, an `enum` as its underlying integer (via `Convert.ToDouble`). Pick the
type that reads best in your code; the wire form is numeric either way.

### Stat kinds and their defaults

Each kind sets a sensible pair of aggregation defaults (see
[Aggregation](#aggregation)), which any property can override by named argument.

| Attribute | Across instances | Over time | Use for |
|---|---|---|---|
| `[Counter]` | `Sum` | `Sum` | monotonic event counts (packets, errors) |
| `[Gauge]` | `Sum` | `Mean` | instantaneous rates / levels (B/s, queue depth) |
| `[Discrete]` | `None` | `Median` | modes, epochs, flags — where averaging is meaningless |

All three also accept `Description`, `Unit` and `Parent` (a UI grouping hint):

```csharp
[Gauge("Peak queue depth", OverTime = TimeAggregation.Max)]
public int QueueDepth { get; set; }

[Gauge("Down confidence", AcrossInstances = StatAggregation.Mean)]
public double DownConfidence { get; set; }
```

### Label rules

- At most **one** property per POCO may carry `[StatLabel]`, and it must be of
  type `string` — both enforced by `StatsSchema.Build` (which throws
  `InvalidOperationException` otherwise).
- When no property carries it, every captured instance has a `null` label —
  fine for a single-row group (e.g. server-wide totals).

## Publishing a snapshot

Build the schema once (it is cached per POCO type, so the reflection runs only
on the first call), capture your live instances into a group, and publish under
a provider name:

```csharp
using System;
using PluginSdk.Stats;

var schema = StatsSchema.Build(typeof(ClientBandwidth));   // cached

// Your plugin samples its own state however it likes:
IEnumerable<ClientBandwidth> rows = SampleConnectedClients();

var snapshot = new StatsSnapshot
{
    UtcTimestamp = DateTime.UtcNow,
    Groups = { schema.CaptureGroup(rows) },   // capture an enumerable -> one StatGroup
};

PluginStats.Publish("bandwidth", snapshot);   // replaces any prior "bandwidth" snapshot
```

- `schema.CaptureGroup(instances)` reads each POCO's label + values into a
  `StatGroup` carrying the schema. The sequence may be **empty** (e.g. no
  clients connected) — you still get a group, just with no rows.
- `schema.Capture(poco)` captures a single instance if you are assembling a
  group by hand.
- `Publish` keeps only the **latest** snapshot per provider name. Call it on
  your own cadence (e.g. once a second from your plugin's update loop).

For a multi-group snapshot, capture each group (typically from its own schema)
and add them all to `Groups`.

## Consuming snapshots

A consumer either **pulls** the latest snapshot or **subscribes** to be pushed
each new one:

```csharp
// Pull the latest for one provider:
if (PluginStats.TryGetSnapshot("bandwidth", out var snap))
{
    foreach (var group in snap.Groups)
    {
        var fields = group.Schema.Fields;            // column metadata, ordered
        foreach (var row in group.Instances)
        {
            // row.Label is the StatLabel value; row.Values[i] pairs with fields[i]
            for (int i = 0; i < fields.Count; i++)
                Render(row.Label, fields[i], row.Values[i]);
        }
    }
}

// Or be pushed every publication (synchronous, on the publishing thread):
PluginStats.Updated += (provider, s) => Ingest(provider, s);

// Discover who is publishing:
foreach (var provider in PluginStats.Providers)   // point-in-time copy
    Console.WriteLine(provider);
```

`row.Values` is positionally parallel to `group.Schema.Fields`, so a consumer
walks the two together and needs no compile-time reference to the producing
plugin.

## Aggregation

Each field carries two independent hints telling a consumer how to fold the raw
per-instance, per-snapshot values into a chart. They are advisory metadata — the
SDK never aggregates; it only records the producer's intent.

**`StatAggregation` — across the instances of a group, at one instant:**

| Member | Meaning |
|---|---|
| `None` | Not combinable — keep the per-instance values (e.g. an id, an epoch) |
| `Sum` | Add across instances (per-client rates sum to the server total) |
| `Mean` | Average across instances |
| `Min` / `Max` | Smallest / largest across instances |

**`TimeAggregation` — folding successive snapshots of one instance over time:**

| Member | Meaning |
|---|---|
| `Last` | Keep the most recent sample (a flag, a tick rate, an epoch) |
| `Sum` | Add the samples (an event count per interval) |
| `Mean` | Arithmetic mean over the bucket |
| `Min` / `Max` | Smallest / largest sample in the bucket |
| `Median` | Middle sample — meaningful for discrete values where the mean is not |
| `Percentiles` | Retain a p50/p90/p99 distribution rather than one folded value |

The hints are stored in the schema as enum **member names** (`"Sum"`,
`"Median"`, …), so the wire form is stable against enum renumbering.

## Thread-safety and fault isolation

- The provider store is a `ConcurrentDictionary`, so `Publish`,
  `TryGetSnapshot`, `Providers` and `Clear` are safe to call from any thread
  without external locking.
- `Updated` subscribers are invoked **synchronously on the publishing thread**,
  each inside its own try/catch. A subscriber that throws is logged (through the
  SDK `Logger`, source `"PluginStats"`) and **never** blocks the other
  subscribers or the publisher. Keep handlers quick; offload heavy work.

## Lifecycle

`PluginStats.Clear(provider)` removes a single provider's snapshot — call it
when your plugin is disabled at runtime so a stale snapshot does not linger. It
is idempotent, a null/empty name is a no-op, and it does **not** raise
`Updated`.

## What this is — and is not

- **In-process only.** Magnetar moves self-describing snapshots between a
  producer and a consumer **in the same process**. It does not persist them,
  transport them off-box, or render anything — those belong to the consumer.
- **Untyped on the wire.** Every value is a `double` and the schema travels
  with it; a consumer needs no reference to the plugin assembly.
- **No in-tree consumer yet.** The intended collector is the external **Quasar
  Agent**, which subscribes, rolls snapshots up using the aggregation hints, and
  charts them. One plugin may also consume another's stats directly via the same
  `PluginStats` API.
- **Distinct from `Shared.Votes`.** That is Magnetar's own opt-in
  community-rating/usage telemetry for the launcher; this is the *plugin-facing*
  runtime-metrics API and is unrelated.
