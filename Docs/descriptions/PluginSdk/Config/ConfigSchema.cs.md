# PluginSdk/Config/ConfigSchema.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Config` · **Kind:** Static reflection extractor + DTO classes · **Lines:** 550

## Summary
Reflection-based schema extractor that turns a `PluginConfig`-derived type into a `ConfigSchemaData` document describing its layout tree, options, nested struct definitions and enum definitions. The schema is embedded in the JSON envelope produced by `ConfigStorage.SaveJson` so an external manager app can render a Web UI without knowing the plugin's C# types. The builder walks the config class's `LayoutContainerAttribute`s and each `[ConfigOption]`-annotated property, and uses a worklist queue to recursively discover and describe any user-defined structs referenced (directly, or as list/dict element/value types, or via nested struct members). Enums are registered once each, listing member names (the storage/wire value) plus UI captions.

## Types

### ConfigSchema — static class, public
The extractor. Maps each `ConfigOptionAttribute` subtype to a schema `Type` string and copies its constraints; discovers and describes structs/enums transitively.
- **Methods:**
  - `ConfigSchemaData Build(Type configType)` — entry point. Validates `configType` derives from `PluginConfig` (else `ArgumentException`/`ArgumentNullException`), copies layout containers, builds a `ConfigPropertyInfo` for each config property, then drains a `Queue<Type>` of pending structs (skipping ones already in `schema.Structs` by name) building a `StructInfo` for each.
  - `ConfigPropertyInfo BuildPropertyInfo(PropertyInfo, Queue<Type>, ConfigSchemaData)` — private. Reads the property's `ConfigOptionAttribute` (returns `null` if absent), then a big `switch` on the concrete attribute fills `Type` plus type-specific fields: numeric ranges (only emitted when not at the sentinel min/max), string `MaxLength`/`Pattern`/`Multiline`, list/dict element & value types (enqueuing structs / registering enums), `[StructOption]` and `[EnumOption]` special-cased for `List<...>` element forms, `Color` (`HasAlpha`), the VRage vectors, `Direction` (also registers the `Base6Directions.Direction` enum so the UI gets the member list), and `MyPositionAndOrientation`. Unknown attributes yield `Type = "unknown"`.
  - `StructInfo BuildStructInfo(Type, Queue<Type>, ConfigSchemaData)` — private. Describes all public instance fields and read/write properties of a struct, and resolves the single optional `[StructCaption]` member.
  - `string ValidateCaptionMember(Type, string memberName, Type memberType, StructMemberAttribute, string existingCaptionMember)` — private. Enforces at-most-one `[StructCaption]` per struct, that it also carries `[StructMember]`, and that it is `string`-typed; throws `InvalidOperationException` otherwise. Returns the member name.
  - `StructMemberInfo DescribeStructMember(string name, Type, string description, Queue<Type>, ConfigSchemaData)` — private. Mirrors `BuildPropertyInfo` for struct members: classifies scalars, `List<T>`, `Dictionary<K,V>` (incl. `SerializableDictionary`), nested structs and enums; `"unknown"` for anything else.
  - `string TypeName(Type)` — private. Maps a CLR type to a schema scalar name (`bool`/`int`/`long`/`float`/`double`/`string`), or `"enum"` (any enum), `"struct"` (non-primitive non-enum value type), else `"unknown"`.
  - `void RegisterEnum(ConfigSchemaData, Type)` — private, idempotent. Adds the enum's members (natural underlying-value order) to `schema.Enums`, each with its name and an `EnumCaptionAttribute`-overridable caption.
  - `Type GetGenericArgument(Type, Type expectedGenericDefinition, int index)` — private. Returns a generic arg of a closed generic, else throws.
  - `(Type Key, Type Value) GetDictionaryArguments(Type)` / `(Type Key, Type Value)? TryGetDictionaryArguments(Type)` — private. Walk the base-type chain to find `Dictionary<,>`'s arguments (so `SerializableDictionary<K,V>` is recognised). The `Try` variant returns `null` when not a dictionary.

### ConfigSchemaData — sealed class, public
Root of the schema document embedded in JSON output.
- **Properties:** `Layout` (`List<LayoutContainerInfo>`); `Properties` (`List<ConfigPropertyInfo>`); `Structs` (`Dictionary<string, StructInfo>` keyed by struct type name); `Enums` (`Dictionary<string, List<EnumValueInfo>>` keyed by enum type name, members in natural order). All initialized to empty collections.

### StructInfo — sealed class, public
Schema of one user-defined struct config value.
- **Properties:** `Members` (`List<StructMemberInfo>`); `CaptionMember` — name of the `[StructCaption]` string member, or `null` (UI uses positional placeholder).

### EnumValueInfo — sealed class, public
One enum member. **Properties:** `Name` — member identifier used in XML/JSON storage and on the wire; `Caption` — UI caption (defaults to `Name`, overridable via `EnumCaptionAttribute`).

### LayoutContainerInfo — sealed class, public
One layout tree node. **Properties:** `Kind`, `Id`, `Parent`, `Caption` (copied from a `LayoutContainerAttribute`).

### ConfigPropertyInfo — sealed class, public
Metadata for one config option; fields not applicable to the option's type are left `null` and omitted from JSON via `JsonIgnoreCondition.WhenWritingNull`.
- **Properties:** `Name`, `Type`, `Description`, `Parent`; numeric `Min`/`Max` (`double?`, covering int/long/float/double); string `MaxLength` (`int?`) / `Pattern` / `Multiline` (`bool?`, `true` = auto-growing multi-line text area); list/dict `MaxCount`, `ElementType`, `ElementStruct`, `ElementEnum`, `KeyType`, `ValueType`, `ValueStruct`, `ValueEnum`, `TreeParentField`; `StructName`; `EnumName` (references `ConfigSchemaData.Enums`); `HasAlpha` (`bool?`) — set on color options, `true` = RGBA editor, `false` = RGB editor (alpha fixed at 255).

### StructMemberInfo — sealed class, public
One member of a struct config value; same null-omitted nested metadata as `ConfigPropertyInfo`.
- **Properties:** `Name`, `Type`, `Description`, plus `ElementType`, `ElementStruct`, `ElementEnum`, `KeyType`, `ValueType`, `ValueStruct`, `ValueEnum`, `StructName`, `EnumName` for nested collections/structs/enums.

## Cross-references
- **Uses:** `PluginSdk/Config/ConfigAttributes.cs` (all option/layout/struct/enum attributes it reflects over); `PluginSdk/Config/PluginConfig.cs` (`PluginConfig.GetConfigProperties`, type-derivation check); SE DS assemblies (`VRageMath.Color/Vector2D/Vector3D/Vector2I/Vector3I/Base6Directions.Direction`, `VRage.MyPositionAndOrientation`); `System.Reflection`.
- **Used by:** [SerializationTests.cs](../../PluginSdkTests/SerializationTests.cs.md), [ConfigStorage.cs](ConfigStorage.cs.md), [SchemaTests.cs](../../PluginSdkTests/SchemaTests.cs.md)
