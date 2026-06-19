# Module: PluginSdk.Stats

**Project:** `PluginSdk` · **Files:** 4 · **Source lines:** 498

## Purpose

Lets a plugin publish self-describing runtime statistics as an in-process publish/subscribe feed. A plugin declares a plain POCO whose properties carry stat attributes (counter, gauge, discrete) plus an optional label; StatsSchema reflects that declaration into a serializable schema and captures live instances into double-valued snapshots; PluginStats then keeps the latest snapshot per provider and notifies subscribers. The schema travels inline in every snapshot, so a consumer interprets and charts the numbers without any compile-time knowledge of the plugin's types.

## Role in Magnetar

The telemetry-producer counterpart of the config system: where PluginConfig and ConfigSchema turn an annotated class into a remotely-rendered editor, a stats POCO plus StatsSchema turn an annotated class into a remotely-rendered chart. The API is public static with no InternalsVisibleTo, so any plugin publishes or consumes directly. There is no in-tree consumer yet; the intended collector is the external Quasar Agent, which subscribes, rolls snapshots up using the per-field aggregation hints, and charts them. This is unrelated to Shared.Votes, which is Magnetar's own launcher-side usage and community-rating telemetry.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `PluginStats` | static class | [`PluginSdk/Stats/PluginStats.cs`](../descriptions/PluginSdk/Stats/PluginStats.cs.md) | Process-wide publish/subscribe hub holding the latest snapshot per provider, with Publish, TryGetSnapshot, Providers, Clear and an Updated event. |
| `StatsSchema` | sealed class | [`PluginSdk/Stats/StatsSchema.cs`](../descriptions/PluginSdk/Stats/StatsSchema.cs.md) | Cached reflection schema for a stats POCO plus the Capture / CaptureGroup routines that project live instances into snapshot value arrays. |
| `StatAttribute` | abstract class | [`PluginSdk/Stats/StatsAttributes.cs`](../descriptions/PluginSdk/Stats/StatsAttributes.cs.md) | Base for the Counter / Gauge / Discrete attributes that mark a numeric POCO property as a published value and set per-kind aggregation defaults. |
| `StatLabelAttribute` | sealed class | [`PluginSdk/Stats/StatsAttributes.cs`](../descriptions/PluginSdk/Stats/StatsAttributes.cs.md) | Marks the single string property whose value labels each captured instance (e.g. a Steam id); at most one per POCO. |
| `StatAggregation` | enum | [`PluginSdk/Stats/StatsAttributes.cs`](../descriptions/PluginSdk/Stats/StatsAttributes.cs.md) | How a value combines across the instances of a group at one instant: None, Sum, Mean, Min, Max. |
| `TimeAggregation` | enum | [`PluginSdk/Stats/StatsAttributes.cs`](../descriptions/PluginSdk/Stats/StatsAttributes.cs.md) | How a consumer folds successive samples of one instance over time: Last, Sum, Mean, Min, Max, Median, Percentiles. |
| `StatsSnapshot` | sealed class | [`PluginSdk/Stats/StatsSnapshot.cs`](../descriptions/PluginSdk/Stats/StatsSnapshot.cs.md) | A timestamped publication from one provider: a UTC timestamp and one or more StatGroups. |
| `StatGroup` | sealed class | [`PluginSdk/Stats/StatsSnapshot.cs`](../descriptions/PluginSdk/Stats/StatsSnapshot.cs.md) | One schema (carried inline) plus the dynamically sized set of instances captured against it, e.g. one row per connected client. |
| `StatInstance` | sealed class | [`PluginSdk/Stats/StatsSnapshot.cs`](../descriptions/PluginSdk/Stats/StatsSnapshot.cs.md) | One captured row: a label and a double[] of values positionally parallel to the schema's fields. |
| `StatsSchemaData` | sealed class | [`PluginSdk/Stats/StatsSchema.cs`](../descriptions/PluginSdk/Stats/StatsSchema.cs.md) | Serializable schema description (name, label description, ordered field definitions) carried inline in every StatGroup. |
| `StatFieldInfo` | sealed class | [`PluginSdk/Stats/StatsSchema.cs`](../descriptions/PluginSdk/Stats/StatsSchema.cs.md) | Metadata for one stat field: name, kind, unit and the two aggregation hints, stored as enum member names for wire stability. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`PluginSdk/Stats/PluginStats.cs`](../descriptions/PluginSdk/Stats/PluginStats.cs.md) | 90 | The process-wide publish/subscribe hub for plugin statistics: a producer publishes a self-describing `StatsSnapshot` under a provider name, and a consumer reads the latest snapshot by name, lists the active providers, or subscribes to `Updated` to receive every publication as it happens. |
| [`PluginSdk/Stats/StatsAttributes.cs`](../descriptions/PluginSdk/Stats/StatsAttributes.cs.md) | 183 | The attribute vocabulary a plugin uses to annotate a stats POCO, plus the two enums that describe how each value aggregates. |
| [`PluginSdk/Stats/StatsSchema.cs`](../descriptions/PluginSdk/Stats/StatsSchema.cs.md) | 172 | Reflection-based schema for a stats POCO, plus the capture routines that project live POCO instances into the serializable `StatInstance` and `StatGroup` shapes. |
| [`PluginSdk/Stats/StatsSnapshot.cs`](../descriptions/PluginSdk/Stats/StatsSnapshot.cs.md) | 53 | The serializable payload a producer publishes through `PluginStats`: a timestamped tree of groups, each pairing a schema with the instances captured against it. |

## Public API surface

- `PluginStats.Publish(string provider, StatsSnapshot snapshot) - stores the latest snapshot for a provider and notifies subscribers`
- `PluginStats.TryGetSnapshot(string provider, out StatsSnapshot) / PluginStats.Providers - pull the latest snapshot, or list the active providers`
- `PluginStats.Updated (event Action<string, StatsSnapshot>) - raised on every publication, each subscriber invoked with fault isolation`
- `PluginStats.Clear(string provider) - drop one provider's snapshot when its plugin is disabled at runtime`
- `StatsSchema.Build(Type pocoType) - cached reflection schema for a stats POCO`
- `StatsSchema.Capture(object) / CaptureGroup(IEnumerable) - project live POCO instances into a StatInstance / StatGroup`
- `[Counter] / [Gauge] / [Discrete] / [StatLabel] - attributes a plugin puts on its stats POCO properties`
- `StatAggregation / TimeAggregation - per-field hints telling a consumer how to aggregate across instances and over time`

## Dependencies

**Uses modules:** [PluginSdk.Logging](PluginSdk.Logging.md)  
**Used by modules:** _none_  
**External systems:** System.Collections.Concurrent; System.Reflection

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
