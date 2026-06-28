# Shared/LogFile.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared` · **Kind:** static class (+ interface) · **Lines:** 129

## Summary
Magnetar's central logging facade. Configures an NLog `FileTarget` writing to a per-start `info_yyyyMMdd_HHmmssfff.log` under the main directory and exposes thread-id/level/timestamp-formatted line writers used throughout the loader. It also writes `info.current` with the active file name so companion components can append to the same startup log. Logging is fail-soft: any logging exception disposes the logger rather than propagating, so a logging failure can never crash the DS bootstrap. Also declares `IGameLog`, the hook by which environment-specific code can bridge into the SE game's own log.

## Types
### `IGameLog` — interface, public
Abstraction over the SE Dedicated Server's native log so shared code can write to it without referencing SE assemblies directly.
- **Methods:** `Open()` — open/attach the game log; `Exists()` — whether the game log is available; `Write(string line)` — append a line to it.

### `LogFile` — static class, public
Owns a single NLog `Logger`/`LogFactory` targeting one timestamped `info_*.log` for the current startup. The file is not deleted on startup (`DeleteOldFileOnStartup=false`), is opened per-write (`KeepFileOpen=false`), and uses layout `${longdate} [${level}] (${threadid}) ${message:withexception=true}`. `ThrowExceptions=false` keeps NLog quiet on failure.
- **Fields:** `GameLog` — public pluggable `IGameLog` (set by environment code; not used internally here); `fileNameBase` / `fileExtension` / `currentLogFileName` — naming constants; `logger`, `logFactory` — NLog instances; `file` — full active log path.
- **Methods:** `Init(string mainPath)` — creates a timestamped log path, writes `info.current`, builds the NLog config/target, and obtains the logger (null on failure); `Error(string)` / `Warn(string)` — write at the respective level; `WriteLine(string, LogLevel=null)` — core writer defaulting to `Info`, disposing on exception; `Open()` — launches the active log file with the OS default handler via `Process.Start(UseShellExecute=true)`; `Dispose()` — flushes and disposes the factory, nulling the logger.

## Cross-references
- **Uses:** NLog (`Logger`, `LogFactory`, `FileTarget`, `SimpleLayout`); `System.Diagnostics.Process`.
- **Used by:** [Game.cs](../Legacy/Launcher/Game.cs.md)
