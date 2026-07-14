# ConfigTerminal/Ui/HelpDialog.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** static class · **Lines:** 25

## Summary
The About/help modal. Holds a fixed help text describing MagnetarConfig, its F-key bindings, the auto-save behavior, the files it edits atomically with `.bak` backups, and how new worlds are created; renders it left-aligned via `Dialogs.QueryDetails` (whose per-line centering would otherwise mangle the bullet lists and key tables).

## Types
### HelpDialog — static class, internal
- **Fields:** `Text` (const string) — the multi-line help body.
- **Methods:** `Show()` — displays the text left-aligned in a "Close" dialog via `Dialogs.QueryDetails`.

## Cross-references
- **Uses:** `Dialogs` (this module).
- **Used by:** [AppShell.cs](AppShell.cs.md)
