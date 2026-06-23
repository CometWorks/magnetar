# PluginSdk/Logging/LogEnvironment.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Logging` · **Kind:** static class · **Lines:** 60

## Summary
Acts as the environment probe that decides which `ILogSink` the SDK uses. When the Quasar Agent launches a managed server process it injects the `QUASAR_AGENT` environment variable; `LogEnvironment` detects that variable and selects `QuasarLogSink`. When the variable is absent (standalone Magnetar), `MagnetarLogSink` is chosen instead. It also hosts `LineEmitted`, a process-wide tap that `QuasarLogSink` raises for each formatted JSON line so the in-process Quasar Agent can ship plugin logs to Quasar over its network channel — a delivery path that survives Quasar restarts and reconnects to a detached server daemon, unlike standard-output capture.

## Types

### `LogEnvironment` — static class, public
Provides the sink factory helpers used by `Logger.Create`, plus the agent log relay.

- **Fields:** `QuasarEnvironmentVariable = "QUASAR_AGENT"` — the environment variable name the Quasar Agent sets on managed server processes
- **Events:**
  - `LineEmitted : Action<string>` (public static) — raised once per formatted JSON log line by `QuasarLogSink`. The in-process Quasar Agent subscribes to buffer and ship lines; nothing is wired in standalone Magnetar
- **Methods:**
  - `IsManagedByQuasar() : bool` — returns true when `QUASAR_AGENT` is set to a non-empty string
  - `CreateDefaultSink() : ILogSink` — returns a new `QuasarLogSink` if managed by Quasar, otherwise a new `MagnetarLogSink`
  - `RaiseLineEmitted(string line)` (internal static) — invokes `LineEmitted`; subscriber exceptions are **not** caught and propagate to the caller (by design — a failure in the log relay has no way to be logged, so it is allowed to surface rather than be silently swallowed)

## Cross-references
- **Uses:** `PluginSdk/Logging/ILogSink.cs`, `PluginSdk/Logging/MagnetarLogSink.cs`, `PluginSdk/Logging/QuasarLogSink.cs`
- **Used by:** [Logger.cs](Logger.cs.md), [QuasarLogSink.cs](QuasarLogSink.cs.md), [LoggingTests.cs](../../PluginSdkTests/LoggingTests.cs.md)
