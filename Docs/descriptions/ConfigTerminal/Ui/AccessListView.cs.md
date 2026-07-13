# ConfigTerminal/Ui/AccessListView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 151

## Summary
Editors for the dedicated config's Administrators / Banned / Reserved SteamID lists plus the GroupID field, laid out as three add/delete columns and one text field. Implements `IAutoSaveContent`, saving change-only via a canonical-string snapshot with a `touched` gate; SteamIDs are numeric-enforced at add, and an invalid GroupID is shown red and kept out of the document.

## Types
### AccessListView — sealed class, internal (`Window`, `IAutoSaveContent`)
The Access Lists panel.

- **Fields:** `cfg` (`DedicatedConfigDocument`), `writer` (`AtomicFile`), `onSaved` (`Action`); the three working buffers `admins`/`banned`/`reserved` and their `ListView`s; `groupId` (`TextField`); `snapshot` (string), `touched` (bool), `groupIdInvalid` (bool).
- **Methods:**
  - `AccessListView(cfg, writer, onSaved)` — builds the three columns, the GroupID field, and the auto-save hint; normalizes the baseline through `CommitToDocument` before snapshotting so merely viewing never looks changed.
  - `ValidateGroupId()` — private; empty means "0", otherwise requires a numeric parse; shows red while invalid and marks `touched`.
  - `MakeColumn(title, x, data, out frame)` — private; builds a framed list with Add (prompts for a numeric SteamID) and Del buttons operating on the working buffer.
  - `CommitToDocument()` — private; pushes the buffers into the cfg and applies GroupID only when valid.
  - `FlushPendingSave()` — skips when untouched; commits, no-ops if the canonical string is unchanged, else saves, invokes `onSaved`, and re-snapshots; swallows save errors for retry.
  - `InvalidFields` — `{"GroupID"}` while invalid, else empty.

## Cross-references
- **Uses:** Terminal.Gui `Window`/`FrameView`/`ListView`/`TextField`/`Label`/`Button`; `DedicatedConfigDocument` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); `Dialogs`, `TurboVisionTheme`, `IAutoSaveContent` (this module).
- **Used by:** [AppShell.cs](AppShell.cs.md)
