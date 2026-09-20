# Shared plugin state and node-aware requests

Available in the coordinated Magnetar 2.4.2.1 / cluster 1.1.0 integration.
Ordinary plugins keep their existing configuration and local APIs. Plugins opt in
when data must be shared across processes or an operation needs a particular owner.
Quasar and Agent are not in the data path.

## Consumer API

Call `PluginCluster.ForPlugin(pluginId)` from the plugin assembly. Magnetar binds
that assembly to its manifest ID before construction; namespaces cannot accidentally
collide. This is ownership validation, not a sandbox for untrusted native plugins.

| API | Contract |
| --- | --- |
| `Context`, `ContextChanged` | Physical node, Registry incarnation, role, availability, observed WA generation and owned partition generations. Re-read after availability/ownership notification. |
| `ReadAsync(key)` | Authoritative read; missing/tombstoned records return `NotFound` with a version suitable for CAS. |
| `CompareExchangeAsync(key, expected, schemaVersion, payload, operationId, fence?, deleted?)` | Durable conditional write; `Conflict` returns current data. Optional fence restricts a new mutation to the current owner. |
| `ResolveOwnerAsync(target)` | Resolve an exact physical node/incarnation, current WA, or current partition owner. |
| `RegisterHandler(topic, handler)` | One handler per plugin/topic; dispose registration to remove it. Register once a provider is available. |
| `RequestAsync(target, topic, payload, operationId, timeout, cancellationToken)` | Resolve once and send once. Explicit success, unavailable, fenced, timeout, unsupported, capacity or invalid result. |

The Registry serializes clustered records with its existing state lock and flushes
its WAL before acknowledging writes. Records are plugin-namespaced and survive
Registry restart and node replacement. Standalone uses an exclusively locked local
store with flushed contents and durable atomic replacement. A clustered process
without its runtime provider returns unavailable, never independent standalone data.

Versions contain a store UUID and revision. Deletion keeps a tombstone/revision.
Explicit restore generates a new store UUID and clears replay history; old writes
are fenced even if their numeric revision happens to match. Binary/config rollback
keeps current data. Schema upgrades are plugin-owned CAS writes, with explicit
handling of older/newer payload schemas; no automatic schema downgrade occurs.

Retry an uncertain CAS with the **same operation ID and identical arguments**.
The bounded durable replay ledger returns the original outcome even after ownership
changes, provided the caller is a currently admitted node. A new mutation must pass
current ownership checks. Reusing the ID with different arguments conflicts.
After replay eviction the old expected revision conflicts instead of applying twice.

## Bounds and execution

Values and message payloads are limited to 64 KiB. Each plugin namespace has at most
512 records (including tombstones), 1 MiB of combined current/replay payload, and
32 replay outcomes holding at most 256 KiB. The whole store additionally limits
records to 4096, current/replay payload to 8 MiB, and replay to 128 outcomes / 1 MiB.
One plugin cannot fill the entire shared store. Global limits still bound aggregate
usage across plugins. Existing larger namespaces remain readable and may delete or
shrink records without growing their total payload; no stored records are discarded during
upgrade. Deletions retain revision-bearing tombstones to prevent stale CAS/ABA; quota
exhaustion returns `Capacity` for new keys, while existing keys remain reusable.
Upgrading a legacy store already at the global key ceiling does not reclaim its
keys automatically; retaining them preserves CAS history.
These are small
coordination records, not a bulk world-data store. Names have at most 200 characters;
ordinary spaces in loader identities are supported, while control characters remain forbidden.

Messaging queues/correlation tables are bounded at 128 entries, timeouts at 30 seconds.
There is no automatic retry, reroute, broadcast or exactly-once side-effect promise.
Cancellation/timeout stops waiting; an already dispatched handler may still execute.
Plugins must persist external idempotency at the effect boundary where needed.

Handlers start on the game update thread (at most eight dispatches per frame).
Async continuations are not guaranteed to return to that thread. The runtime validates
the destination against Registry and rechecks local incarnation/ownership immediately
before dispatch. Admission expires at the earlier of the validated lease deadline
and a 250 ms freshness bound, measured monotonically from request start. A stalled
queue fails fenced. Ownership can change after dispatch: use the supplied destination
fence on authoritative CAS writes, and never treat a routed message as a lock over
arbitrary game or external effects.

Standalone creates durable storage only when a plugin calls `PluginCluster.ForPlugin`.
Ordinary launches neither open nor lock `PluginState`. A storage failure or another
process holding its lease logs an error and leaves shared-state services unavailable
until restart; no in-memory replacement silently accepts non-durable writes. Namespace
ownership still requires the calling assembly's exact loader identity. Plugins should
handle `Unavailable` results; handler registration requires an available provider.

Standalone maps logical targets to one local owner and rejects remote physical
targets as unsupported. Provider disposal fences queued work. Consumers should
use results, rather than connectivity snapshots, to decide whether an operation worked.

## Monitoring and packaging

`cluster-plugin-services` publishes per-incarnation availability, pending work,
conflicts, failures, observed ownership generations and ownership changes through PluginStats. Counters are not summed across replicas.
Registry topology remains authoritative; Agent only observes these statistics.
The cluster release capability marker includes `pluginServices: 1` and the exact
PluginSdk SHA-256 used to compile the node/WA plugins. Managed preparation rejects a
different SDK even when its display version is identical.

The runnable [ClusterState example](../../Examples/ClusterState/Plugin.cs) registers
an owner-routed handler and conditionally writes shared state. Build with
`dotnet build Examples/ClusterState/ClusterState.csproj`. Copy `ClusterState.dll` and
`ClusterState.xml` together into `Local/ClusterState` through the common plugin bundle;
do not distribute another PluginSdk.dll. The example's public
`RememberOnWorldAuthorityAsync` method exercises the handler; it does not mutate data
on startup. Run the same plugin standalone to get the local-owner behavior.

Infrastructure transport/lifecycle providers remain documented in [Clustering.md](Clustering.md).
