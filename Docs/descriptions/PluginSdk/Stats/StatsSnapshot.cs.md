# PluginSdk/Stats/StatsSnapshot.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Stats` · **Kind:** Serializable DTO classes · **Lines:** 53

## Summary

The serializable payload a producer publishes through `PluginStats`: a timestamped tree of groups, each pairing a schema with the instances captured against it. A `StatsSnapshot` carries a UTC timestamp and one or more `StatGroup`s; each group carries its `StatsSchemaData` inline plus a dynamically sized list of `StatInstance`s (e.g. one per connected client); each instance is a label and a `double[]` of values positionally parallel to the schema's fields. Every value is a `double` — booleans capture as 0/1 and enums as their underlying integer — so a consumer needs only the inline schema to interpret a snapshot it picks up without prior knowledge. The classes are plain mutable POCOs with parameterless collection defaults, shaped for a serializer.

## Types

### `StatInstance` — sealed class, public

One captured row of a schema.

- **Properties:** `Label` (`string`) — the instance label from the POCO's `[StatLabel]` property, or `null` when the schema declares no label; `Values` (`double[]`) — the field values, in the same order as `StatsSchemaData.Fields`

### `StatGroup` — sealed class, public

A schema together with the instances captured for it at one point in time. Carrying the schema inline keeps a snapshot self-describing.

- **Properties:** `Schema` (`StatsSchemaData`) — the schema the instances were captured against; `Instances` (`List<StatInstance>`, defaults to empty) — the captured rows, possibly empty (e.g. no clients connected)

### `StatsSnapshot` — sealed class, public

An immutable point-in-time publication from one provider, published through `PluginStats.Publish`.

- **Properties:** `UtcTimestamp` (`DateTime`) — when the snapshot was captured (UTC); `Groups` (`List<StatGroup>`, defaults to empty) — the groups in this snapshot (e.g. a single-instance server group plus a multi-instance per-client group)

## Cross-references

- **Uses:** `PluginSdk/Stats/StatsSchema.cs` (`StatsSchemaData`, carried inline by each `StatGroup`)
- **Used by:** [PluginStats.cs](PluginStats.cs.md), [StatsSchema.cs](StatsSchema.cs.md)
