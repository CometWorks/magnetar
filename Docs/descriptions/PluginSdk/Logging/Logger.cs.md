# PluginSdk/Logging/Logger.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Logging` · **Kind:** sealed class · **Lines:** 84

## Summary
The primary logging facade a plugin holds as a `static readonly` field. A `Logger` captures the plugin's name and an `ILogSink` once at construction time, then stamps every emitted entry with the current UTC time and managed thread id before forwarding it to the sink. Because the sink is injected, the same `Logger` API works unchanged in standalone Magnetar (forwarding to `MyLog.Default`) and in a Quasar-managed process (emitting JSON). The static factory `Logger.Create` resolves the sink automatically via `LogEnvironment`.

## Types

### `Logger` — sealed class, public
Provides per-severity convenience methods and a generic `Log` overload. All paths converge on the private `Write` method which allocates a `LogEntry` struct (on the stack) and passes it by `in` reference to the sink.

- **Fields:**
  - `pluginName : string` (private readonly) — name stamped on every entry; null guard on construction
  - `sink : ILogSink` (private readonly) — the rendering back-end; null guard on construction
- **Properties:** `PluginName : string` — exposes the captured plugin name (read-only)
- **Methods:**
  - `Create(string pluginName) : Logger` (static) — constructs a `Logger` using `LogEnvironment.CreateDefaultSink()`; the preferred factory for plugin code
  - `Debug(string message, object data = null)` — emits at `LogLevel.Debug`
  - `Info(string message, object data = null)` — emits at `LogLevel.Info`
  - `Warning(string message, object data = null)` — emits at `LogLevel.Warning`
  - `Error(string message, object data = null)` — emits at `LogLevel.Error` without an exception
  - `Error(string message, Exception exception, object data = null)` — emits at `LogLevel.Error` with an attached exception
  - `Critical(string message, object data = null)` — emits at `LogLevel.Critical` without an exception
  - `Critical(string message, Exception exception, object data = null)` — emits at `LogLevel.Critical` with an attached exception
  - `Log(LogLevel level, string message, Exception exception = null, object data = null)` — generic overload; delegates to `Write`
  - `Write(...)` (private) — constructs a `LogEntry` and calls `sink.Write(in entry)`

## Cross-references
- **Uses:** `PluginSdk/Logging/ILogSink.cs`, `PluginSdk/Logging/LogEntry.cs`, `PluginSdk/Logging/LogLevel.cs`, `PluginSdk/Logging/LogEnvironment.cs`
- **Used by:** [PluginStats.cs](../Stats/PluginStats.cs.md), [LoggingTests.cs](../../PluginSdkTests/LoggingTests.cs.md)
