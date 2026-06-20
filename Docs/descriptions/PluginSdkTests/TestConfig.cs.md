# PluginSdkTests/TestConfig.cs

**Project:** PluginSdkTests · **Namespace:** `PluginSdk.Tests` · **Kind:** test fixtures · **Lines:** 197

## Summary
Defines the shared fixture types used across all PluginSdkTests test classes. `TestConfig` is a concrete `PluginConfig` subclass that exercises every option type and UI layout container supported by the PluginSdk: scalars (`bool`, `int`, `long`, `float`, `double`, `string`), all list element types, all dict key/value combinations, enum, list-of-enum, struct, list-of-struct, tree-list-of-struct, nested struct, and all VRage built-in value types (`Color` ×2, `Vector2D`, `Vector3D`, `Vector2I`, `Vector3I`, `Base6Directions.Direction`, `MyPositionAndOrientation`). Supporting struct types (`TestStruct`, `TreeNode`, `NestedStruct`) and the `Quality` enum are also declared here and reused across serialisation, schema, and change-notification tests.

## Types

### `Quality` — enum, public

Three-value enum with non-contiguous underlying values (`Low=0`, `Medium=5`, `High=10`) to prove that storage-by-name is independent of numeric values. `Low` and `Medium` carry `[EnumCaption]` overrides for UI display; `High` falls back to its member name.

- **Members:** `Low` (0, caption "Low quality"), `Medium` (5, caption "Medium quality"), `High` (10, no caption override).

---

### `TestStruct` — struct, public

Value type exercising every scalar type that may appear as a `[StructMember]`: `bool Flag`, `int Integer`, `long LongInteger`, `float FloatNumber`, `double DoubleNumber`, `string Text` (property), `Quality Quality`. All seven members carry `[StructMember]`. Used as the element type of `TestConfig.StructList` and as the `Inner` member of `NestedStruct`.

---

### `TreeNode` — struct, public

Value type that models a parent-child relationship via integer IDs, enabling the UI to render a `List<TreeNode>` as a tree widget. `Id` and `ParentId` are `[StructMember]` `int` fields; `Label` is a `[StructMember, StructCaption]` `string` property — the `[StructCaption]` attribute designates it as the display name for each row in the UI. `TestConfig.TreeNodes` uses `[ListOption(TreeParentField = nameof(TreeNode.ParentId))]` to expose the parent-link field to the schema.

---

### `NestedStruct` — struct, public

Struct with nested collection fields: `string Name`, `List<int> Numbers`, `SerializableDictionary<string, double> Map`, and `TestStruct Inner`. All four members carry `[StructMember]`. Exercises the schema-recursion path that must walk inner structs and register them in `ConfigSchema.Structs`, and the deep-equality path in `PluginConfig` that compares nested collections field by field.

---

### `TestConfig` — class, public : `PluginConfig`

Canonical configuration fixture. Class-level attributes declare the UI layout: `[Tab("general", "General")]`, `[Tab("advanced", "Advanced")]`, `[Section("scalars", parent:"general", "Scalar values")]`, `[Section("collections", parent:"advanced", "Collections")]`, `[Column("scalars-left", parent:"scalars", "Left")]`, `[Column("scalars-right", parent:"scalars", "Right")]` (6 containers total). Every property setter delegates to `SetField(ref field, value)` for equality-gated change notification.

- **Properties (scalars):**
  - `Flag` (`bool`) — `[BoolOption]`, parent `scalars-left`.
  - `Integer` (`int`) — `[IntOption(0, 100)]`, parent `scalars-left`.
  - `LongInteger` (`long`) — `[LongOption]`, parent `scalars-left`.
  - `FloatNumber` (`float`) — `[FloatOption]`, parent `scalars-right`.
  - `DoubleNumber` (`double`) — `[DoubleOption]`, parent `scalars-right`.
  - `Text` (`string`, default `""`) — `[StringOption(maxLength:64)]`, parent `scalars-right`.

- **Properties (lists of scalars):**
  - `BoolList`, `IntList`, `LongList`, `FloatList`, `DoubleList`, `StringList` — each `[ListOption]` with the corresponding element type; all default to empty lists.

- **Properties (dicts — all combinations of key type × value type):**
  - `DictStringInt`, `DictStringString`, `DictStringDouble` — `string` keys.
  - `DictIntString`, `DictIntDouble` — `int` keys.
  - `DictLongBool`, `DictLongLong` — `long` keys.
  - All `[DictOption]`, all default to empty `SerializableDictionary<TK,TV>` instances.

- **Properties (enum):**
  - `Quality` (`Quality`, default `Quality.Medium`) — `[EnumOption]`, parent `scalars-right`.
  - `QualityList` (`List<Quality>`) — `[EnumOption]`, default empty list.

- **Properties (struct and list-of-struct):**
  - `StructValue` (`TestStruct`) — `[StructOption]`.
  - `StructList` (`List<TestStruct>`) — `[StructOption]`, default empty list.
  - `TreeNodes` (`List<TreeNode>`) — `[ListOption(TreeParentField = nameof(TreeNode.ParentId))]`, default empty list.
  - `Nested` (`NestedStruct`) — `[StructOption]`, default initialises `Numbers` and `Map` to empty collections.

- **Properties (VRage types):**
  - `SolidColor` (`Color`, default RGB `(10,20,30,255)`) — `[ColorOption(ColorFormat.Rgb)]`; no alpha channel in the UI picker.
  - `TintColor` (`Color`, default RGBA `(40,50,60,128)`) — `[ColorOption(ColorFormat.Rgba)]`; full alpha channel exposed.
  - `UvOffset` (`Vector2D`, default `(0.25, 0.75)`) — `[Vector2DOption]`.
  - `WorldOffset` (`Vector3D`, default `(1.5, 2.5, 3.5)`) — `[Vector3DOption]`.
  - `TileCoord` (`Vector2I`, default `(3, 4)`) — `[Vector2IOption]`.
  - `GridSize` (`Vector3I`, default `(1, 2, 3)`) — `[Vector3IOption]`.
  - `Facing` (`Base6Directions.Direction`, default `Up`) — `[DirectionOption]`.
  - `SpawnPose` (`MyPositionAndOrientation`, default position `(10,20,30)`, forward `Vector3.Forward`, up `Vector3.Up`) — `[PositionAndOrientationOption]`.

## Cross-references
- **Uses:**
  - `PluginSdk/Config/` — `PluginConfig`, `BoolOption`, `IntOption`, `LongOption`, `FloatOption`, `DoubleOption`, `StringOption`, `ListOption`, `DictOption`, `EnumOption`, `StructOption`, `ColorOption`, `ColorFormat`, `Vector2DOption`, `Vector3DOption`, `Vector2IOption`, `Vector3IOption`, `DirectionOption`, `PositionAndOrientationOption`, `Tab`, `Section`, `Column`, `EnumCaption`, `StructMember`, `StructCaption`
  - `PluginSdk/Tools/SerializableDictionary.cs` — `SerializableDictionary<TK,TV>`
  - SE DS / VRage assemblies — `VRageMath.Color`, `VRageMath.Vector2D/3D/2I/3I`, `VRageMath.Base6Directions.Direction`, `VRage.MyPositionAndOrientation`
- **Used by:** [ChangeNotificationTests.cs](ChangeNotificationTests.cs.md), [SchemaTests.cs](SchemaTests.cs.md), [SerializationTests.cs](SerializationTests.cs.md)
  - `PluginSdkTests/ChangeNotificationTests.cs`
  - `PluginSdkTests/SchemaTests.cs`
  - `PluginSdkTests/SerializationTests.cs`
