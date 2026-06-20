# PluginSdk/Logging/LogEntry.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Logging` · **Kind:** readonly struct · **Lines:** 48

## Summary
A single immutable log record that is passed by `in` reference from `Logger` to `ILogSink`. Because it is a `readonly struct` and is passed by reference, producing a log entry causes zero heap allocation for the entry itself. It carries every field a sink needs to render the line: timestamp, severity, plugin identity, thread identity, text message, an optional exception, and an optional structured data payload.

## Types

### `LogEntry` — readonly struct, public
Holds all fields for one log event. The primary constructor (C# 12 syntax) sets every property directly. There are no mutable paths; sinks receive an `in` reference and treat the record as read-only.

- **Properties:**
  - `UtcTimestamp` — `DateTime.UtcNow` captured at emit time; always UTC
  - `Level` — `LogLevel` severity enum value
  - `PluginName` — name of the originating plugin, supplied once to `Logger`
  - `ThreadId` — `Environment.CurrentManagedThreadId` at the moment of the call
  - `Message` — the human-readable text; never null (empty string when the caller passes null)
  - `Exception` — optional attached exception; null when none
  - `Data` — optional JSON-serializable payload object; null when none; sinks serialize it via `LogJson`

## Cross-references
- **Uses:** `PluginSdk/Logging/LogLevel.cs`, `PluginSdk/Logging/LogJson.cs` (indirectly, through sinks)
- **Used by:** [ILogSink.cs](ILogSink.cs.md), [Logger.cs](Logger.cs.md), [MagnetarLogSink.cs](MagnetarLogSink.cs.md), [QuasarLogSink.cs](QuasarLogSink.cs.md), [LoggingTests.cs](../../PluginSdkTests/LoggingTests.cs.md)
