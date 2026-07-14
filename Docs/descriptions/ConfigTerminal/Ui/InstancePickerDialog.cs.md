# ConfigTerminal/Ui/InstancePickerDialog.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** static class · **Lines:** 96

## Summary
Modal dialog that prompts for the folder pair (and launcher / DS install) identifying an instance — the DS data dir, Magnetar config dir, launcher executable, and DS install — each with a Browse button. Pre-filled with resolved defaults; the DS data dir must exist. Returns the assembled `InstanceBinding`, or null on cancel.

## Types
### InstancePickerDialog — static class, internal
Instance-selection dialog.

- **Methods:**
  - `Show(InstanceBinding seed = null)` — seeds from `InstanceLocator.ResolveDefaults`, builds four path fields with Browse buttons (directory/file pickers), validates that the entered data dir exists, assembles a new binding, resolves its defaults, and returns it (or null on cancel).
  - `Empty(TextField)` — private; trimmed field text or null when blank.
  - `Field(dlg, label, y, value, browse)` — private; adds a labelled `TextField` anchored at column 0 (so long paths show their start) with a Browse button that fills the picked path.

## Cross-references
- **Uses:** Terminal.Gui `Dialog`/`Label`/`TextField`/`Button`/`Application`; NStack `ustring`; `InstanceBinding`/`InstanceLocator` (`ConfigTerminal/Model/`); `FileDialogs`, `Dialogs`, `TurboVisionTheme` (this module); `System.IO.Directory`.
- **Used by:** [Program.cs](../Program.cs.md), [AppShell.cs](AppShell.cs.md)
