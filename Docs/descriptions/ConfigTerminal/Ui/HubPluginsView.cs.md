# ConfigTerminal/Ui/HubPluginsView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 196

## Summary
Browses the plugins offered by the instance's configured hub/remote sources — read offline from Magnetar's cached catalogs under `Sources/Hubs` and `Sources/Plugins` — plus the registered dev folders (shown with a "- dev folder" suffix), and enables/disables them in the active profile. Space or Enter toggles; enabling a hub plugin also pulls in its declared dependencies. A filter box narrows the list and a details pane shows the focused plugin's author, id, dependencies, tagline and description.

## Types
### HubPluginsView — sealed class, internal (`Window`)
The Hub Plugins panel.

- **Fields:** `plugins` (`MagnetarPlugins`), `filter` (`TextField`), `list` (`ListView`), `details` (`TextView`), `allItems`/`items` (`List<HubPluginView>`), `defaultHubLabel` (string); const `CachedEmptyMessage` (shown when nothing is cached).
- **Methods:**
  - `HubPluginsView(magnetarConfigDir, writer)` — builds the framed filter+list (34% wide) and details pane, plus Toggle/Refresh buttons and a sources hint; refreshes.
  - `ProcessKey(KeyEvent)` — Space toggles when the list has focus.
  - `Refresh()` — reloads, records `DefaultHubLabel`, and merges `HubCatalogPlugins()` with `DevFolderCatalogViews()` ordered by friendly name.
  - `ApplyFilter()` / `Matches(v, q)` / `Contains(s, q)` — narrow to rows matching name/id/author/tagline, preserving the selection; shows the empty-catalog or no-match message.
  - `Format(HubPluginView)` — `[x]`/`[ ]` box plus friendly name, a "- dev folder" or "(mod)" marker, and the source label only when it isn't the default hub.
  - `ShowDetails()` — builds the details text (name, author, repo, id, dependency ids, tooltip, description).
  - `Toggle()` — toggles the selected plugin: dev folders via `SetDevFolderEnabled`, hub plugins via `SetHubPluginEnabled` (reporting how many dependency plugins were also enabled); errors are shown in a dialog.

## Cross-references
- **Uses:** Terminal.Gui `Window`/`FrameView`/`TextField`/`ListView`/`TextView`/`Button`/`Label`; `MagnetarPlugins`/`HubPluginView`/`HubPluginInfo`/`HubPluginKind` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); `Dialogs`, `TurboVisionTheme` (this module).
- **Used by:** [AppShell.cs](AppShell.cs.md)
