# PluginSdk/Config/TypeSerialization.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Config` · **Kind:** Internal static class + nested `JsonConverter`s · **Lines:** 413

## Summary
Bespoke XML read/write helpers and `System.Text.Json` converters for the small set of VRage value types that are first-class configuration values: `Color`, `Vector2D`, `Vector3D`, `Vector2I`, `Vector3I`, `Base6Directions.Direction` and `MyPositionAndOrientation`. These types cannot go straight through `XmlSerializer` — `Color` exposes aliased `X/R`, `Y/G`, `Z/B` properties over its `PackedValue` field, and `MyPositionAndOrientation` has a derived `Orientation` quaternion whose getter is undefined for a zero-initialized instance. `PluginConfig` calls `IsHandled` to route these types here for both XML serialization and deep-equality; `ConfigStorage` registers `JsonConverters` for the JSON wire format. Colors travel as `#RRGGBB[AA]` hex; vectors as space-separated numbers in XML and `{x,y[,z]}` objects in JSON; the pose as Position/Forward/Up.

## Types

### TypeSerialization — static class, internal
Routing/serialization helpers for the supported VRage value types.
- **Fields:** `JsonConverters` (`public static readonly JsonConverter[]`) — the Color, Vector2D/3D, Vector2I/3I and `MyPositionAndOrientation` converters (Direction is left to the global `JsonStringEnumConverter`).
- **Methods:**
  - `bool IsHandled(Type t)` — public; true for the seven supported VRage value types. Used by `PluginConfig` to bypass both the generic `XmlSerializer` path and the deep-struct equality walk (these types implement `IEquatable<T>`).
  - `void WriteXml(XmlWriter writer, string elementName, object value)` — public; writes a wrapping element and delegates the body to `WriteXmlBody`.
  - `void WriteXmlBody(XmlWriter, object)` — private; `switch` on value: Color as hex string, vectors as space-separated numbers, Direction as its name, pose as three child `Position`/`Forward`/`Up` elements. Throws `InvalidOperationException` for any other type.
  - `object ReadXml(XmlReader reader, Type type)` — public; pose is read via `ReadPoseXml`; all other types are single-text-value leaf elements parsed into the corresponding type (Color via `ParseColor`, vectors via `ParseDoubles`/`ParseInts`, Direction via `Enum.Parse` case-sensitive). Throws for unsupported types.
  - `MyPositionAndOrientation ReadPoseXml(XmlReader)` — private; seeds a pose `(Zero, Forward, Up)`, walks `Position`/`Forward`/`Up` child elements (Forward/Up downcast to `Vector3` float), handles the empty-element case, skips unknown nodes.
  - `string FormatColor(Color)` / `Color ParseColor(string)` — private; format as `#RRGGBBAA`; parse `#RRGGBB` (alpha forced to 255) or `#RRGGBBAA`, otherwise `FormatException`.
  - `string FormatDoubles(params double[])` / `string FormatInts(params int[])` — private; join with spaces; doubles use `"G17"` round-trip formatting, invariant culture.
  - `double[] ParseDoubles(string, int expectedCount)` / `int[] ParseInts(string, int expectedCount)` — private; split on whitespace, enforce the expected count (else `FormatException`), parse invariant-culture.

### ColorJsonConverter — sealed class, private : `JsonConverter<Color>`
Reads/writes a `Color` as a `#RRGGBB[AA]` hex string via `ParseColor`/`FormatColor`.

### Vector2DJsonConverter — sealed class, private : `JsonConverter<Vector2D>`
Reads an object accepting `x`/`X`, `y`/`Y` (defaulting missing to 0); writes `{x, y}`.

### Vector3DJsonConverter — sealed class, private : `JsonConverter<Vector3D>`
Reads `x/X`, `y/Y`, `z/Z`; writes `{x, y, z}`.

### Vector2IJsonConverter — sealed class, private : `JsonConverter<Vector2I>`
Integer counterpart of `Vector2DJsonConverter` (reads via `GetInt32`).

### Vector3IJsonConverter — sealed class, private : `JsonConverter<Vector3I>`
Integer counterpart of `Vector3DJsonConverter`.

### PositionAndOrientationJsonConverter — sealed class, private : `JsonConverter<MyPositionAndOrientation>`
Reads an object with `position`/`forward`/`up` (case-insensitive keys), each deserialized as a `Vector3D` (Forward/Up downcast to `Vector3`); defaults missing fields to `(Zero, Forward, Up)`. Writes `position`/`forward`/`up` as `Vector3D` objects (reusing the registered Vector3D converter), deliberately omitting the derived `Orientation` quaternion.

## Cross-references
- **Uses:** SE DS assemblies (`VRageMath.Color/Vector2D/Vector3D/Vector2I/Vector3I/Vector3/Base6Directions.Direction`, `VRage.MyPositionAndOrientation`); `System.Text.Json`, `System.Xml`, `System.Globalization`.
- **Used by:** [ConfigStorage.cs](ConfigStorage.cs.md), [PluginConfig.cs](PluginConfig.cs.md)
