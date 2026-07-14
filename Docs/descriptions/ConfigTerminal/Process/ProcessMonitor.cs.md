# ConfigTerminal/Process/ProcessMonitor.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Process` · **Kind:** sealed class · **Lines:** 39

## Summary
Polls the managed instance's status and raises `Changed` when it moves, so the UI can react to start/stop/foreign transitions. UI-agnostic: the view drives `Poll()` from the Terminal.Gui main-loop timer, keeping every callback on the single UI thread without any threading of its own.

## Types
### ProcessMonitor — sealed class, internal
Thin change-detecting wrapper over a `MagnetarProcess`.
- **Fields:** `process` (`MagnetarProcess`) — the status source.
- **Properties:** `Latest` (`ServerStatus`, private set) — the most recent snapshot; seeded with a default `ServerStatus` at construction.
- **Events:** `Changed` (`Action<ServerStatus>`) — fired on a state or pid transition.
- **Methods:**
  - `Poll()` — calls `process.Query()`, stores the result as `Latest`, and invokes `Changed` only when the `State` or `Pid` differs from the previous snapshot; on no transition it still refreshes `Latest` so derived uptime stays current. Returns the new status.

## Cross-references
- **Uses:** `MagnetarProcess`, `ServerStatus`/`ServerState` (this module); `System.Action`.
- **Used by:** [AppShell.cs](../Ui/AppShell.cs.md)
