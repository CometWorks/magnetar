# PluginSdk/Config/ConfigStorage.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Config` · **Kind:** Static helper class · **Lines:** 158

## Summary
Save/load facade for `PluginConfig`-derived instances in two formats. **XML** is the local on-disk format: written atomically via a temp file + rename, emitting only non-default values (the sparse format is driven by `PluginConfig`'s `IXmlSerializable` implementation), so missing elements fall back to defaults on load. **JSON** is the remote management wire format — a three-part envelope of `schema` (from `ConfigSchema.Build`), `defaults` (a fresh instance) and `values` (the current config); loading reads only `values` while regenerating schema/defaults on every save. Configures a shared `JsonSerializerOptions` that camelCases names, serializes enums by name, and plugs in the VRage value-type converters from `TypeSerialization`.

## Types

### ConfigStorage — static class, public
The save/load facade.
- **Fields:** `JsonOptions` (`static readonly JsonSerializerOptions`) — shared options built once by `BuildJsonOptions`.
- **Methods:**
  - `JsonSerializerOptions BuildJsonOptions()` — private static. Builds options with `CamelCase` property naming, `DictionaryKeyPolicy = null` (dictionary keys kept verbatim), `IncludeFields = true`, `WriteIndented = true`, `DefaultIgnoreCondition = WhenWritingNull`. Adds `JsonStringEnumConverter` (enums and `Base6Directions.Direction` stored by member name, never integer) and every converter in `TypeSerialization.JsonConverters` (Color, vectors, pose).
  - `void SaveXml<T>(T config, string path) where T : PluginConfig` — null-checks args, creates the target directory, serializes with `XmlSerializer` to `path + ".tmp"`, deletes any existing `path`, then `File.Move`s the temp file into place (atomic, crash-safe). Only non-default values are emitted (via `PluginConfig.WriteXml`).
  - `T LoadXml<T>(string path) where T : PluginConfig, new()` — returns `new T()` if the file is absent; otherwise deserializes via `XmlSerializer`. Missing elements leave properties at their defaults.
  - `string SaveJson<T>(T config) where T : PluginConfig` — builds the envelope: serializes the `ConfigSchema.Build(type)` schema, a default-constructed `T` (`defaults`), and `config` (`values`) each to a `JsonElement`, wraps them in `ConfigEnvelope`, and serializes that. Every option is present in `values` even when equal to its default.
  - `T LoadJson<T>(string json) where T : PluginConfig, new()` — parses the document; if it is an object with a `values` property, deserializes that subtree; otherwise falls back to treating the whole document as a flat values-only dump (backward compatibility).

### ConfigEnvelope — sealed class, private (nested in `ConfigStorage`)
DTO for the three-part JSON document. **Properties:** `Schema`, `Defaults`, `Values` — each a `JsonElement`.

## Cross-references
- **Uses:** `PluginSdk/Config/PluginConfig.cs` (the serialized base type and its `IXmlSerializable` sparse format); `PluginSdk/Config/ConfigSchema.cs` (`Build`, `ConfigSchemaData`); `PluginSdk/Config/TypeSerialization.cs` (`JsonConverters`); `System.Text.Json`, `System.Xml.Serialization`, `System.IO`.
- **Used by:** [SchemaTests.cs](../../PluginSdkTests/SchemaTests.cs.md), [SerializationTests.cs](../../PluginSdkTests/SerializationTests.cs.md)
