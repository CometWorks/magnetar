# ConfigTerminal/Ui/PasswordDialog.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** static class · **Lines:** 57

## Summary
Modal dialog to set or clear the server password. The plaintext is never stored — only the PBKDF2 hash+salt written by `DedicatedConfigDocument.SetPassword`. Two secret fields must match; leaving both empty and pressing Set clears the password.

## Types
### PasswordDialog — static class, internal
Password-editing dialog.

- **Methods:**
  - `Show(DedicatedConfigDocument cfg, AtomicFile writer, Action onSaved)` — shows the current state, two `Secret` text fields, and Set/Cancel buttons; on Set validates the two entries match, calls `cfg.SetPassword` (null clears), saves, invokes `onSaved`, and reports the result; save errors are shown in an error dialog.

## Cross-references
- **Uses:** Terminal.Gui `Dialog`/`Label`/`TextField`/`Button`/`Application`; `DedicatedConfigDocument` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); `Dialogs`, `TurboVisionTheme` (this module).
- **Used by:** [AppShell.cs](AppShell.cs.md)
