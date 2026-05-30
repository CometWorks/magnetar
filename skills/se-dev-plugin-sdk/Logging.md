# Logging: One Logger, Two Environments

`PluginSdk.Logging` gives a plugin a single `Logger` it uses the same way
whether the server runs **standalone Magnetar** or is **managed by Quasar**.
The plugin never branches on the environment — it calls `Logger.Create` once
and logs. The SDK picks the right destination (sink) underneath:

| Environment | Sink | What it produces |
|---|---|---|
| Standalone Magnetar | `MagnetarLogSink` | Forwards to the game's `MyLog.Default`, prefixed with plugin name and thread id. |
| Managed by Quasar | `QuasarLogSink` | One structured JSON object per line, for ingestion by the Quasar Agent. |

## Getting a logger

```csharp
using PluginSdk.Logging;

private static readonly Logger Log = Logger.Create("MyPlugin");
```

`Logger.Create(pluginName)` selects the sink for the current process via
[`LogEnvironment`](#environment-selection). The plugin name is stamped onto
every entry, so pass the same name you use elsewhere to identify the plugin.

## Logging

```csharp
Log.Debug("Entering load phase");
Log.Info("Loaded 42 definitions");
Log.Warning("Mod download retried");
Log.Error("Failed to patch method", exception);
Log.Critical("Unrecoverable state", exception);
```

Levels mirror VRage's `MyLogSeverity` one-to-one (same members, same order),
so the standalone sink forwards them without remapping:

`Debug` · `Info` · `Warning` · `Error` · `Critical`

Every entry is stamped automatically with:

- the **UTC timestamp** (captured at the call),
- the **plugin name** (from `Create`),
- the **managed thread id** of the calling thread.

### Methods

| Method | Purpose |
|---|---|
| `Debug/Info/Warning/Error/Critical(string message, object data = null)` | Log a message, optionally with a data payload. |
| `Error/Critical(string message, Exception exception, object data = null)` | Log a message with an exception (and optional data). |
| `Log(LogLevel level, string message, Exception exception = null, object data = null)` | The general form the others delegate to. |

Overload resolution does the obvious thing: a second argument that is an
`Exception` binds to the exception overload; any other object binds to the
`data` overload. The two can be combined: `Log.Error("failed", ex, payload)`.

## Structured data payloads

Every method takes an optional **JSON-serializable** `data` object. How it is
rendered depends on the sink, but you write the call once either way:

```csharp
Log.Info("Mods downloaded", new { count = 12, totalBytes = 8_421_344 });
```

- **Standalone Magnetar** — the payload is serialized and **appended to the log
  line** as JSON text:

  ```
  Info: [MyPlugin] [thread 1] Mods downloaded {"count":12,"totalBytes":8421344}
  ```

  (`MyLog` prepends its own timestamp, thread and severity; the SDK adds the
  `[plugin] [thread N]` prefix.)

- **Managed by Quasar** — the payload becomes a nested **`data` field** in the
  JSON object (a real object, not a string):

  ```json
  {"timestamp":"2026-05-30T12:34:56.123456Z","level":"Info","plugin":"MyPlugin","thread":1,"message":"Mods downloaded","data":{"count":12,"totalBytes":8421344}}
  ```

Use any serializable shape: an anonymous object, a POCO, a dictionary. The two
sinks serialize it through the same options, so the JSON is identical in both
modes. Serialization **never throws** — a payload that cannot be serialized
(e.g. a cyclic graph) is replaced by a small `{"error":"...","type":"..."}`
object so a bad payload can never take down logging.

## The Quasar JSON line

`QuasarLogSink` emits one object per entry (newline-delimited JSON):

| Field | Notes |
|---|---|
| `timestamp` | UTC, ISO 8601, **microsecond** (6-digit) precision, e.g. `2026-05-30T12:34:56.123456Z`. |
| `level` | Severity name (`Info`, `Warning`, ...). |
| `plugin` | Plugin name from `Create`. |
| `thread` | Managed thread id. |
| `message` | The log message. |
| `data` | The serialized payload — **omitted when absent**. |
| `exception` | `Exception.ToString()` — **omitted when absent**. |

> **Transport is a placeholder.** Until the Quasar Agent integration lands,
> `QuasarLogSink` writes JSON lines to **standard output** (which the agent
> captures from the managed process). The constructor accepts an
> `Action<string>` line writer, so the real transport drops in later without
> touching any call site:
>
> ```csharp
> var sink = new QuasarLogSink(line => agentChannel.Send(line));
> var log  = new Logger("MyPlugin", sink);
> ```

## Environment selection

`LogEnvironment` decides which sink `Logger.Create` uses:

```csharp
LogEnvironment.IsManagedByQuasar();   // true under Quasar
LogEnvironment.CreateDefaultSink();   // QuasarLogSink or MagnetarLogSink
```

Selection is driven by the `QUASAR_AGENT` environment variable
(`LogEnvironment.QuasarEnvironmentVariable`): when the Quasar Agent launches a
managed server it sets it, and any non-empty value switches logging to the
JSON sink. Otherwise the standalone sink is used.

> The variable name is the current detection hook and may change when the
> Quasar Agent integration is finalized. Plugins should **not** read it
> directly — call `Logger.Create` and let the SDK decide.

## Custom and explicit sinks

Pass a sink to the constructor to bypass auto-selection — useful in tests or to
redirect output:

```csharp
// Capture log entries in a test:
var entries = new List<LogEntry>();
var log = new Logger("MyPlugin", new TestSink(entries));

// Force JSON to a custom writer:
var log = new Logger("MyPlugin", new QuasarLogSink(myWriter.WriteLine));
```

A sink is any `ILogSink`:

```csharp
public interface ILogSink
{
    void Write(in LogEntry entry);   // must be thread-safe
}
```

`LogEntry` is an immutable `readonly struct` carrying the timestamp, level,
plugin name, thread id, message, optional exception and optional data payload.
`Logger` passes it by `in` reference, so logging a message allocates nothing
for the entry itself.

## What the Logger does *not* do

- It does **not** configure or open the game log. `MagnetarLogSink` forwards to
  `MyLog.Default` and is a **no-op** when that log is null or disabled (e.g.
  before the game has initialized), so logging early never throws.
- It does **not** filter by level — every entry reaches the sink. Add level
  gating in a custom sink if you need it.
- It does **not** buffer, batch, or flush. The Magnetar sink follows `MyLog`'s
  own flushing; the Quasar sink writes a line per entry.
- It does **not** transport anything to Quasar yet — see the placeholder note
  above.
