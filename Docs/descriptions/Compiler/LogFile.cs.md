# Compiler/LogFile.cs

**Project:** Compiler · **Namespace:** `Pulsar.Compiler` · **Kind:** static class · **Lines:** 122

## Summary
Minimal NLog-backed file logger used by the Compiler module to record Roslyn reference loading, publicizing, and compilation diagnostics to the active Magnetar `info_*.log` file. It exists because the Compiler can run in a separate AppDomain (`RoslynCompiler` derives from `MarshalByRefObject`) and on both runtimes (Legacy .NET Framework 4.8 / Interim .NET 10), so it needs a self-contained, exception-swallowing log sink independent of the host plugin logging. All other Compiler types call `LogFile.WriteLine` for diagnostics.

## Types
### LogFile — static class, public
Owns a single named NLog `Logger` ("Pulsar") and its `LogFactory`, configured to append timestamped lines to the active `info_*.log` named by `<mainPath>/info.current`. If the marker is missing, it creates a new timestamped file and marker. Every public method is wrapped in try/catch so logging failures never propagate into the compile pipeline; on a write failure the logger is disposed.
- **Fields:** `fileNameBase` / `fileExtension` / `currentLogFileName` — naming constants; `logger` — the active NLog `Logger`, or `null` when uninitialized/disposed; `logFactory` — the `LogFactory` owning configuration and lifetime.
- **Methods:**
  - `Init(string mainPath)` — resolves the active log file from `info.current`, builds a `LoggingConfiguration` with a single `FileTarget` (append mode: `DeleteOldFileOnStartup=false`, `ReplaceFileContentsOnEachWrite=false`, `KeepFileOpen=false`); layout is `${longdate} [${level:uppercase=true}] (${threadid}) ${message:withexception=true}`; creates the `LogFactory` with `ThrowExceptions=false` and obtains the `"Pulsar"` logger, nulling it on failure.
  - `Error(string text)` — logs at `LogLevel.Error` via `WriteLine`.
  - `Warn(string text)` — logs at `LogLevel.Warn` via `WriteLine`.
  - `WriteLine(string text, LogLevel level = null)` — core sink; defaults level to `Info`, calls `logger?.Log`, and on any exception calls `Dispose()` to stop further attempts.
  - `Dispose()` — flushes and disposes the `LogFactory` (no-op if `logger` is already null), then nulls both `logger` and `logFactory`; swallows flush/dispose exceptions.

## Cross-references
- **Uses:** NLog (`Logger`, `LogFactory`, `LoggingConfiguration`, `FileTarget`, `SimpleLayout`); `System.IO.Path`; `System.Globalization`.
- **Used by:** _none within the repository_
