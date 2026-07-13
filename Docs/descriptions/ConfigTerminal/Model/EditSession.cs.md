# ConfigTerminal/Model/EditSession.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 190

## Summary
Dirty-tracking plus validation for one open config document. Dirty is decided by content comparison against a snapshot taken at open/save, so editing a value back to its original clears the flag; a cheap `touched` gate lets the ~1s auto-save tick skip the expensive full-document serialization while idle. Save runs validate → backup → atomic write → new snapshot (backup/atomic handled by `AtomicFile`).

## Types
### OptionIssue — sealed class, internal
A validation problem on one option; carries `OptionId`, `Message`, and `IsError` (warnings do not block saving).
### EditSession — sealed class, internal
- **Fields:** `options` (the definitions in scope), `snapshot` (canonical string at open/save), `touched` (cheap dirty gate).
- **Properties:** `Document` (`ConfigDocumentBase`); `IsDirty` — false unless `touched`; when touched, re-serializes and compares to the snapshot, clearing `touched` when equal again.
- **Events:** `DirtyChanged` (Action).
- **Methods:**
  - `NotifyChanged()` — sets `touched` and raises `DirtyChanged` after any mutation.
  - `ChangedFromDefault()` — options whose current value is set and differs from the registry default (for the save summary).
  - `Validate()` — collects blocking errors (via `ErrorFor`) and non-blocking warnings (unknown enum values, experimental-mode toggles) for every set option, then `CrossFieldChecks`.
  - `ErrorFor(OptionDefinition o, string raw)` (static) — blocking error for a candidate value (bad number / out of range) or null; shared with the form's live per-field validation.
  - `RangeError` / `CrossFieldChecks` / `Fmt` (private) — min/max checks; port-collision detection across `ServerPort`/`SteamPort`/`RemoteApiPort` for a `DedicatedConfigDocument`.
  - `HasErrors()` — any blocking issue.
  - `Save(AtomicFile writer)` — writes the document, rebases the snapshot, clears `touched`, raises `DirtyChanged`.
  - `Rebase()` — rebases the snapshot to current content (e.g. "keep mine" on external change).

## Cross-references
- **Uses:** `ConfigDocumentBase`/`DedicatedConfigDocument`/`OptionDefinition`/`OptionKind`/`OptionRegistry` (this module); `AtomicFile` (`ConfigTerminal/Io/`); `System.Linq`, `System.Globalization`.
- **Used by:** [AppShell.cs](../Ui/AppShell.cs.md), [OptionFormView.cs](../Ui/OptionFormView.cs.md), [WorldsView.cs](../Ui/WorldsView.cs.md)
