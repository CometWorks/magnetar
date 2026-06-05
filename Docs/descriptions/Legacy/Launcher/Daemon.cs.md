# Legacy/Launcher/Daemon.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Launcher` · **Kind:** static class · **Lines:** 160

## Summary
Detaches the running process from its parent (typically Quasar) when the `-daemon` flag is set, so the parent terminating does not take the dedicated server down with it. Called once from [`Program.MagnetarMain`](../Program.cs.md) after `SetupCoreData` (so `LogFile` is ready) and before the heavy startup work, guarded by [`Flags.Daemon`](../../Shared/Flags.cs.md).

On Linux the detach is a `setsid()` — the process leaves the parent's session and process group, so the group-wide SIGHUP/termination delivered when the parent or its controlling terminal goes away no longer reaches it (an explicit `kill -HUP <pid>` still reloads the config via [`ServerControl`](ServerControl.cs.md)). When launched as a child (e.g. Quasar spawning it) this happens *in place*, preserving the PID and inherited stdout/stderr. When the process is itself a process-group leader — e.g. a wrapper script `exec`'d it into the group the shell created — `setsid()` is forbidden (`EPERM`), so it re-execs a fresh child (which is *not* a leader) that detaches successfully, while the parent exits.

## Types
### `Daemon` — static class, internal
- **Constants:** `EPERM = 1`; `ReexecMarker = "MAGNETAR_DAEMON_REEXEC"` (env var set on the re-exec'd child to prevent re-exec recursion).
- **P/Invokes:** `getpid()`, `getsid(int)`, `setsid()`, `_exit(int)` (as `LibcExit`) from `libc` (Linux); `FreeConsole()` from `kernel32.dll` (Windows).
- **Methods:**
  - `Detach()` — platform dispatch. On `NETCOREAPP` + Linux calls `DetachPosix`; otherwise (Windows, including the net48 Legacy build) calls `DetachWindows`. Idempotent — safe to call again after a daemon-mode restart.
  - `DetachPosix()` — if already a session leader (`getsid(0) == getpid()`, e.g. after an earlier re-exec or an `execve` restart that kept `-daemon`) logs and returns. Otherwise calls `setsid()`; on success logs and returns. On failure reads `errno`: if it is `EPERM` and the re-exec marker is not set, calls `ReexecDetached` (the process is a group leader and cannot start a session in place); any other error (or `EPERM` with the marker already set) logs a warning and continues attached.
  - `ReexecDetached()` — spawns a fresh copy of the process via `Process.Start` (`FileName = Environment.ProcessPath`, the current arguments forwarded verbatim, `UseShellExecute = false` so the child inherits stdin/stdout/stderr), with the re-exec marker set in the child's environment. The child is not a process-group leader, so its own `DetachPosix` `setsid()` succeeds. The parent then disposes its log and calls `_exit(0)` so the child is reparented to init and the group/session it was leading dissolves. Logs and returns (staying attached) if `Environment.ProcessPath` is unknown or `Process.Start` throws.
  - `DetachWindows()` — calls `FreeConsole()` to detach from the inherited console so a parent console-close event cannot terminate it. A `false` return means no console was attached (not an error). Does not defeat a parent Job Object set to kill on close — that can only be avoided at process-creation time.

## Cross-references
- **Uses:** `Shared/LogFile.cs` (detach logging); `libc` / `kernel32.dll` via P/Invoke; `System.Diagnostics.Process` (re-exec).
- **Used by:** [Program.cs](../Program.cs.md) (`MagnetarMain`, when `Flags.Daemon`).
