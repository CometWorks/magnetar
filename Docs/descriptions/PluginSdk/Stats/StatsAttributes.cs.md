# PluginSdk/Stats/StatsAttributes.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Stats` · **Kind:** Attribute family + two enums · **Lines:** 183

## Summary

The attribute vocabulary a plugin uses to annotate a stats POCO, plus the two enums that describe how each value aggregates. A numeric property is marked as a published value with one of three concrete kinds — `[Counter]`, `[Gauge]` or `[Discrete]` — each a `StatAttribute` subclass that sets sensible per-kind aggregation defaults a property can override by named argument. One `string` property may carry `[StatLabel]` to label each captured instance. The two aggregation axes are `StatAggregation` (how a value combines across the instances of a group at one instant) and `TimeAggregation` (how a consumer folds successive samples of one instance over time); they are non-nullable enums because attribute arguments cannot be `Nullable<TEnum>`, so the per-kind default set in the constructor is overridden by any named-argument assignment. `StatsSchema` reflects over these to build the wire schema.

## Types

### `StatAggregation` — enum, public

How a value combines across the instances of a group at a single point in time. Members: `None` (not combinable — keep per-instance), `Sum`, `Mean`, `Min`, `Max`.

### `TimeAggregation` — enum, public

How a consumer folds the successive snapshots of one instance into a single number for a reporting bucket. Members: `Last` (keep most recent), `Sum`, `Mean`, `Min`, `Max`, `Median` (50th percentile, for discrete values), `Percentiles` (retain a p50/p90/p99 distribution).

### `StatAttribute` — abstract class, public : `Attribute`

Base for the value-marking attributes (`AttributeUsage(Property, AllowMultiple = false, Inherited = true)`). Mirrors `ConfigOptionAttribute`.

- **Properties:** `Description` — UI text; `Unit` — free-form unit (e.g. `"B/s"`, `"ms"`), `null` when dimensionless; `Parent` — optional UI grouping hint; `AcrossInstances` (`StatAggregation`); `OverTime` (`TimeAggregation`); `Kind` (abstract `string`) — kind discriminator surfaced in the schema
- **Methods:** `StatAttribute(string description)` — protected ctor storing the description

### `CounterAttribute` — sealed class, public : `StatAttribute`

A monotonically increasing event count (e.g. packets sent). Counts add both across instances and over time, so defaults are `Sum` / `Sum`. `Kind => "counter"`. Ctor `(string description = null)`.

### `GaugeAttribute` — sealed class, public : `StatAttribute`

An instantaneous measured quantity (e.g. a byte rate or queue depth). Such quantities sum across instances but average over time, so defaults are `Sum` / `Mean`. `Kind => "gauge"`. Ctor `(string description = null)`.

### `DiscreteAttribute` — sealed class, public : `StatAttribute`

A discrete or categorical value for which averaging is meaningless (e.g. a mode, a connection epoch, a boolean flag). Defaults are `None` / `Median`, where min/max/percentiles also stay meaningful. `Kind => "discrete"`. Ctor `(string description = null)`.

### `StatLabelAttribute` — sealed class, public : `Attribute`

Marks the single `string` property whose value labels each captured instance (e.g. `"server"`, or a client's Steam id), `AttributeUsage(Property, AllowMultiple = false, Inherited = true)`. At most one property per POCO may carry it (enforced by `StatsSchema.Build`); when absent, captured instances have a `null` label.

- **Properties:** `Description` — what the label identifies
- **Methods:** `StatLabelAttribute(string description = null)`

## Cross-references

- **Uses:** `System.Attribute` (the attribute base type and reflection model)
- **Used by:** [StatsSchema.cs](StatsSchema.cs.md)
