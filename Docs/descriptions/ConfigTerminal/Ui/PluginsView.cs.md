# ConfigTerminal/Ui/PluginsView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 203

## Summary
Manages the Magnetar instance's local plugin sources: a left pane of local DLLs from the `Local/` folder (Space toggles enabled) and a right pane of registered dev folders added Quasar-style by picking a manifest XML. The dev-folder box toggles the registration's own `Enabled` flag in `sources.xml`; the per-profile selection is made separately under Hub Plugins, and Magnetar AND-s the two at load time. The last-visited folder is remembered for the next add.

## Types
### PluginsView — sealed class, internal (`Window`)
The Plugins (local & dev folders) panel.

- **Fields:** `plugins` (`MagnetarPlugins`), `settings` (`ToolSettings`), `writer` (`AtomicFile`), `localList`/`devList` (`ListView`), `locals` (`List<LocalDllInfo>`), `devs` (`List<DevFolderPlugin>`).
- **Methods:**
  - `PluginsView(magnetarConfigDir, writer, settings)` — builds the two framed lists (Enter/double-click toggles) and the Toggle DLL / Add Dev Folder / Toggle Enabled / Remove Dev Folder / Refresh buttons; refreshes.
  - `ProcessKey(KeyEvent)` — Space toggles the focused pane's selected item.
  - `Refresh()` — reloads the plugins and repopulates both lists.
  - `FormatLocal(LocalDllInfo)` — `[x]`/`[ ]` box, file name, and a "(file missing)" note.
  - `FormatDevList(List<DevFolderPlugin>)` — id-padded rows with the source `Enabled` box and a "folder missing" flag.
  - `ToggleLocal()` / `ToggleDev()` — flip the selected local DLL's or dev folder's enabled state via `MagnetarPlugins`, refreshing and restoring the selection.
  - `AddDevFolder()` — picks a manifest via `ManifestPicker`, remembers the folder in `settings`, registers it through `AddDevFolderFromManifest`, and explains it is now selectable under Hub Plugins.
  - `RemoveDevFolder()` — confirms and unregisters the selected dev folder (source files on disk untouched).

## Cross-references
- **Uses:** Terminal.Gui `Window`/`FrameView`/`ListView`/`Button`; `MagnetarPlugins`/`LocalDllInfo`/`DevFolderPlugin` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); `ToolSettings` (`ConfigTerminal/State/`); `ManifestPicker`, `Dialogs`, `TurboVisionTheme` (this module); `System.IO`.
- **Used by:** [AppShell.cs](AppShell.cs.md)
