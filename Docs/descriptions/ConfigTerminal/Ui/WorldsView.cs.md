# ConfigTerminal/Ui/WorldsView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 246

## Summary
Lists the worlds found under `Saves/` and offers per-world settings and mod editing, activation, creation, and deletion. Activation writes `Saves/LastSession.sbl` and clears the cfg's `IgnoreLastSession`/`LoadWorld` so the DS loads the chosen world next; deletion recursively removes the world folder and cleans up any now-dangling selection that pointed at it.

## Types
### WorldsView — sealed class, internal (`Window`)
The Worlds panel, hosting a formatted `ListView` and a row of action buttons.

- **Fields:** `shell` (`AppShell`), `list` (`ListView`), `worlds` (`List<WorldInfo>`).
- **Methods:**
  - `WorldsView(AppShell)` — builds the aligned header, the list (Enter opens settings), and the Settings/Mods/Activate (F6)/Refresh/New World/Delete buttons; reloads.
  - `ProcessKey(KeyEvent)` — F6 activates the selected world.
  - `Reload()` — reloads the instance via the shell and repopulates the list from `Instance.Worlds`.
  - `Row(...)` / `Format(WorldInfo)` — shared fixed-width column layout for the header and each row (name, last-save time, mod count, config ok/missing, active marker).
  - `Selected` — the currently selected `WorldInfo` or null.
  - `OpenSettings()` — opens an `OptionFormView` over the world's `WorldConfigDocument` (session options), with `editMods` linking to the mod editor, hosted through `shell.ShowWorldContent`.
  - `OpenMods(WorldInfo)` — hosts a `ModListView` for the world.
  - `ActivateWorld()` — validates the world has a checkpoint and a safe folder name, confirms (listing what will be written), writes the `LastSessionFile`, clears `IgnoreLastSession`/`LoadWorld` if set, saves the cfg, and reloads.
  - `DeleteWorld()` — destructive confirm (default Keep), recursively deletes the folder, clears any dangling selection, and reloads.
  - `ClearDanglingSelection(WorldInfo)` — best-effort: deletes `LastSession.sbl` and/or clears the cfg `LoadWorld` when either pointed at the just-deleted world, so the next start doesn't fail on a missing world.
  - `TargetsFolder(LastSessionFile, folderName)` — path-comparer test of whether the sbl's relative or absolute path names the given folder.

## Cross-references
- **Uses:** Terminal.Gui `Window`/`ListView`/`Label`/`Button`; `WorldInfo`/`WorldConfigDocument`/`DedicatedConfigDocument`/`LastSessionFile`/`OptionRegistry`/`EditSession` (`ConfigTerminal/Model/`); `PlatformPaths` (`ConfigTerminal/Io/`); `AppShell`, `OptionFormView`, `ModListView`, `Dialogs` (this module); `System.IO`.
- **Used by:** [AppShell.cs](AppShell.cs.md)
