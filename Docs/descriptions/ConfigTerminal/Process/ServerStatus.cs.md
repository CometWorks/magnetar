# ConfigTerminal/Process/ServerStatus.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Process` · **Kind:** enum, sealed class · **Lines:** 44

## Summary
Defines the `ServerState` enum and the `ServerStatus` snapshot that carries the managed instance's process state across the module. `ServerStatus` is produced by `PidFileReader.Query()`, consumed by `MagnetarProcess` and `ProcessMonitor`, and formats itself for the UI status line.

## Types
### ServerState — enum, internal
The mutually exclusive process states: `NotRunning`, `Starting`, `Running`, `Stopping`, `StalePidFile` (pid file present but the process is gone), and `Foreign` (pid alive but its identity does not match this instance).

### ServerStatus — sealed class, internal
A snapshot of the managed instance's process state.
- **Properties:**
  - `State` (`ServerState`) — the classified state; defaults to `NotRunning`.
  - `Pid` (int?) — the recorded pid when known.
  - `StartedAt` (DateTime?) — process start time when available.
  - `Detail` (string) — human-readable qualifier (e.g. the stale/foreign explanation).
  - `IsAlive` — true for `Running`, `Starting`, or `Stopping`.
  - `Uptime` (TimeSpan?) — `DateTime.Now - StartedAt` when `StartedAt` is set, else null.
- **Methods:**
  - `ToString()` — status-line rendering: `RUNNING pid N up H:MM:SS`, `STARTING…`, `STOPPING…`, `STALE PID FILE`, `FOREIGN pid N`, or `STOPPED`.
  - `FormatUptime(TimeSpan t)` (static) — formats an uptime as `H:MM:SS` (total hours, then zero-padded minutes and seconds).

## Cross-references
- **Uses:** `System` (`DateTime`, `TimeSpan`).
- **Used by:** [Diagnostics.cs](../Diagnostics.cs.md), [MagnetarProcess.cs](MagnetarProcess.cs.md), [PidFileReader.cs](PidFileReader.cs.md), [ProcessMonitor.cs](ProcessMonitor.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [DashboardView.cs](../Ui/DashboardView.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md)
