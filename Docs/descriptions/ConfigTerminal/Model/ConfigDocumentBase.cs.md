# ConfigTerminal/Model/ConfigDocumentBase.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** abstract class · **Lines:** 99

## Summary
Base for the `XDocument`-backed DS config wrappers, implementing per-element upsert editing: unknown elements, comments and the ordering of untouched elements are preserved so the tool coexists with hand edits, the DS's own saves and newer game versions. Values are read tolerantly, and a field the user never touched stays absent so the DS's own (version-specific) default still applies. Also hosts the tolerant scalar-parsing helpers shared across the model.

## Types
### ConfigDocumentBase — abstract class, internal
XDocument wrapper with option get/set/unset over a scope-resolved parent element.
- **Fields:** `Xml` (protected `XDocument`).
- **Properties:** `FilePath` (string); `ExistsOnDisk` (bool, private setter — flipped true on `Save`).
- **Methods:**
  - `ResolveScopeRoot(OptionScope scope, bool create)` (protected abstract) — resolves the parent element the option's XML element lives under, optionally creating it.
  - `Get(OptionDefinition def)` — raw string value of the element, or the registry `Default` when absent.
  - `IsSet(OptionDefinition def)` — true when the element is physically present.
  - `Set(OptionDefinition def, string value)` — upserts the element, normalizing enum values via `def.NormalizeEnum`; creates the scope root if needed.
  - `Unset(OptionDefinition def)` — removes the element so the DS default applies again; no-op when absent.
  - `GetBool(OptionDefinition def)` — `Get` parsed via `ParseBool`.
  - `ToCanonicalString()` — canonical serialized form (via `XmlOut.ToXmlString`), used for content-based dirty tracking and saving.
  - `Save(AtomicFile writer)` — atomically writes the document and sets `ExistsOnDisk`.
  - `ReplaceXml(XDocument xml)` (protected) — swaps the in-memory document (Revert / external reload).
  - `ParseBool(string)` / `TryParseLong(string, out long)` / `TryParseDouble(string, out double)` (static) — leniency matching the DS/Quasar (`"1"` is true; invariant-culture number parsing).

## Cross-references
- **Uses:** `System.Xml.Linq`, `System.Globalization`; `OptionDefinition`/`OptionScope` (this module); `AtomicFile`/`XmlOut` (`ConfigTerminal/Io/`).
- **Used by:** [DedicatedConfigDocument.cs](DedicatedConfigDocument.cs.md), [EditSession.cs](EditSession.cs.md), [LastSessionFile.cs](LastSessionFile.cs.md), [PluginProfileDocument.cs](PluginProfileDocument.cs.md), [PluginSourcesDocument.cs](PluginSourcesDocument.cs.md), [WorldConfigDocument.cs](WorldConfigDocument.cs.md), [OptionFormView.cs](../Ui/OptionFormView.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md)
