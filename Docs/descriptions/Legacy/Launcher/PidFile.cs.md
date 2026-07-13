# Legacy/Launcher/PidFile.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Launcher` · **Kind:** static class · **Lines:** 79

## Summary
Writes and removes `magnetar.pid` in the Magnetar config directory so an external tool (MagnetarConfig) can discover this dedicated-server instance and verify the running process belongs to it. The file has two lines — the process id, then the resolved DS data directory (the `-path` value) as an identity line. It is written once startup has passed the daemon detach (so the pid is final) and removed on every clean shutdown path; a crash leaves it behind, so readers must always re-verify the pid is live and its identity matches rather than trust the file's presence alone.

## Types
### PidFile — static class, internal
Owns the pid-file lifecycle for the launcher. Best-effort by design: both operations swallow exceptions, since a failed write or delete only costs external observability, never server startup or shutdown.

- **Fields:**
  - `FileName` (const string) — `"magnetar.pid"`, the fixed file name written into the config dir.
  - `writtenPath` (static string) — the full path last written, cached so `Delete()` can remove the file without re-resolving the config dir from the shutdown path (`ServerControl` holds no reference to it).
- **Methods:**
  - `Write(string configDir, string dataDir)` — writes the pid file into `configDir` (Magnetar's config directory); returns immediately if `configDir` is null/empty. Content is the current process id (formatted with `CultureInfo.InvariantCulture`) on line 1 and `dataDir` (or an empty string when null) on line 2, each terminated with `"\n"`. Records the path in `writtenPath` and logs via `LogFile.WriteLine`; on any exception logs a warning via `LogFile.Warn` and never throws. Pass null/empty `dataDir` when the DS runs on its default instance.
  - `Delete()` — removes the file recorded by `Write` (`writtenPath`). No-op when nothing was written; deletes only if the file still exists, clears `writtenPath` in a `finally`, and swallows all exceptions (a leftover pid file is treated as stale by readers). Idempotent, never throws.

## Cross-references
- **Uses:** `Pulsar.Shared` (`LogFile`); `System.Diagnostics.Process` (current pid); `System.IO.File`/`Path`; `System.Globalization.CultureInfo`.
- **Used by:** [ServerControl.cs](ServerControl.cs.md), [Program.cs](../Program.cs.md)
