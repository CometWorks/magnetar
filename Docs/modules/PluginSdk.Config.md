# Module: PluginSdk.Config

**Project:** `PluginSdk` · **Files:** 5 · **Source lines:** 1832

## Purpose

Declarative, attribute-driven configuration system for Magnetar plugins. Plugins derive a config class from PluginConfig, declare one annotated property per option, and the module discovers a UI schema, persists values as sparse XML on disk, and serializes a schema+defaults+values JSON envelope for remote management. It also handles the awkward VRage value types (Color, vectors, Direction, MyPositionAndOrientation) that the standard XmlSerializer/System.Text.Json cannot serialize directly.

## Role in Magnetar

One of the developer-facing pillars of the PluginSdk. It lets plugins expose typed, validated, remotely-manageable settings that the external manager app (Quasar Web UI) renders without knowing the plugin's C# types. Sits alongside the SDK's Commands, Logging and Runtime modules and is consumed by the loader/integration layers that host plugins on both the Legacy (.NET 4.8) and Interim (.NET 10) Dedicated Server.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `PluginConfig` | abstract class | [`PluginSdk/Config/PluginConfig.cs`](../descriptions/PluginSdk/Config/PluginConfig.cs.md) | Base config class implementing INotifyPropertyChanged + IXmlSerializable sparse (non-default-only) XML format and deep value-equality. |
| `ConfigStorage` | static class | [`PluginSdk/Config/ConfigStorage.cs`](../descriptions/PluginSdk/Config/ConfigStorage.cs.md) | SaveXml/LoadXml (atomic temp-file write) and SaveJson/LoadJson (schema+defaults+values envelope) facade. |
| `ConfigSchema` | static class | [`PluginSdk/Config/ConfigSchema.cs`](../descriptions/PluginSdk/Config/ConfigSchema.cs.md) | Reflects a config type into ConfigSchemaData (layout, options, structs, enums) for the Web UI; recursive struct discovery via worklist. |
| `ConfigOptionAttribute` | class | [`PluginSdk/Config/ConfigAttributes.cs`](../descriptions/PluginSdk/Config/ConfigAttributes.cs.md) | Abstract base of the option-attribute family (BoolOption, IntOption, ListOption, EnumOption, ColorOption, etc.) annotating config properties. |
| `LayoutContainerAttribute` | class | [`PluginSdk/Config/ConfigAttributes.cs`](../descriptions/PluginSdk/Config/ConfigAttributes.cs.md) | Abstract base of Section/Tab/Column layout attributes forming an optional UI layout tree on the config class. |
| `ConfigSchemaData` | class | [`PluginSdk/Config/ConfigSchema.cs`](../descriptions/PluginSdk/Config/ConfigSchema.cs.md) | Root schema DTO (Layout, Properties, Structs, Enums) embedded in the JSON envelope. |
| `TypeSerialization` | static class | [`PluginSdk/Config/TypeSerialization.cs`](../descriptions/PluginSdk/Config/TypeSerialization.cs.md) | XML helpers and JsonConverters for VRage value types (Color, vectors, Direction, MyPositionAndOrientation) the default serializers cannot handle. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`PluginSdk/Config/ConfigAttributes.cs`](../descriptions/PluginSdk/Config/ConfigAttributes.cs.md) | 412 | Declares the full attribute vocabulary a plugin uses to annotate a `PluginConfig`-derived class so Magnetar can discover, validate, remotely manage and lay out each configuration option in an external Web UI (rendered by the manager app, e.g. |
| [`PluginSdk/Config/ConfigSchema.cs`](../descriptions/PluginSdk/Config/ConfigSchema.cs.md) | 550 | Reflection-based schema extractor that turns a `PluginConfig`-derived type into a `ConfigSchemaData` document describing its layout tree, options, nested struct definitions and enum definitions. |
| [`PluginSdk/Config/ConfigStorage.cs`](../descriptions/PluginSdk/Config/ConfigStorage.cs.md) | 158 | Save/load facade for `PluginConfig`-derived instances in two formats. **XML** is the local on-disk format: written atomically via a temp file + rename, emitting only non-default values (the sparse format is driven by `PluginConfig`'s `IXmlSerializable` implementation), so missing elements fall back to defaults on load. **JSON** is the remote management wire format — a three-part envelope of `schema` (from `ConfigSchema.Build`), `defaults` (a fresh instance) and `values` (the current config); loading reads only `values` while regenerating schema/defaults on every save. |
| [`PluginSdk/Config/PluginConfig.cs`](../descriptions/PluginSdk/Config/PluginConfig.cs.md) | 299 | Abstract base class for managed plugin configuration. |
| [`PluginSdk/Config/TypeSerialization.cs`](../descriptions/PluginSdk/Config/TypeSerialization.cs.md) | 413 | Bespoke XML read/write helpers and `System.Text.Json` converters for the small set of VRage value types that are first-class configuration values: `Color`, `Vector2D`, `Vector3D`, `Vector2I`, `Vector3I`, `Base6Directions.Direction` and `MyPositionAndOrientation`. |

## Public API surface

- `ConfigStorage.SaveXml<T>(T, string) / LoadXml<T>(string)`
- `ConfigStorage.SaveJson<T>(T) / LoadJson<T>(string)`
- `ConfigSchema.Build(Type) -> ConfigSchemaData`
- `PluginConfig.SetField<T>(ref T, T, [CallerMemberName] string) (protected)`
- `PluginConfig.NotifyChanged(string) and PropertyChanged event`
- `Attribute set: Section/Tab/Column, Bool/Int/Long/Float/Double/String/List/Dict/Struct/Enum/Color/Vector2D/Vector3D/Vector2I/Vector3I/Direction/PositionAndOrientation Option, StructMember/StructCaption/EnumCaption`

## Dependencies

**Uses modules:** _none_  
**Used by modules:** [PluginSdkTests](PluginSdkTests.md)  
**External systems:** SE DS assemblies (VRageMath: Color, Vector2D/3D, Vector2I/3I, Base6Directions.Direction; VRage: MyPositionAndOrientation); System.Text.Json; System.Xml.Serialization

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
