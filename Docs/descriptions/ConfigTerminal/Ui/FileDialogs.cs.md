# ConfigTerminal/Ui/FileDialogs.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** static class · **Lines:** 166

## Summary
Filesystem browse dialogs shared by the instance picker (path fields) and the dev-folder manifest picker. Wraps Terminal.Gui's `OpenDialog` for directory and file selection, seeds it sensibly at the current value, and — via reflection against the pinned 1.19.0 internals — quiets the dialog's overly-broad `FileSystemWatcher` that otherwise makes the selection jump in actively-built folders.

## Types
### FileDialogs — static class, internal
Directory/file browse helpers.

- **Methods:**
  - `PickDirectory(title, prompt, initial)` — an `OpenDialog` restricted to directories; returns the picked path or null on cancel.
  - `PickFile(title, prompt, initial, allowedTypes = null)` — an `OpenDialog` restricted to files, optionally filtered by extension.
  - `Run(dlg, initial)` — private; seeds, quiets the watcher, runs, and returns the first non-empty picked path or null.
  - `Seed(dlg, initial)` — private; when the seed exists opens its parent with the seed pre-selected (so Open is active immediately); falls back to the folder itself, its parent, or the user's home.
  - `QuietDirectoryWatcher(dlg)` — reflects into the internal `DirListView` to disable its `watcher` and chains onto the public `DirectoryChanged` hook to re-silence the fresh watcher `Reload()` creates on each folder change; best-effort with a try/catch fallback to stock behavior.
  - `FindView(root, typeName)` — private recursive search of the view tree by runtime type name.

## Cross-references
- **Uses:** Terminal.Gui `OpenDialog`/`FileDialog`/`View`/`Application`; NStack `ustring`; `TurboVisionTheme` (this module); `System.IO` (`FileSystemWatcher`, `Path`, `Directory`, `File`); `System.Reflection`.
- **Used by:** [InstancePickerDialog.cs](InstancePickerDialog.cs.md), [ManifestPicker.cs](ManifestPicker.cs.md)
