# PluginSdk/Config/ConfigAttributes.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Config` · **Kind:** Attribute family (abstract bases + sealed attributes + one enum) · **Lines:** 412

## Summary
Declares the full attribute vocabulary a plugin uses to annotate a `PluginConfig`-derived class so Magnetar can discover, validate, remotely manage and lay out each configuration option in an external Web UI (rendered by the manager app, e.g. Quasar). Two attribute families exist: *layout container* attributes applied to the config class (`Section`/`Tab`/`Column`) that form an optional layout tree, and *option* attributes applied to public properties (`BoolOption`, `IntOption`, `ListOption`, `EnumOption`, `ColorOption`, etc.) that declare the type and constraints of each option. Helper attributes (`StructMember`, `StructCaption`, `EnumCaption`) annotate user-defined structs and enums used as config values. These are pure metadata; `ConfigSchema` reflects over them to build the wire schema and `PluginConfig`/`ConfigStorage` drive serialization.

## Types

### ConfigAttributes — static class, public
Empty aggregator class that exists only as a documentation anchor / namespace cross-reference hub for the attribute family. Has no members.

### LayoutContainerAttribute — abstract class, public : `Attribute`
Base for the three layout container attributes. Applied to a config class (`AttributeUsage(Class, AllowMultiple = true, Inherited = true)`) to declare one node of the layout tree; nodes link to each other via `Id`/`Parent`.
- **Properties:** `Id` — unique container id within the config class; `Parent` — id of the parent container or `null` for a root node; `Caption` — human-readable UI caption; `Kind` (abstract) — layout kind string (`section`/`tab`/`column`).
- **Methods:** `LayoutContainerAttribute(string id, string parent, string caption)` — protected ctor storing the three values.

### SectionAttribute — sealed class, public : `LayoutContainerAttribute`
Groups options in a captioned group box; sections with the same parent stack vertically. `Kind => "section"`. Ctor `(string id, string parent = null, string caption = null)`.

### TabAttribute — sealed class, public : `LayoutContainerAttribute`
A tab within a tab strip; tabs sharing a parent are mutually exclusive siblings. `Kind => "tab"`. Ctor `(string id, string parent = null, string caption = null)`.

### ColumnAttribute — sealed class, public : `LayoutContainerAttribute`
A vertical column; columns sharing a parent lay out horizontally side-by-side. `Kind => "column"`. Ctor `(string id, string parent = null, string caption = null)`.

### ConfigOptionAttribute — abstract class, public : `Attribute`
Base for every option attribute. Applied to a property (`AttributeUsage(Property, AllowMultiple = false, Inherited = true)`).
- **Properties:** `Description` — UI description text; `Parent` (settable) — id of the layout container the option attaches to, `null` = root of the UI tree.
- **Methods:** `ConfigOptionAttribute(string description)` — protected ctor.

### BoolOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a `bool` option. Ctor `(string description = null)`.

### IntOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a 32-bit `int` option with an inclusive range.
- **Properties:** `Min`, `Max` — bounds; default to `int.MinValue`/`int.MaxValue` (treated as "no bound" by the schema builder).
- **Methods:** `IntOptionAttribute(int min = int.MinValue, int max = int.MaxValue, string description = null)`.

### LongOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a 64-bit `long` option with an inclusive range. Properties `Min`/`Max` default to `long.MinValue`/`long.MaxValue`. Ctor mirrors `IntOptionAttribute`.

### FloatOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a `float` option. Properties `Min`/`Max` default to `float.NegativeInfinity`/`float.PositiveInfinity` (sentinel for "no bound").

### DoubleOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a `double` option. Properties `Min`/`Max` default to `double.NegativeInfinity`/`double.PositiveInfinity`.

### StringOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a `string` option.
- **Properties:** `MaxLength` — max length, `0` = unlimited; `Pattern` — optional regex the value must fully match; `Multiline` — `bool` (settable), `true` makes the UI render an auto-growing multi-line text area instead of a single-line input.
- **Methods:** `StringOptionAttribute(int maxLength = 0, string pattern = null, string description = null)`.

### ListOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a `List<T>` option whose element type is a supported scalar, enum, or struct.
- **Properties:** `MaxCount` — max element count, `0` = unlimited; `TreeParentField` (settable) — name of a struct member on the element type referencing the parent element's id; when set the UI renders the list as a tree.
- **Methods:** `ListOptionAttribute(int maxCount = 0, string description = null)`.

### DictOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a `SerializableDictionary<TKey,TValue>` option (key must be `string`/`int`/`long`; value a supported scalar/struct).
- **Properties:** `MaxCount` — max entry count, `0` = unlimited; `TreeParentField` (settable) — name of a struct member on the value type referencing another entry's key; when set the UI renders the dict as a tree.
- **Methods:** `DictOptionAttribute(int maxCount = 0, string description = null)`.

### StructOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks an option whose type is a user-defined struct, or a `List<Struct>`. Annotate the struct's members with `StructMemberAttribute`. Ctor `(string description = null)`.

### EnumOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks an option whose type is a user-defined `enum` or `List<TEnum>`. The value is stored (XML and JSON) by member name, never the underlying integer, so renumbering does not break configs. Ctor `(string description = null)`.

### ColorFormat — enum, public
Storage/UI form for `ColorOptionAttribute`. Value is always RGBA on disk; this only selects UI alpha exposure. Members: `Rgb` (hide alpha, force 255), `Rgba` (expose alpha).

### ColorOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a `VRageMath.Color` option (always stored as four-byte RGBA).
- **Properties:** `Format` — `ColorFormat` selecting whether the UI shows the alpha control.
- **Methods:** `ColorOptionAttribute(ColorFormat format = ColorFormat.Rgba, string description = null)`.

### Vector2DOptionAttribute / Vector3DOptionAttribute / Vector2IOptionAttribute / Vector3IOptionAttribute — sealed classes, public : `ConfigOptionAttribute`
Mark `VRageMath.Vector2D` / `Vector3D` / `Vector2I` / `Vector3I` options respectively. Each has a single ctor `(string description = null)`.

### DirectionOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a `VRageMath.Base6Directions.Direction` option. Stored by member name (Forward/Backward/Left/Right/Up/Down) so storage is independent of the enum's integer ordering. Ctor `(string description = null)`.

### PositionAndOrientationOptionAttribute — sealed class, public : `ConfigOptionAttribute`
Marks a `VRage.MyPositionAndOrientation` option. The UI exposes Position (Vector3D), Forward and Up (Vector3 float each); the derived `Orientation` quaternion is not surfaced. Ctor `(string description = null)`.

### StructMemberAttribute — sealed class, public : `Attribute`
Marks a public field or property of a struct that is used as a config value (`AttributeUsage(Property | Field, AllowMultiple = false, Inherited = true)`). Carries a UI description; constraint validation for struct members is intentionally metadata-only.
- **Properties:** `Description` — UI description.
- **Methods:** `StructMemberAttribute(string description = null)`.

### EnumCaptionAttribute — sealed class, public : `Attribute`
Applied to an enum field (`AttributeUsage(Field, AllowMultiple = false, Inherited = false)`) to override the UI caption for that member. The member identifier — not the caption — is what is persisted; this attribute is metadata only.
- **Properties:** `Caption` — the override caption.
- **Methods:** `EnumCaptionAttribute(string caption)`.

### StructCaptionAttribute — sealed class, public : `Attribute`
Marks one `StructMember`-annotated `string` member as the source of the row caption when struct instances appear in a `List<Struct>` editor (`AttributeUsage(Property | Field, AllowMultiple = false, Inherited = false)`). At most one per struct; the marked member must also carry `StructMemberAttribute` and be `string`-typed (enforced by `ConfigSchema.ValidateCaptionMember`). When absent, the UI uses a positional placeholder. Has no members.

## Cross-references
- **Uses:** `PluginSdk/Config/PluginConfig.cs` (attributes annotate `PluginConfig` subclasses); `PluginSdk/Tools` (`SerializableDictionary<TKey,TValue>`); SE DS assemblies (`VRageMath.Color/Vector2D/Vector3D/Vector2I/Vector3I/Base6Directions.Direction`, `VRage.MyPositionAndOrientation`); `System.Attribute` reflection.
- **Used by:** [PluginConfig.cs](PluginConfig.cs.md), [ConfigSchema.cs](ConfigSchema.cs.md), [TestConfig.cs](../../PluginSdkTests/TestConfig.cs.md)
