# Server Control: Save, Reload, Quit, Restart

`PluginSdk.ServerControl` is a static facade a plugin uses to drive the
dedicated server's lifecycle — save the world, reload the dedicated config, and
quit or restart the process. You call the static methods directly; the host
binds the real implementations at startup. These mirror the operations the host
also triggers from POSIX signals (`SIGTERM`/`SIGINT` → `SaveAndQuit`, `SIGHUP`
→ `ReloadConfig`).

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
