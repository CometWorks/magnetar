# ConfigTerminalTests/PluginConfigTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 254

## Summary
Comprehensive xUnit suite for the plugin/profile/sources model and the `MagnetarPlugins` facade, proving that enabling/disabling plugins is a surgical upsert that never clobbers unmanaged siblings. It exercises `PluginProfileDocument`, `PluginSourcesDocument`, and `MagnetarPlugins` end to end: GitHub/Mods/Local entries survive edits, enable is idempotent and disable removes, local-DLL enumeration excludes infrastructure DLLs, hub-catalog enable/disable round-trips through the profile, and dev-folder registration/enable/removal split cleanly between `sources.xml` and the profile. Each test uses a fresh temp dir (`IDisposable`).

## Types
### PluginConfigTests — class, public, implements `IDisposable`
Seeds hand-written XML skeletons in a temp dir, then drives the model/facade and re-reads both the raw XML and the reopened model.

- **Fields:** `dir` — per-test temp directory.
- **Methods:**
  - `Profile_upsert_preserves_github_and_mods()` — `EnableLocalDll`/`EnableDevFolder` add `<string>`/`<Id>`/`<DataFile>` entries while keeping the pre-existing `<GitHub>` id and `<Mods>` value; reopened `LocalDlls`/`DevFolders` reflect them.
  - `Enable_is_idempotent_and_disable_removes()` — `EnableLocalDll` returns true once then false; `DisableLocalDll` true then false; ends empty.
  - `Sources_add_preserves_hub_and_dedups_by_folder()` — `AddLocalPlugin` keeps the existing `RemoteHub`, writes `<Folder>`/`<Enabled>true`, and dedups by folder path.
  - `Facade_lists_local_dlls_excluding_infrastructure()` — `MagnetarPlugins.LocalDlls()` includes `Essentials.dll` but excludes `0Harmony.dll` and `Quasar.Agent.dll`; `SetLocalDllEnabled` flips the `Enabled` flag.
  - `Profile_github_upsert_preserves_siblings_including_mods()` — `EnableGitHub` is idempotent and preserves existing GitHub/Local/Mods entries (the tool no longer edits `<Mods>` but leaves it untouched); `DisableGitHub` removes it.
  - `Sources_remote_hub_add_toggle_remove_preserves_managed_fields()` — `AddRemoteHub` dedups by repo, `SetRemoteHubEnabled` toggles, and Magnetar-managed `Hash`/`LastCheck` fields survive; reopened model reflects the disable and `RemoveRemoteHub` works.
  - `Facade_hub_catalog_reflects_profile_enabled_and_pulls_dependencies()` — copies the `magnetar-hub.bin` fixture into `Sources/Hubs`, then `SetHubPluginEnabled` on/off is reflected in `HubCatalogPlugins()` `Enabled`.
  - `Facade_registers_dev_folder_from_manifest_without_enabling_it()` — `AddDevFolderFromManifest` uses the folder name (not the manifest `<Id>`) as the id, registers only in `sources.xml` (no profile written), surfaces a `DevFolderCatalogViews` row using the manifest `FriendlyName`; `SetDevFolderEnabled` writes the profile entry and `RemoveDevFolder` unregisters + disables in one shot.

## Cross-references
- **Uses:** `PluginProfileDocument`, `PluginSourcesDocument`, `MagnetarPlugins`, `HubPluginView`, `DevFolderPlugin` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); `Fixtures/magnetar-hub.bin`; xUnit; `System.IO`, `System.Linq`.
- **Used by:** _none within the repository_
