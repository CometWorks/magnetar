# PluginSdk/Logging/LogLevel.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Logging` · **Kind:** enum · **Lines:** 16

## Summary
Declares the severity levels used throughout the SDK logging subsystem. The enum is intentionally ordered and named to mirror VRage's `MyLogSeverity` exactly (same members, same ordinal values), so `MagnetarLogSink` can convert between the two with a trivial switch expression and no remapping table.

## Types

### `LogLevel` — enum, public
Five severity levels in ascending order: `Debug`, `Info`, `Warning`, `Error`, `Critical`. Matching VRage's `MyLogSeverity` means the game's built-in log filtering and coloring apply correctly when forwarding through `MagnetarLogSink`.

## Cross-references
- **Uses:** VRage `MyLogSeverity` (conceptual mirror; no direct code dependency in this file)
- **Used by:** [PluginInstance.cs](../../Legacy/Loader/PluginInstance.cs.md), [LogEntry.cs](LogEntry.cs.md), [Logger.cs](Logger.cs.md), [MagnetarLogSink.cs](MagnetarLogSink.cs.md), [LoggingTests.cs](../../PluginSdkTests/LoggingTests.cs.md)
