# ConfigTerminal/Model/PluginProfileDocument.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 249

## Summary
`XDocument` wrapper for a Magnetar plugin profile (`Profiles/<key>.xml`, root `Profile`), with `Current.xml` the active set the server loads. It edits only the enabled-set collections — `Local` DLLs and `DevFolder` configs, plus `GitHub` hub plugins — preserving unknown elements (same upsert philosophy as the DS files). Because Magnetar's `Profile.Validate()` requires all four collections (`GitHub`/`DevFolder`/`Local`/`Mods`), the skeleton always writes them; `Mods` is written but no longer edited (the per-world mod list is authoritative).

## Types
### DevFolderEntry — sealed class, internal
One enabled dev-folder plugin: `Id` (source folder name), `DataFile` (manifest filename), `DebugBuild`, `Folder`.
### PluginProfileDocument — sealed class, internal
- **Fields/consts:** `ProfileName` ("Current"), xsi/xsd namespaces, `xml`.
- **Properties:** `FilePath`; `Name` (the `<Name>` element, upsert); `Root`/`List(name)` (private).
- **Methods:**
  - Path helpers (static): `ProfilesDir(dir)`, `PathFor(dir)` (Current.xml), `PathForKey(dir, key)`, `CleanKey(name)` (Magnetar's `Tools.CleanFileName`).
  - `Open(dir)` / `OpenNamed(dir, key)` / `OpenPath` / `CreateSkeleton(name)` (static/private) — load-with-preserve-whitespace or a four-collection skeleton.
  - `CopyCollectionsFrom(source)` — replaces the four plugin collections with clones from another profile.
  - `CollectionsSignature()` — order-independent canonical signature of the four enabled sets (used to tell whether a saved profile matches the active one).
  - Local DLLs: `LocalDlls`, `EnableLocalDll(name)`, `DisableLocalDll(name)`.
  - Dev folders: `DevFolders`, `EnableDevFolder(id, dataFile, debugBuild)` (writes a `LocalFolderConfig`), `DisableDevFolder(id)`.
  - Hub plugins: `GitHubPlugins`, `EnableGitHub(id)` (writes a `GitHubPluginConfig`), `DisableGitHub(id)`.
  - `Save(writer)` / `SaveTo(writer, path)` — atomic write via `XmlOut.ToXmlString`.
  - `IdEq(a, b)` (private static) — trimmed case-insensitive id comparison.

## Cross-references
- **Uses:** `ConfigDocumentBase.ParseBool` (this module); `AtomicFile`/`XmlOut` (`ConfigTerminal/Io/`); `System.Xml.Linq`, `System.IO`, `System.Linq`, `System.Text`.
- **Used by:** [MagnetarPlugins.cs](MagnetarPlugins.cs.md), [ProfileCatalog.cs](ProfileCatalog.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [ProfilesView.cs](../Ui/ProfilesView.cs.md), [PluginConfigTests.cs](../../ConfigTerminalTests/PluginConfigTests.cs.md), [PluginInteropTests.cs](../../ConfigTerminalTests/PluginInteropTests.cs.md), [ProfileCatalogTests.cs](../../ConfigTerminalTests/ProfileCatalogTests.cs.md)
