# Server Control: Save, Reload, Quit, Restart

`PluginSdk.ServerControl` is a static facade a plugin uses to drive the
dedicated server's lifecycle — save the world, reload the dedicated config, and
quit or restart the process. You call the static methods directly; the host
binds the real implementations at startup. These mirror the operations the host
also triggers from POSIX signals (`SIGTERM`/`SIGINT` → `SaveAndQuit`, `SIGHUP`
→ `ReloadConfig`).

It also raises a `Terminating` event so a plugin can *observe* an admin-driven
shutdown or restart and react before the process goes down — see
[Reacting to admin lifecycle commands](#reacting-to-admin-lifecycle-commands).

```csharp
using PluginSdk;

ServerControl.SaveWorld();
```

There is nothing to register and no host id to pass — the facade is global. You
only need `using PluginSdk;`.

## The API

| Method | Returns | Effect |
|---|---|---|
| `SaveWorld()` | `bool` | Saves the world without quitting. `false` when no session is loaded or the host has not bound an implementation. |
| `ReloadConfig()` | `bool` | Saves the world, then re-reads the dedicated config and applies the settings that are safe to change at runtime (the MOTD in particular). Does not quit. `false` when no session is loaded or unbound. |
| `SaveAndQuit()` | `void` | Saves the world, then quits the process with exit code 0. |
| `SaveAndRestart()` | `void` | Saves the world, then replaces the process with a fresh instance launched with the original command line, environment and working directory captured at first startup. |
| `QuitWithoutSaving()` | `void` | Quits the process immediately with exit code 0, without saving. |
| `RestartWithoutSaving()` | `void` | Restarts the process immediately (original command line, environment and working directory), without saving. |

The four shutdown/restart methods first consult the optional typed cluster
lifecycle provider described in [Clustering.md](Clustering.md). With no provider
registered, standalone behaviour is unchanged. With one registered, the call is
routed asynchronously and never falls back to the local action, including after
rejection, link loss, timeout, or provider failure. The existing methods remain
`void`; callers that need the authoritative acknowledgement can construct a
`ClusterLifecycleRequest` and call `ClusterLifecycle.TryRequest` directly.

This routing applies to plugin calls and Magnetar's in-game `!quit`, `!stop`, and
`!restart` commands. It deliberately does not intercept `SIGTERM`/`SIGINT` or
the dedicated server's internal exit path, so the process executor retains its
host-level stop authority.

## Reacting to admin lifecycle commands

The methods above let a plugin *cause* a lifecycle change. The `Terminating`
event lets a plugin *observe* one an admin started, and run a little work before
the server goes down — for example, notify an external control plane so it does
not treat the stop as a crash and bring the server back.

```csharp
public static event Action<ServerTerminationKind> Terminating;

public enum ServerTerminationKind
{
    Shutdown,   // !quit / !stop — the server is meant to stay down
    Restart,    // !restart      — the server will come back up
}
```

Subscribe in `Init`, unsubscribe in `Dispose`:

```csharp
using PluginSdk;

public void Init(object gameInstance)
{
    ServerControl.Terminating += OnTerminating;
}

public void Dispose()
{
    ServerControl.Terminating -= OnTerminating;
}

private void OnTerminating(ServerTerminationKind kind)
{
    if (kind == ServerTerminationKind.Shutdown)
        // an admin asked the server to stay down — react here
        NotifyMyControlPlane();
}
```

What you can rely on:

- **In-game admin commands only.** It fires for the `!quit`/`!stop` command
  (`Shutdown`) and the `!restart` command (`Restart`). It does **not** fire for
  `SIGTERM`/`SIGINT` (e.g. `systemctl stop`) or SE's internal exit path — those
  are not an admin lifecycle intent, so distinguish "stay down" from "comes
  back" by the `kind`, not by inferring it from a process exit.
- **Raised before teardown.** It fires on the thread performing the termination,
  while plugins are still loaded and the session is still intact — before any
  world save or plugin disposal. So your handler still has a live game and a
  working plugin to act through.
- **Keep handlers short.** Teardown continues once every handler returns. You
  may block briefly (e.g. to flush a network message) but must not hang; a
  handler that throws is isolated and never blocks the shutdown or the other
  subscribers.
- **Fires once per termination**, and only for these admin commands. Save
  (`!save`) and config reload (`!reload`) do not raise it — they do not tear the
  process down.

## Behaviour before the host binds

The host installs the real implementations once at launcher startup. Before
binding — or in a non-hosted context such as a unit test — every call is a safe
no-op: the `bool`-returning methods report `false` and the others do nothing.
So calling `ServerControl` early (e.g. from `Init()`) never throws.

## Thread safety

All calls are thread-safe. The host marshals world access to the game's update
thread and null-guards when no session is loaded, so a plugin may invoke these
from any thread, including from a chat-command handler running on the update
thread.

## Typical use

A chat command that saves the world, reusing the [Commands](Commands.md)
pipeline:

```csharp
using PluginSdk;
using PluginSdk.Commands;
using VRage.Game.ModAPI;   // MyPromoteLevel

[CommandRoot("ess", "Essentials", "core admin tools")]
public sealed class AdminCommands : CommandModule
{
    [Command("save", "Saves the world")]
    [Permission(MyPromoteLevel.Admin)]
    public string Save()
        => ServerControl.SaveWorld() ? "World saved." : "No world is loaded.";

    [Command("restart", "Saves and restarts the server")]
    [Permission(MyPromoteLevel.Admin)]
    public string Restart()
    {
        ServerControl.SaveAndRestart();   // process is replaced; no return reached on success
        return "Restarting…";
    }
}
```

## Notes and limits

- The `bool`-returning calls (`SaveWorld`, `ReloadConfig`) tell you whether the
  operation could run — check the result if it matters. The `void` calls
  (`SaveAndQuit`, `SaveAndRestart`, `QuitWithoutSaving`, `RestartWithoutSaving`)
  tear down or replace the process, so code after them on the success path does
  not run.
- `ReloadConfig` only applies the dedicated-config settings that are safe to
  change at runtime; it is not a full reconfiguration.
- The facade does not expose binding — installing the implementations is
  host-only. A plugin just calls the methods.
