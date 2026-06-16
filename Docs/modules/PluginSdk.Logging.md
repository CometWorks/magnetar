# Module: PluginSdk.Logging

**Project:** `PluginSdk` · **Files:** 8 · **Source lines:** 435

## Purpose

Provides a unified, environment-agnostic logging API for Magnetar plugins. Plugins obtain a `Logger` (stamped with their name) via `Logger.Create` and call severity methods without knowing how or where messages are stored. The correct sink — `MagnetarLogSink` (forwarding to the SE DS `MyLog.Default`) or `QuasarLogSink` (emitting one-line JSON to stdout **and** raising `LogEnvironment.LineEmitted` for the in-process Quasar Agent to ship over its network channel) — is resolved automatically from the `QUASAR_AGENT` environment variable at startup.

## Role in Magnetar

Sits between plugin code and the two deployment back-ends (standalone Magnetar and Quasar-managed). It is the only layer plugin authors directly reference; all environment detection and rendering is encapsulated here so plugins need zero conditional logging code.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `ILogSink` | interface | [`PluginSdk/Logging/ILogSink.cs`](../descriptions/PluginSdk/Logging/ILogSink.cs.md) | Contract for a log destination; one method Write(in LogEntry) must be thread-safe. |
| `LogEntry` | struct | [`PluginSdk/Logging/LogEntry.cs`](../descriptions/PluginSdk/Logging/LogEntry.cs.md) | Immutable readonly struct carrying timestamp, level, plugin, thread, message, exception and optional data payload; passed by in reference to avoid allocation. |
| `LogEnvironment` | static class | [`PluginSdk/Logging/LogEnvironment.cs`](../descriptions/PluginSdk/Logging/LogEnvironment.cs.md) | Probes the QUASAR_AGENT environment variable and returns the appropriate ILogSink via CreateDefaultSink; also hosts the `LineEmitted` relay the Quasar Agent uses to ship logs over the network. |
| `LogJson` | static class | [`PluginSdk/Logging/LogJson.cs`](../descriptions/PluginSdk/Logging/LogJson.cs.md) | Internal shared System.Text.Json configuration and safe serialization helpers used by both sinks. |
| `LogLevel` | enum | [`PluginSdk/Logging/LogLevel.cs`](../descriptions/PluginSdk/Logging/LogLevel.cs.md) | Five severity levels (Debug through Critical) mirroring VRage MyLogSeverity to allow direct conversion. |
| `Logger` | class | [`PluginSdk/Logging/Logger.cs`](../descriptions/PluginSdk/Logging/Logger.cs.md) | Primary plugin-facing facade; captures plugin name and sink, stamps entries, and dispatches to the sink. |
| `MagnetarLogSink` | class | [`PluginSdk/Logging/MagnetarLogSink.cs`](../descriptions/PluginSdk/Logging/MagnetarLogSink.cs.md) | Forwards entries to VRage MyLog.Default; used in standalone Magnetar; no-ops when MyLog is not yet initialized. |
| `QuasarLogSink` | class | [`PluginSdk/Logging/QuasarLogSink.cs`](../descriptions/PluginSdk/Logging/QuasarLogSink.cs.md) | Serializes each entry as a compact single-line JSON object, writes it to stdout (on-disk log) and raises `LogEnvironment.LineEmitted` for the agent's network relay. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`PluginSdk/Logging/ILogSink.cs`](../descriptions/PluginSdk/Logging/ILogSink.cs.md) | 19 | Defines the single-method contract that every log destination must satisfy. |
| [`PluginSdk/Logging/LogEntry.cs`](../descriptions/PluginSdk/Logging/LogEntry.cs.md) | 48 | A single immutable log record that is passed by `in` reference from `Logger` to `ILogSink`. |
| [`PluginSdk/Logging/LogEnvironment.cs`](../descriptions/PluginSdk/Logging/LogEnvironment.cs.md) | 70 | Acts as the environment probe that decides which `ILogSink` the SDK uses; also hosts the `LineEmitted` agent relay. |
| [`PluginSdk/Logging/LogJson.cs`](../descriptions/PluginSdk/Logging/LogJson.cs.md) | 51 | Centralises `System.Text.Json` configuration and serialization helpers so both `MagnetarLogSink` and `QuasarLogSink` produce identical JSON shapes for the optional structured `data` payload. |
| [`PluginSdk/Logging/LogLevel.cs`](../descriptions/PluginSdk/Logging/LogLevel.cs.md) | 16 | Declares the severity levels used throughout the SDK logging subsystem. |
| [`PluginSdk/Logging/Logger.cs`](../descriptions/PluginSdk/Logging/Logger.cs.md) | 84 | The primary logging facade a plugin holds as a `static readonly` field. |
| [`PluginSdk/Logging/MagnetarLogSink.cs`](../descriptions/PluginSdk/Logging/MagnetarLogSink.cs.md) | 58 | The `ILogSink` used when the server runs under standalone Magnetar (no Quasar Agent). |
| [`PluginSdk/Logging/QuasarLogSink.cs`](../descriptions/PluginSdk/Logging/QuasarLogSink.cs.md) | 89 | The `ILogSink` used when the server process is managed by the Quasar Agent. |

## Public API surface

- `Logger.Create(string pluginName) — factory for plugin code; auto-selects the sink`
- `Logger.Log(LogLevel, string, Exception, object) — generic log method`
- `Logger.Debug/Info/Warning/Error/Critical — convenience severity methods`
- `ILogSink.Write(in LogEntry) — sink contract called by Logger`
- `LogEnvironment.CreateDefaultSink() — selects and constructs the appropriate ILogSink`
- `LogEnvironment.IsManagedByQuasar() — checks QUASAR_AGENT env var`
- `LogEnvironment.LineEmitted — process-wide event of formatted JSON lines, raised by QuasarLogSink for the agent relay`
- `MagnetarLogSink.Format(in LogEntry) — formats a line for MyLog (public for reuse)`
- `QuasarLogSink.Format(in LogEntry) — formats a JSON line (public for testing)`

## Dependencies

**Uses modules:** _none_  
**Used by modules:** [Legacy.Loader](Legacy.Loader.md), [PluginSdk.Stats](PluginSdk.Stats.md), [PluginSdkTests](PluginSdkTests.md)  
**External systems:** SE DS assemblies (VRage.Library: VRage.Utils.MyLog, VRage.Utils.MyLogSeverity)

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
