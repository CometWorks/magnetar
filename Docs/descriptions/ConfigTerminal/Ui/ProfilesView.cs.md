# ConfigTerminal/Ui/ProfilesView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 173

## Summary
Manages plugin *profiles* — named presets of the enabled-plugin set stored as `Profiles/<Key>.xml`, with `Current.xml` the active set the server loads. Mirrors Magnetar's in-game profile UI from the terminal: Load copies a preset into the active set; Save As New / Update snapshot the active set into a preset; plus Rename and Delete. The preset whose enabled set matches the active one is marked.

## Types
### ProfilesView — sealed class, internal (`Window`)
The Plugin Profiles panel.

- **Fields:** `catalog` (`ProfileCatalog`), `onActiveChanged` (`Action`), `list` (`ListView`), `activeLabel` (`Label`), `profiles` (`List<ProfileInfo>`).
- **Methods:**
  - `ProfilesView(magnetarConfigDir, writer, onActiveChanged = null)` — builds the framed list (Enter loads) and the Load/Save As New/Update/Rename/Delete buttons plus the active-set label; refreshes.
  - `Refresh()` — repopulates from `catalog.NamedProfiles()`, preserves the selection, and sets the active-set label from `ActiveMatchKey()` (matching profile name or "unsaved").
  - `Format(ProfileInfo)` — `→ ` marker on the profile matching the active set.
  - `Selected()` — the selected `ProfileInfo` or null.
  - `Load()` — confirms, overwrites `Current.xml` from the preset via `catalog.Load`, refreshes, and invokes `onActiveChanged`.
  - `SaveAsNew()` — prompts for a name, calls `SaveCurrentAs`, and on a name clash offers to overwrite via `Update`.
  - `Update()` — overwrites the selected preset with the active set.
  - `Rename()` — prompts for a new name, guards against a clash, and calls `catalog.Rename`.
  - `Delete()` — confirms and deletes the selected preset (active set unaffected).

## Cross-references
- **Uses:** Terminal.Gui `Window`/`FrameView`/`ListView`/`Label`/`Button`; `ProfileCatalog`/`ProfileInfo`/`PluginProfileDocument` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); `Dialogs`, `TurboVisionTheme` (this module).
- **Used by:** [AppShell.cs](AppShell.cs.md)
