# PluginSdk/Logging/ILogSink.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Logging` · **Kind:** interface · **Lines:** 19

## Summary
Defines the single-method contract that every log destination must satisfy. `Logger` writes all entries through this interface, so the logging back-end can be swapped without touching plugin code. Two concrete implementations ship with the SDK: `MagnetarLogSink` (forwards to the SE DS game log via `VRage.Utils.MyLog.Default`) and `QuasarLogSink` (emits one JSON object per entry to the Quasar Agent). The correct implementation is selected at runtime by `LogEnvironment.CreateDefaultSink`.

## Types

### `ILogSink` — interface, public
Receives log entries from a `Logger` and writes them to a backing store. Implementations must be safe to call concurrently from multiple threads because `Logger` makes no attempt to serialize calls.

- **Methods:** `Write(in LogEntry entry)` — writes one log record; `in` passing avoids copying the struct

## Cross-references
- **Uses:** `PluginSdk/Logging/LogEntry.cs`
- **Used by:** [LogEnvironment.cs](LogEnvironment.cs.md), [Logger.cs](Logger.cs.md), [MagnetarLogSink.cs](MagnetarLogSink.cs.md), [QuasarLogSink.cs](QuasarLogSink.cs.md), [LoggingTests.cs](../../PluginSdkTests/LoggingTests.cs.md)
