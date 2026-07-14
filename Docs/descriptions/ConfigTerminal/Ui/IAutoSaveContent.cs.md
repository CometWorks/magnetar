# ConfigTerminal/Ui/IAutoSaveContent.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** interface · **Lines:** 23

## Summary
The contract for a hosted panel that persists its edits automatically. The shell drives it on a ~1 s tick, on panel switch (before dispose), and on quit, so there is no explicit Save step. Implemented by `OptionFormView`, `ModListView`, and `AccessListView`.

## Types
### IAutoSaveContent — interface, internal
- **Methods:** `FlushPendingSave()` — persists pending valid changes if dirty, a no-op when clean; must never block or pop a dialog.
- **Properties:** `InvalidFields` (`IReadOnlyList<string>`) — labels of fields currently holding invalid, unsaved values; empty when all valid. Used to warn on leaving the panel.

## Cross-references
- **Uses:** `System.Collections.Generic`.
- **Used by:** [AccessListView.cs](AccessListView.cs.md), [AppShell.cs](AppShell.cs.md), [ModListView.cs](ModListView.cs.md), [OptionFormView.cs](OptionFormView.cs.md)
