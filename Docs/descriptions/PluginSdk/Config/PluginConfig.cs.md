# PluginSdk/Config/PluginConfig.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Config` · **Kind:** Abstract base class · **Lines:** 298

## Summary
Abstract base class for managed plugin configuration. A plugin derives from it and declares one public read/write property per option, each annotated with the matching attribute from `ConfigAttributes` and using a setter that calls `SetField` to raise `PropertyChanged` (the C# 14 `field` contextual keyword supplies the backing field, defaults via property initializer). The class implements `IXmlSerializable` to provide the sparse "only non-default values" on-disk XML format, and `INotifyPropertyChanged` so the host observes changes. It centralizes the supported property type set (scalars, enums by name, `List<T>`, `SerializableDictionary`, nested user structs, and VRage value types) and the deep value-equality logic used to decide which properties differ from their defaults.

## Types

### PluginConfig — abstract class, public : `INotifyPropertyChanged, IXmlSerializable`
Base for all plugin config classes. Provides change notification, sparse XML (de)serialization keyed by the property's `[ConfigOption]` attribute, and deep value-equality.
- **Fields:**
  - `XmlSerializerCache` (`static readonly ConcurrentDictionary<(Type, string), XmlSerializer>`) — caches per-(type, root element name) `XmlSerializer`s, since constructing one is expensive.
  - `ConfigPropertyCache` (`static readonly ConcurrentDictionary<Type, PropertyInfo[]>`) — caches the discovered config properties per config type.
- **Events:** `PropertyChanged` — raised whenever an option value changes (scalar via `SetField`, or in-place collection/struct mutation via `NotifyChanged`).
- **Properties:** none (subclasses declare the option properties).
- **Methods:**
  - `void OnPropertyChanged(string propertyName)` — protected virtual; invokes `PropertyChanged`.
  - `void NotifyChanged(string propertyName)` — public; raises `PropertyChanged` after the caller mutates a list/dict/struct option in place (such mutations bypass the setter and would otherwise go unobserved by XML save, the JSON envelope and listeners). Mutate nested contents at any depth, then raise one notification for the top-level property.
  - `bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)` — protected; assigns and notifies only when the value differs per `EqualityComparer<T>.Default`; returns whether it changed. Used by option property setters.
  - `XmlSchema IXmlSerializable.GetSchema()` — returns `null` (no schema).
  - `XmlSerializer GetXmlSerializer(Type, string rootName)` — private static; cache-backed serializer factory with an `XmlRootAttribute` naming.
  - `PropertyInfo[] GetConfigProperties(Type)` — internal static; cache-backed discovery of public instance read/write properties carrying a `ConfigOptionAttribute`. Also used by `ConfigSchema`.
  - `void IXmlSerializable.WriteXml(XmlWriter)` — creates a defaults instance; for each config property whose value differs from the default (per `ValuesEqual`) writes it: VRage value types via `TypeSerialization.WriteXml`, everything else via the cached `XmlSerializer` (with empty namespaces to suppress `xmlns:xsi`/`xmlns:xsd`). Equal-to-default properties are skipped — the sparse format.
  - `void IXmlSerializable.ReadXml(XmlReader)` — builds a name→property map, walks child elements, deserializing matched ones (VRage types via `TypeSerialization.ReadXml`, else the cached serializer) and `Skip`ping unknown elements; handles the empty-element case. Unmatched/missing properties keep their constructor defaults.
  - `bool ValuesEqual(object a, object b)` — internal static; deep value equality covering the supported types. Checks `IDictionary` first (since `SerializableDictionary` is a dictionary, not a list), then `IList`, then user structs (field- and property-wise recursion), else falls through to `a.Equals(b)`. Drives the skip-default rule so a populated list/struct matching the default empty list compares equal despite differing references.
  - `bool IsUserStructType(Type)` — private static; true only for a non-generic, non-primitive, non-enum value type that is not a known BCL value type (`decimal`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`) and not handled by `TypeSerialization` (VRage types implement `IEquatable<T>` and have aliased members like `Color.X/R` that would confuse a field walk). Gates the field-by-field path in `ValuesEqual`.

## Cross-references
- **Uses:** `PluginSdk/Config/ConfigAttributes.cs` (`ConfigOptionAttribute` discovery); `PluginSdk/Config/TypeSerialization.cs` (`IsHandled`, `WriteXml`, `ReadXml` for VRage types); `PluginSdk/Tools` (`SerializableDictionary`); `System.Xml.Serialization` (`IXmlSerializable`, `XmlSerializer`), `System.ComponentModel` (`INotifyPropertyChanged`), `System.Reflection`, `System.Collections.Concurrent`.
- **Used by:** [ConfigSchema.cs](ConfigSchema.cs.md), [ConfigStorage.cs](ConfigStorage.cs.md), [SchemaTests.cs](../../PluginSdkTests/SchemaTests.cs.md), [TestConfig.cs](../../PluginSdkTests/TestConfig.cs.md)
