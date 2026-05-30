# Option Attributes

Every config property carries exactly one `*OptionAttribute` from
`PluginSdk.Config`. The attribute pairs a property with its value type and
carries the metadata the UI needs (description, range, length, etc.).

All option attributes accept an optional `Description` string (the tooltip /
help text shown next to the field) and an optional `Parent = "<container-id>"`
to attach the option to a layout container — see [Layout.md](Layout.md).

## Scalars

| Attribute | Property type | Extra params |
|---|---|---|
| `[BoolOption]` | `bool` | — |
| `[IntOption(min, max)]` | `int` | inclusive `min`/`max`, both optional |
| `[LongOption(min, max)]` | `long` | inclusive `min`/`max`, both optional |
| `[FloatOption(min, max)]` | `float` | inclusive `min`/`max`, both optional |
| `[DoubleOption(min, max)]` | `double` | inclusive `min`/`max`, both optional |
| `[StringOption(maxLength, pattern)]` | `string` | `maxLength = 0` means unlimited; `pattern` is an optional full-match regex |

Numeric ranges left unspecified are emitted as **absent** in the schema (not
as the type's `MinValue`/`MaxValue` sentinel), so Quasar can render an
unbounded editor.

Examples:

```csharp
[BoolOption("Enable verbose logging")]
public bool Verbose { get; set => SetField(ref field, value); }

[IntOption(1, 240, "Ticks per second")]
public int TickRate { get; set => SetField(ref field, value); } = 60;

[StringOption(maxLength: 64, pattern: @"^[A-Za-z0-9_-]+$", description: "Slug")]
public string Slug { get; set => SetField(ref field, value); } = "";
```

## Lists

`[ListOption(maxCount, description)]` marks a `List<T>` where `T` is a scalar,
an enum, or a user struct. `maxCount = 0` (the default) means unlimited.

```csharp
[ListOption(description: "Whitelisted ports")]
public List<int> Ports { get; set => SetField(ref field, value); } = new List<int>();

[ListOption(maxCount: 100, description: "Per-player overrides")]
public List<PlayerOverride> Overrides { get; set => SetField(ref field, value); }
    = new List<PlayerOverride>();
```

### Tree-shaped lists

If the element type is a struct that carries a parent reference, set
`TreeParentField` to the name of that struct member. Quasar then renders the
list as a tree:

```csharp
public struct Node
{
    [StructMember] public int Id;
    [StructMember] public int ParentId;     // <-- pointed at by TreeParentField
    [StructMember] public string Label { get; set; }
}

[ListOption(description: "Menu tree", TreeParentField = nameof(Node.ParentId))]
public List<Node> Menu { ... }
```

The plugin still gets a flat `List<Node>`; the tree shape is purely a UI hint.

## Dictionaries

`[DictOption(maxCount, description)]` marks a
`SerializableDictionary<TKey, TValue>` (see [Config.md](Config.md#supported-property-types)).

- `TKey`: `string`, `int`, or `long`.
- `TValue`: any scalar, or a user struct (set `TreeParentField` to a struct
  member referencing another entry's key for a tree-rendered dictionary).

```csharp
[DictOption(description: "Per-player quotas")]
public SerializableDictionary<string, int> Quotas { get; set => SetField(ref field, value); }
    = new SerializableDictionary<string, int>();
```

## Struct options

`[StructOption(description)]` marks either a single struct value or a
`List<TStruct>`. The struct's public fields and properties must each carry
`[StructMember(description)]` to be included in the schema and to be visible
to Quasar.

```csharp
public struct Range
{
    [StructMember("Lower bound")]  public int Min;
    [StructMember("Upper bound")]  public int Max;
}

[StructOption(description: "Allowed range")]
public Range Bounds { get; set => SetField(ref field, value); }

[StructOption(description: "Tiered ranges")]
public List<Range> Tiers { get; set => SetField(ref field, value); } = new List<Range>();
```

A struct may also be used as the element type of a `List<T>` via the plain
`[ListOption]`, or as the value type of a dictionary via `[DictOption]` —
`[StructOption]` is only required when the property itself *is* the struct (or
a list of structs) and is convenient for clarity.

### Struct member rules

- `[StructMember]` is **mandatory** on each public field/property you want in
  the schema. Members without it are ignored.
- Property members must be both readable and writable (`{ get; set; }`).
- Member types follow the same catalogue as top-level options: scalars,
  enums, `List<T>`, `SerializableDictionary<,>`, or nested structs.
- A struct may be a `List<T>` element, but **not** a dictionary key.

### Naming struct instances in a list

When a struct is the element of a `List<T>` (flat or tree), the UI needs a
caption to show on each row. Mark exactly one `string` `[StructMember]` with
`[StructCaption]` and the row caption is the live value of that member:

```csharp
public struct PolicyNode
{
    [StructMember] public int Id;
    [StructMember] public int ParentId;
    [StructMember, StructCaption] public string Label { get; set; }   // <-- row caption
}
```

The schema emits `structs[PolicyNode].captionMember = "Label"`; the UI walks
each list element and uses `element.Label` as the displayed name. Without
`[StructCaption]` the UI falls back to a positional placeholder (e.g.
"Item 3"), which is fine for short fixed lists but unhelpful for tree editors.

Validation (enforced by `ConfigSchema.Build`):

- The marked member must also carry `[StructMember]` — the value has to be in
  the wire data for the UI to read it.
- The marked member must be of type `string`.
- At most one `[StructCaption]` per struct.

For composite captions (`"{Name} ({Min}-{Max})"`), give the struct a stored
backing string the plugin keeps up to date in its setters, and mark that with
`[StructCaption]`. There is intentionally no template/format string in the
schema — the UI only ever reads a literal member value.

### Constraints inside structs

There is intentionally no `IntStructMember(min, max)` or similar. Struct
members carry only a description. If you need range-validated values, put the
field on the config class directly with the appropriate option attribute
instead of nesting it in a struct.

## Enum options

`[EnumOption(description)]` marks either a single enum value or a
`List<TEnum>`. Inside a struct, an enum field needs only the usual
`[StructMember]` — `[EnumOption]` is a top-level option attribute.

```csharp
public enum Quality
{
    [EnumCaption("Low quality")]    Low,
    [EnumCaption("Medium quality")] Medium,
    High,                                          // caption falls back to "High"
}

[EnumOption("Render quality", Parent = "scalars-right")]
public Quality Quality { get; set => SetField(ref field, value); } = Quality.Medium;

[EnumOption("Preset rotation")]
public List<Quality> Presets { get; set => SetField(ref field, value); } = new List<Quality>();
```

Key properties:

- **Storage is by member name** in both XML and JSON. Renumbering an enum
  cannot silently change stored values; reordering members is also safe.
- **Schema lists members in natural (underlying-value) order**, each with its
  identifier and a UI caption. The caption defaults to the member name and is
  overridden per member with `[EnumCaption("…")]` (same naming convention as
  the `Caption` property on layout containers).
- A `List<TEnum>` can be declared with either `[EnumOption]` or `[ListOption]`
  — both produce the same schema (`type = "list"`, `elementType = "enum"`,
  `elementEnum = "<TEnum>"`). `[EnumOption]` is preferred for clarity, mirroring
  how `[StructOption]` is preferred over `[ListOption]` for a list of structs.
- Enums work as struct members too — the struct's `[StructMember]` is enough,
  and the schema describes the member as `type = "enum"` with the enum's name.

## Built-in VRage value types

The SDK has first-class support for a small set of VRage value types that
plugins routinely persist. Each attribute marks one property type and only
records UI metadata — the on-disk and on-wire formats are fixed.

| Attribute | Property type | XML format | JSON format |
|---|---|---|---|
| `[ColorOption(ColorFormat.Rgb)]` | `Color` | `#RRGGBB` written, `#RRGGBB`/`#RRGGBBAA` both accepted | `"#RRGGBBAA"` |
| `[ColorOption(ColorFormat.Rgba)]` | `Color` | `#RRGGBBAA` | `"#RRGGBBAA"` |
| `[Vector2DOption]` | `Vector2D` | `"x y"` (G17 doubles) | `{ "x": …, "y": … }` |
| `[Vector3DOption]` | `Vector3D` | `"x y z"` (G17 doubles) | `{ "x": …, "y": …, "z": … }` |
| `[Vector2IOption]` | `Vector2I` | `"x y"` (ints) | `{ "x": …, "y": … }` |
| `[Vector3IOption]` | `Vector3I` | `"x y z"` (ints) | `{ "x": …, "y": …, "z": … }` |
| `[DirectionOption]` | `Base6Directions.Direction` | member name (`Forward`, `Up`, …) | string member name |
| `[PositionAndOrientationOption]` | `MyPositionAndOrientation` | nested `<Position>` / `<Forward>` / `<Up>` | `{ position, forward, up }` |

Notes:

- **Color storage is always RGBA** — `ColorFormat` only selects whether the UI
  exposes the alpha slider. `Rgb` writes the shorter `#RRGGBB` form on disk
  (alpha forced to 255 on load) and emits `hasAlpha: false` in the schema;
  `Rgba` writes the full `#RRGGBBAA` form and emits `hasAlpha: true`.
- **`Direction` is stored by member name**, not its underlying byte value, so
  the VRage enum staying in its current order is not a load-bearing assumption.
  The schema entry also embeds the full member list under
  `enumName = "Direction"` so the UI does not hard-code the six values.
- **`MyPositionAndOrientation` surfaces only `Position`, `Forward` and `Up`**.
  The derived `Orientation` quaternion is never serialized — it would be
  undefined for a default-constructed instance with zero direction vectors.

```csharp
using VRage;
using VRageMath;

[ColorOption(ColorFormat.Rgb,  "HUD accent color")]
public Color HudColor { get; set => SetField(ref field, value); } = Color.Cyan;

[ColorOption(ColorFormat.Rgba, "Trail tint with alpha")]
public Color TrailColor { get; set => SetField(ref field, value); }
    = new Color((byte)255, (byte)128, (byte)0, (byte)200);

[Vector3DOption("World offset applied to all spawn points")]
public Vector3D WorldOffset { get; set => SetField(ref field, value); } = Vector3D.Zero;

[DirectionOption("Default placement direction")]
public Base6Directions.Direction PlaceDirection { get; set => SetField(ref field, value); }
    = Base6Directions.Direction.Forward;

[PositionAndOrientationOption("Default spawn pose")]
public MyPositionAndOrientation SpawnPose { get; set => SetField(ref field, value); }
    = new MyPositionAndOrientation(Vector3D.Zero, Vector3.Forward, Vector3.Up);
```

These types are not allowed as struct members or list/dict element/value types —
use them only as top-level config properties. (If a plugin needs a `List<Color>`
or a struct with a `Vector3D` field, raise it; the current scope intentionally
keeps the type matrix small.)

## Choosing list vs. dict vs. struct

- **Same shape, ordered, identity is positional** → `List<scalar>` or
  `List<struct>`.
- **Lookup by name/id** → `SerializableDictionary<string, …>`.
- **Fixed-arity record with named fields** → struct with `[StructOption]`.
- **Hierarchy a user edits as a tree** → `List<struct>` or
  `SerializableDictionary<,struct>` plus `TreeParentField`.
