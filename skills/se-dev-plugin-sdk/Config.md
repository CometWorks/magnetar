# Writing a Config Class

A plugin's configuration is a single class that derives from
`PluginSdk.Config.PluginConfig`. It has three contract obligations:

1. A **public parameterless constructor** that produces the default values
   (required by both XML deserialization and JSON envelope generation).
2. One **public, read/write property per option**, annotated with a matching
   `*OptionAttribute` (see [Options.md](Options.md)).
3. Property setters that **call `SetField(ref field, value)`** so change
   notifications fire when the value actually changes.

## The property pattern

```csharp
public class MyConfig : PluginConfig
{
    [IntOption(1, 240, "Ticks per second")]
    public int TickRate { get; set => SetField(ref field, value); } = 60;
}
```

`field` is the C# 14 contextual keyword that refers to the compiler-generated
backing field of the property — no explicit private field is needed. The
property initializer (`= 60;`) sets the default.

`SetField<T>` is provided by the base class. It:

- Compares old vs. new via `EqualityComparer<T>.Default` (skip if unchanged).
- Writes the field.
- Raises `PropertyChanged(propertyName)`. The property name is captured via
  `[CallerMemberName]`, so callers do not pass it.

A `PluginConfig` instance is the source of truth at runtime. Subscribe to
`PropertyChanged` (the standard `INotifyPropertyChanged` event) if the plugin
needs to react when Quasar pushes a new value or the user edits the local XML.

## What "default" means

Defaults are whatever a freshly constructed instance produces. There is no
separate `[DefaultValue(...)]` attribute and there is no setter-side fallback —
use a property initializer to set the desired default.

```csharp
public string ServerName { get; set => SetField(ref field, value); } = "Unnamed";
public List<int> Ports   { get; set => SetField(ref field, value); } = new List<int>();
```

The defaults are read twice by the framework: once when writing XML (to skip
properties whose current value matches the default), and once when building
the JSON envelope (to populate the `defaults` section so Quasar can show
"reset to default" buttons).

## Supported property types

The complete allowed type catalogue:

| Category | Types |
|---|---|
| Scalars | `bool`, `int`, `long`, `float`, `double`, `string` |
| Enums | Any user-defined `enum`. Stored by member name in both XML and JSON. |
| Collections | `List<T>` of any scalar, enum, or user struct; `SerializableDictionary<TKey, TValue>` with `TKey` ∈ {string, int, long} and a scalar `TValue` |
| Compound | A user-defined `struct` whose public fields and properties are scalars, enums, supported collections, or other supported structs |
| VRage values | `Color` (RGBA), `Vector2D`, `Vector3D`, `Vector2I`, `Vector3I`, `Base6Directions.Direction`, `MyPositionAndOrientation` — see [Options.md](Options.md#built-in-vrage-value-types) |

Plain `Dictionary<,>` is **not** supported — `System.Xml.Serialization` cannot
round-trip it. Use `PluginSdk.Tools.SerializableDictionary<TKey, TValue>`,
which derives from `Dictionary<,>` and implements `IXmlSerializable`.

User structs as configuration values are documented inline in
[Options.md](Options.md#struct-options).

## Declaration order and discovery

`ConfigSchema.Build` walks public, read/write properties with a
`ConfigOptionAttribute` in **declaration order** (via reflection). That order
is also the fallback UI order when no layout containers are declared. Group
related options together in source if you care about visual ordering inside a
container.

## What you do *not* write

- `INotifyPropertyChanged` plumbing — `PluginConfig` already implements it.
- XML/JSON serializers — `PluginConfig` is `IXmlSerializable`; `ConfigStorage`
  drives both formats.
- Default-value attributes, validation methods, "reset" methods, or UI hints
  beyond the per-attribute parameters.
- Any code that knows about Quasar. The plugin only knows about its own
  `PluginConfig` instance.
