# ConfigTerminal/Model/PluginSourcesDocument.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 299

## Summary
`XDocument` wrapper for `Sources/sources.xml` (root `SourcesConfig`), the registry of plugin catalog sources. It reads and edits the four source lists — local dev-folder plugins (`LocalPluginSources`), remote GitHub hubs (`RemoteHubSources`), single remote plugins (`RemotePluginSources`) and local folder hubs (`LocalHubSources`) — preserving mod sources and unknown elements via the same upsert philosophy as the DS files.

## Types
### LocalPluginSource / RemoteHubSource / RemotePluginSource / LocalHubSource — sealed classes, internal
Value types for one entry in each source list (`Name`, `Folder`/`Repo`/`Branch`/`File`, `Enabled`, `Trusted` as applicable). `LocalPluginSource.DataFile` is a manifest-filename hint Magnetar strips on its next save.
### PluginSourcesDocument — sealed class, internal
- **Fields/consts:** `ListName` (`LocalPluginSources`), xsi/xsd namespaces, `xml`.
- **Properties:** `FilePath`; `LocalPlugins`, `RemoteHubs`, `RemotePlugins`, `LocalHubs` — projections of each source list; `Root` (private).
- **Methods:**
  - `PathFor(dir)` / `Open(dir)` / `CreateSkeleton()` (static/private) — `Sources/sources.xml`, load-with-preserve-whitespace or an all-lists skeleton (`ShowWarning`/`MaxSourceAge` included).
  - Dev folders: `AddLocalPlugin(name, folder, dataFile, enabled)` (dedup by folder), `RemoveByFolder(folder)`, `SetLocalPluginEnabled(folder, on)`, `FindById(id)`.
  - Remote hubs: `AddRemoteHub`, `RemoveRemoteHub`, `SetRemoteHubEnabled` (dedup by repo; default branch "main").
  - Remote plugins: `AddRemotePlugin`, `RemoveRemotePlugin`, `SetRemotePluginEnabled`.
  - Local hubs: `AddLocalHub`, `RemoveLocalHub`, `SetLocalHubEnabled`.
  - `Save(writer)` — atomic write via `XmlOut.ToXmlString`.
  - `ListEl` / `RemoveByChild` / `SetChildFlag` / `SetChild` / `RepoEq` / `PathEq` (private) — shared list-element ensure/find/upsert helpers and key comparers.

## Cross-references
- **Uses:** `ConfigDocumentBase.ParseBool` (this module); `AtomicFile`/`XmlOut`/`PlatformPaths` (`ConfigTerminal/Io/`); `System.Xml.Linq`, `System.IO`, `System.Linq`.
- **Used by:** [MagnetarPlugins.cs](MagnetarPlugins.cs.md), [PluginSourcesView.cs](../Ui/PluginSourcesView.cs.md), [PluginConfigTests.cs](../../ConfigTerminalTests/PluginConfigTests.cs.md), [PluginInteropTests.cs](../../ConfigTerminalTests/PluginInteropTests.cs.md)
