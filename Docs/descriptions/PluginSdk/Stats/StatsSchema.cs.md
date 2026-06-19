# PluginSdk/Stats/StatsSchema.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Stats` · **Kind:** Reflection schema builder + DTO classes · **Lines:** 172

## Summary

Reflection-based schema for a stats POCO, plus the capture routines that project live POCO instances into the serializable `StatInstance` and `StatGroup` shapes. It is the stats counterpart of `ConfigSchema`: a plugin declares a plain class whose properties carry `StatAttribute`s (and optionally one `StatLabelAttribute`), and this type reflects over that declaration to produce a `StatsSchemaData` a consumer can render, and per-snapshot value arrays. The reflected `PropertyInfo`s are cached per POCO type, so the reflection runs once and each publish interval only walks the cached accessors. Capture coerces every annotated value to `double` via `Convert.ToDouble`, so booleans and enums become numbers a consumer can chart.

## Types

### `StatsSchema` — sealed class, public

The reflection schema plus the capture routines. Construction is private; instances come from the cached `Build` factory.

- **Fields (private):** `Cache` (`static ConcurrentDictionary<Type, StatsSchema>`) — per-POCO-type schema cache; `labelProp` (`PropertyInfo`) — the `[StatLabel]` property, or `null`; `valueProps` (`PropertyInfo[]`) — the annotated value properties, parallel to `Data.Fields`
- **Properties:** `Data` (`StatsSchemaData`) — the serializable schema description for a consumer
- **Methods:**
  - `Build(Type pocoType) → StatsSchema` — static entry point; returns the cached schema for `pocoType`, building it once on first use; throws `ArgumentNullException` when `pocoType` is null
  - `BuildUncached(Type pocoType) → StatsSchema` — private; walks the public instance properties, resolves the single optional `[StatLabel]` (throwing `InvalidOperationException` on more than one, or a non-`string` label property), and builds one `StatFieldInfo` per `[Stat]`-annotated property
  - `Capture(object poco) → StatInstance` — captures one POCO instance, reading the label and converting each value property to `double` via `Convert.ToDouble`; throws `ArgumentNullException` when `poco` is null
  - `CaptureGroup(IEnumerable instances) → StatGroup` — captures a (possibly empty) sequence of instances into a `StatGroup` carrying this schema; throws `ArgumentNullException` when `instances` is null

### `StatsSchemaData` — sealed class, public

Serializable description of a schema, carried inline in every `StatGroup` so snapshots are self-describing.

- **Properties:** `Name` (`string`) — the POCO type name; `LabelDescription` (`string`) — description of the instance label, or `null` when the schema declares no label; `Fields` (`List<StatFieldInfo>`, defaults to empty) — the ordered field definitions, positionally parallel to `StatInstance.Values`

### `StatFieldInfo` — sealed class, public

Metadata for one stat field. The two aggregation hints are stored as enum member names (e.g. `"Sum"`, `"Median"`) so the wire form is stable against enum renumbering.

- **Properties:** `Name`, `Kind` (`counter` / `gauge` / `discrete`), `Description`, `Unit`, `Parent`, `AcrossInstances`, `OverTime` — all `string`, copied from the property's `StatAttribute`

## Cross-references

- **Uses:** `PluginSdk/Stats/StatsAttributes.cs` (the `StatAttribute` / `StatLabelAttribute` it reflects over and the aggregation enums it stringifies); `PluginSdk/Stats/StatsSnapshot.cs` (`StatInstance` / `StatGroup`, the capture outputs); `System.Reflection`; `System.Collections` (`IEnumerable`); `System.Collections.Concurrent` (`ConcurrentDictionary`)
- **Used by:** [StatsSnapshot.cs](StatsSnapshot.cs.md)
