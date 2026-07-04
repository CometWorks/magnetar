# PluginSdk/Tools/SerializableDictionary.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Tools` · **Kind:** class · **Lines:** 80

## Summary

Provides a generic dictionary that can be round-tripped by `XmlSerializer`, which cannot handle the standard `Dictionary<TKey, TValue>`. Plugin configuration options that map a key to a value (e.g. `string → int` threshold tables, `string → string` name overrides, or `string → struct` per-entry records) use this type as the declared field/property type in their `PluginConfig`-derived class. The serialized XML element is named `Dictionary` and each entry is wrapped in an `Entry` element containing `Key` and `Value` child elements, each delegating to a per-type `XmlSerializer` instance.

## Types

### `SerializableDictionary<TKey, TValue>` — class, public : `Dictionary<TKey, TValue>`, `IXmlSerializable`

Inherits the full `Dictionary<TKey, TValue>` API and adds XML round-trip support via `IXmlSerializable`. The class is marked `[XmlRoot("Dictionary")]`. Allowed key types are `string`, `int`, `long`; allowed value types are the scalar types supported by `PluginConfig`, an enum, or a user-defined struct (documented there). Two `XmlSerializer` instances are created fresh on each `ReadXml`/`WriteXml` call (one for `TKey`, one for `TValue`).

- **Methods:**
  - `SerializableDictionary()` — default constructor; forwards to `Dictionary<TKey, TValue>()`
  - `SerializableDictionary(IDictionary<TKey, TValue> source)` — copy constructor; initializes from an existing dictionary
  - `GetSchema() → XmlSchema` — returns `null` per the `IXmlSerializable` convention (schema is not formally described)
  - `ReadXml(XmlReader reader)` — deserializes the dictionary from XML; reads the opening element, then loops over `Entry` / `Key` / `Value` element triples until the closing element is reached, calling `keySerializer.Deserialize` and `valueSerializer.Deserialize` for each pair and calling `Add(key, value)`; handles the empty-element edge case by checking `reader.IsEmptyElement` before entering the loop
  - `WriteXml(XmlWriter writer)` — serializes all key-value pairs; iterates `this` and for each entry writes `<Entry><Key>…</Key><Value>…</Value></Entry>` using the per-type `XmlSerializer` instances

## Cross-references

- **Uses:** `System.Xml` / `System.Xml.Serialization` (BCL), `PluginSdk/Config/PluginConfig.cs` (documented type-constraint context)
- **Used by:** [ChangeNotificationTests.cs](../../PluginSdkTests/ChangeNotificationTests.cs.md), [SerializationTests.cs](../../PluginSdkTests/SerializationTests.cs.md), [TestConfig.cs](../../PluginSdkTests/TestConfig.cs.md)
