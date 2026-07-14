# ConfigTerminal/Process/PidFileReader.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Process` · **Kind:** sealed class · **Lines:** 152

## Summary
Reads and verifies the `magnetar.pid` file written by the launcher (spec §2.8), producing a `ServerStatus` snapshot. The file's mere presence is never trusted: the recorded pid must reference a live process *and* that process's identity must match this instance, otherwise the reader reports `StalePidFile` (process gone) or `Foreign` (pid recycled by an unrelated process) rather than a false `Running`. Identity verification is layered and cross-platform — data-dir line match first, then a Linux `/proc/<pid>/cmdline` check, then a Windows process-name fallback.

## Types
### PidFileReader — sealed class, internal
Read-only status source over one pid-file path plus the expected DS data dir it should belong to.
- **Fields:** `pidFilePath` (string) — the `magnetar.pid` path; `expectedDataDir` (string) — the DS data dir this instance bound to, used as the strongest identity signal.
- **Methods:**
  - `Query()` — the sole public entry. Returns `NotRunning` when the file is absent, unreadable, or its first line is not a parseable pid (all failure/parse paths degrade to `NotRunning`). Reads line 1 as the pid and line 2 (if present) as the DS data dir. If no live process holds the pid → `StalePidFile` (pid + "process is gone" detail). Otherwise records the pid and best-effort `proc.StartTime`, then classifies `Running` when `IdentityMatches` succeeds or `Foreign` (with an "identity mismatch" detail) when it does not.
  - `IdentityMatches(int pid, string dataDirLine, SysProcess proc)` (private) — layered verification: (1) strongest — the pid file's data-dir line equals `expectedDataDir` via `PathsEqual`; (2) Linux — `ReadProcCmdline` still references "Magnetar" or the expected data dir (guards against a recycled pid), else false; (3) Windows fallback — the process name contains "Magnetar" or "SpaceEngineers".
  - `PathsEqual(string a, string b)` (static, private) — normalizes both via `Path.GetFullPath`, trims trailing slashes, and compares with `PlatformPaths.PathComparer`; falls back to a raw comparison if normalization throws.
  - `ReadProcCmdline(int pid)` (static, private) — reads Linux `/proc/<pid>/cmdline`, replacing NUL argument separators with spaces; returns null on absence or error.
  - `TryGetProcess(int pid)` (static, private) — returns the live `System.Diagnostics.Process` for the pid, or null if it does not exist or has already exited.

## Cross-references
- **Uses:** `ServerStatus`/`ServerState` (this module); `PlatformPaths.IsLinux`/`PathComparison`/`PathComparer` (`ConfigTerminal/Io/`); `System.Diagnostics.Process` (`GetProcessById`, `StartTime`, `ProcessName`); `System.IO.File`/`Path`; Linux `/proc/<pid>/cmdline`.
- **Used by:** [MagnetarProcess.cs](MagnetarProcess.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md)
