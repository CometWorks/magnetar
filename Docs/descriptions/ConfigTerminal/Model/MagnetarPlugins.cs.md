# ConfigTerminal/Model/MagnetarPlugins.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 399

## Summary
Facade over Magnetar's plugin config for one instance: the active profile (the enabled set) and the dev-folder sources, joined into UI-ready view rows. It enables/disables local DLLs (from the `Local/` folder), dev-folder plugins (Quasar-style: pick a manifest XML and derive folder + filename + folder-name id), and hub/remote catalog plugins (offline, from Magnetar's cached `.bin` blobs), and manages the plugin sources (remote hub / remote plugin / local hub / dev folder). All writes go through `AtomicFile` (backup + atomic replace).

## Types
### LocalDllInfo — sealed class, internal
A DLL discovered in `Local/`: `FileName` (== plugin id, with extension), `FullPath`, `Enabled`.
### DevFolderPlugin — sealed class, internal
A registered dev-folder source joined with state: `Id` (folder name), `Folder`, `DataFile` (manifest hint), `Enabled` (per-profile), `SourceEnabled` (sources.xml flag), `SourceMissing` (folder gone).
### HubPluginView — sealed class, internal
A catalog row joined with enabled state: `Info` (`HubPluginInfo`), `Enabled`, `IsDevFolder`, `DataFile`, `Id`.
### MagnetarPlugins — sealed class, internal
- **Fields:** `ImplicitIds` (infrastructure DLLs excluded from user plugins — `0Harmony.dll`, `Magnetar.Protocol.dll`, `Quasar.Agent.dll`), `configDir`, `writer`, `profile` (`PluginProfileDocument`), `sources` (`PluginSourcesDocument`).
- **Properties:** `LocalDir`, `HubDir` (`Sources/Hubs`), `RemotePluginDir` (`Sources/Plugins`), `DefaultHubLabel` (name of the first-listed hub).
- **Methods:**
  - `Reload()` — reopens the profile and sources documents.
  - `LocalDlls()` / `SetLocalDllEnabled(name, on)` — list `Local/` DLLs (recursive, excluding infrastructure and surfacing missing-but-enabled entries) and toggle a DLL in the profile.
  - `DevFolderPlugins()` / `DevFolderCatalogViews()` — dev-folder sources joined with profile state, and as catalog rows with manifest-read display metadata.
  - `AddDevFolderFromManifest(manifestXmlPath)` — registers a dev-folder source (not enabled) from a picked manifest; returns the folder-name id.
  - `SetDevFolderEnabled(id, dataFile, on)` / `RemoveDevFolder(plugin)` — enable/disable a dev folder in the profile (writing a `LocalFolderConfig`); unregister and disable.
  - `HubCatalogPlugins()` — the browsable catalog from the cached `.bin` blobs joined with `Profile.GitHub`, dropping obsolete/hidden entries and merging duplicates by id.
  - `SetHubPluginEnabled(id, enabled)` — toggle a hub plugin, pulling in catalog-declared dependencies transitively on enable; returns the ids actually touched.
  - Source management: `RemoteHubs`/`RemotePlugins`/`LocalHubs`; `AddRemoteHub`/`AddRemotePlugin`/`AddLocalHub`; `RemoveRemoteHub`/`RemoveRemotePlugin`/`RemoveLocalHub`; `SetRemoteHubEnabled`/`SetRemotePluginEnabled`/`SetLocalHubEnabled`/`SetLocalPluginEnabled` — each saving the sources document when it changed.
  - `HubBinFiles` / `LabelForBin` / `SafeEnumerateBin` / `SafeEnumerate` (private) — locate the `.bin` catalog files and derive their source labels; exception-swallowing directory enumeration.

## Cross-references
- **Uses:** `PluginProfileDocument`/`PluginSourcesDocument`/`PluginManifest`/`HubCatalog`/`HubPluginInfo`/`HubPluginKind`/`LocalPluginSource` and the source records (this module); `AtomicFile` (`ConfigTerminal/Io/`); `System.IO`, `System.Linq`.
- **Used by:** [Diagnostics.cs](../Diagnostics.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [HubPluginsView.cs](../Ui/HubPluginsView.cs.md), [PluginSourcesView.cs](../Ui/PluginSourcesView.cs.md), [PluginsView.cs](../Ui/PluginsView.cs.md), [PluginConfigTests.cs](../../ConfigTerminalTests/PluginConfigTests.cs.md)
