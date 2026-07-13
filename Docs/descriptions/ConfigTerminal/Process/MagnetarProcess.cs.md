# ConfigTerminal/Process/MagnetarProcess.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Process` · **Kind:** sealed class, sealed class · **Lines:** 218

## Summary
Controls the single managed Magnetar DS instance: starts it daemonized, gracefully stops it (SIGTERM → save+quit), reloads live config (SIGHUP), force-kills it, and queries its status via the pid file. It is the imperative counterpart to `PidFileReader` (which only reads state): every mutating operation first calls `Query()` to gate on the current `ServerState`, and platform behaviour diverges sharply — on Linux signals are delivered through a `libc kill` P/Invoke, whereas on Windows graceful Stop and Reload are unavailable (no signal path to a detached process) and only force-kill via `Process.Kill` works. Carries no Terminal.Gui dependency.

## Types
### OpResult — sealed class, internal
Immutable outcome of a process operation, returned by every public mutating method so the UI can report success/failure with a message.
- **Properties:**
  - `Ok` (bool, private set) — whether the operation succeeded.
  - `Message` (string, private set) — human-readable result/error text; defaults to empty.
- **Methods:**
  - `Success(string m = "")` — factory for an `Ok = true` result.
  - `Fail(string m)` — factory for an `Ok = false` result carrying the failure reason.

### MagnetarProcess — sealed class, internal
Lifecycle coordinator for the one bound instance. Constructed from an `InstanceBinding`, it builds a `PidFileReader` from the binding's pid-file path and DS data dir, and every operation re-queries status through it so decisions act on live state rather than cached assumptions.
- **Constants:** `SIGHUP` = 1, `SIGKILL` = 9, `SIGTERM` = 15 — POSIX signal numbers passed to `kill`.
- **P/Invoke:** `kill(int pid, int sig)` — `libc kill` (`SetLastError`), the Linux signal delivery path; failures surface the errno via `Marshal.GetLastWin32Error`.
- **Fields:** `binding` (`InstanceBinding`) — the bound instance's paths; `pidReader` (`PidFileReader`) — status source, built over `binding.PidFilePath` and `binding.DataDir`.
- **Methods:**
  - `Query()` — delegates to `pidReader.Query()`, returning the current `ServerStatus`.
  - `Start(LaunchSpec spec, TimeSpan? readyTimeout = null)` — refuses to start when already `Running`/`Starting`, or when a `Foreign` process holds the pid; validates the spec via `spec.RejectionReason()`; verifies `binding.MagnetarExePath` exists; deletes any stale pid file so the appearance wait is unambiguous. Builds a `ProcessStartInfo` (no shell, redirected+drained stdout/stderr, no window, working dir = launcher's directory) with args from `spec.BuildArgs()`, starts the process, then polls `Query()` every 250 ms until `Running` (success with pid), the child exits early (failure reporting the exit code), or `readyTimeout` (default 60 s) elapses (timeout failure).
  - `Stop(TimeSpan gracePeriod)` — graceful stop; fails if not `Running` or pid missing; **Linux-only** (returns a failure directing the user to force-kill on Windows). Sends `SIGTERM` so Magnetar saves the world and quits, then waits up to `gracePeriod` via `WaitForExit`.
  - `Reload()` — **Linux-only**, running-only; sends `SIGHUP` so Magnetar saves and reloads live-reloadable config without quitting. Returns immediately after the signal is sent (does not wait).
  - `ForceKill(TimeSpan gracePeriod)` — fails if no pid; on Linux sends `SIGKILL`, on Windows calls `Process.GetProcessById(pid).Kill()`. Data since the last save is lost. After the process exits, deletes the leftover pid file (force-kill bypasses the launcher's clean shutdown so it never clears its own pid).
  - `WaitForExit(TimeSpan timeout)` (private) — polls `Query()` every 250 ms, returning true once the state reaches `NotRunning` or `StalePidFile`, else re-checks once after the timeout.
  - `TryDeleteStalePid(ServerStatus current)` (private) — deletes `binding.PidFilePath` only when the passed status is `StalePidFile`; swallows IO errors.
  - `DrainAsync(SysProcess proc)` (static, private) — attaches no-op output/error handlers and begins async reads so the child's console output is discarded and never corrupts the TUI or blocks on a full pipe; best-effort (swallows failures).

## Cross-references
- **Uses:** `PidFileReader`, `ServerStatus`/`ServerState`, `LaunchSpec` (this module); `InstanceBinding` (`ConfigTerminal/Model/`); `PlatformPaths.IsLinux` (`ConfigTerminal/Io/`); `System.Diagnostics.Process`/`ProcessStartInfo`/`Stopwatch`; `libc kill` via P/Invoke; `System.Runtime.InteropServices.Marshal`; `System.IO.File`/`Path`; `System.Threading.Thread`.
- **Used by:** [Diagnostics.cs](../Diagnostics.cs.md), [ProcessMonitor.cs](ProcessMonitor.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md)
