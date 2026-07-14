# ConfigTerminal/Model/PluginManifest.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 92

## Summary
Reads the display metadata a dev-folder plugin declares in its manifest XML — a `GitHubPlugin` serialized as `PluginData` (namespace `Pulsar.Shared.Data`). It reads by local element name so it is robust to the root type, `xsi:type` and namespaces, and never references `Shared`.

## Types
### PluginManifest — sealed class, internal
- **Fields:** `FriendlyName`, `Author`, `Tooltip`, `Description`.
- **Methods:**
  - `Read(string xmlPath)` (static) — parses the manifest into a `PluginManifest`; returns an all-null manifest on any error or missing file, so bad metadata degrades to just the folder-name id in the UI.
  - `FindInFolder(string folder)` (static) — the first top-level `*.xml` whose root is a `PluginData` document (or declares a name); returns the filename or null. Fallback when the sources.xml hint is gone.
  - `LooksLikeManifest(path)` / `Local(root, name)` (private static) — detect a `PluginData`/named-plugin root; case-tolerant local-name element lookup returning trimmed non-empty text or null.

## Cross-references
- **Uses:** `System.Xml.Linq`, `System.IO`, `System.Linq`.
- **Used by:** [MagnetarPlugins.cs](MagnetarPlugins.cs.md)
