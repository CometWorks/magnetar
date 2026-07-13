# ConfigTerminal/Ui/Dialogs.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** static class · **Lines:** 181

## Summary
Shared modal dialog helpers in the Turbo Vision look used across every view: info/error/confirm boxes, "details" dialogs that keep a centered question over a left-aligned detail block (so bullet lists aren't mangled by `MessageBox`'s per-line centering), a destructive confirm defaulting to the safe option, a text prompt with optional validation, and a background-work runner that keeps the UI live.

## Types
### Dialogs — static class, internal
Stateless helper methods over Terminal.Gui `MessageBox`/`Dialog`.

- **Methods:**
  - `Info(title, message)` / `Error(title, message)` — `MessageBox.Query`/`ErrorQuery` with an OK button.
  - `Confirm(title, message, yes = "Yes", no = "No")` — two-button query; true on the first button.
  - `InfoDetails` / `ErrorDetails` / `ConfirmDetails` — centered question over a left-aligned detail block via `QueryDetails`.
  - `ConfirmDestructive(title, question, details, confirmLabel, cancelLabel = "No")` — error-themed confirm with the safe option first (default, focus, and Esc fallback); returns true only on an explicit confirm click.
  - `QueryDetails(title, question, details, error, params buttons)` — public entry to the details modal (default button 0, Esc → last).
  - `QueryDetailsCore(...)` — private core: measures the question/details/buttons to size a `Dialog`, adds a centered question label and a left-aligned details label, wires each button to record its index and stop the dialog, and returns the clicked index (or `escButton` on Esc).
  - `PendingChanges(title)` — three-way Save/Discard/Cancel prompt.
  - `Prompt(title, label, initial = "", width = 60, validate = null)` — single-line text prompt; when `validate` returns a non-null error the dialog stays open, so the caller only ever gets a validated value or null.
  - `RunBackground<T>(work, onDone)` — runs a blocking operation on a `Task`, then marshals `onDone` (or an error dialog on exception) back onto `Application.MainLoop`.

## Cross-references
- **Uses:** Terminal.Gui `MessageBox`/`Dialog`/`Label`/`Button`/`Application`/`Dim`; `TurboVisionTheme` (this module); `System.Threading.Tasks`.
- **Used by:** [Program.cs](../Program.cs.md), [AccessListView.cs](AccessListView.cs.md), [AppShell.cs](AppShell.cs.md), [HelpDialog.cs](HelpDialog.cs.md), [HubPluginsView.cs](HubPluginsView.cs.md), [InstancePickerDialog.cs](InstancePickerDialog.cs.md), [ModListView.cs](ModListView.cs.md), [NewWorldWizard.cs](NewWorldWizard.cs.md), [PasswordDialog.cs](PasswordDialog.cs.md), [PluginSourcesView.cs](PluginSourcesView.cs.md), [PluginsView.cs](PluginsView.cs.md), [ProfilesView.cs](ProfilesView.cs.md), [WorldsView.cs](WorldsView.cs.md)
