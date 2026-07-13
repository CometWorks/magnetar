# ConfigTerminal/State/ToolSettings.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.State` · **Kind:** sealed class · **Lines:** 59

## Summary
The TUI tool's own per-instance settings, persisted as a small `ConfigTerminal.xml` next to Magnetar's `config.xml` in the selected config dir so per-instance state travels with the instance. Load and save are both fully tolerant — a missing, partial, or corrupt file falls back to defaults, and neither operation ever throws.

## Types
### ToolSettings — sealed class, internal
Holds the tool's remembered per-instance UI state and reads/writes it as XML.

- **Fields:**
  - `FileName` (const `"ConfigTerminal.xml"`) — the settings file name.
  - `filePath` (readonly string) — the resolved full path the instance loads from / saves to.
- **Properties:**
  - `LastPluginFolder` — directory the plugin-manifest picker should reopen at next (remembered across sessions).
- **Methods:**
  - `Load(string magnetarConfigDir)` (static) — combines the config dir (or `"."` when null) with `FileName`, and if the file exists parses the root's `<LastPluginFolder>` element into the property; any exception is swallowed and defaults are used.
  - `Save(AtomicFile writer)` — builds an XML document with a `<ConfigTerminal>` root, emitting `<LastPluginFolder>` only when non-empty, serialises via `XmlOut.ToXmlString`, and writes it through the supplied `AtomicFile`; failures are swallowed so losing tool settings never breaks the triggering operation.
  - `ToolSettings(string filePath)` (private ctor) — stores the resolved path.

## Cross-references
- **Uses:** `AtomicFile`/`XmlOut` (`ConfigTerminal/Io/`), `System.Xml.Linq` (`XDocument`/`XElement`/`XDeclaration`), `System.IO.Path`/`File`.
- **Used by:** [AppShell.cs](../Ui/AppShell.cs.md), [PluginsView.cs](../Ui/PluginsView.cs.md)
