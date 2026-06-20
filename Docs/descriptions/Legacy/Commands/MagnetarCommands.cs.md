# Legacy/Commands/MagnetarCommands.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Commands` · **Kind:** class (multiple) · **Lines:** 61

## Summary
Declares three built-in chat-command modules — `!save`, `!restart`, and `!quit` — that Magnetar registers with `CommandService` before any plugin loads. Because they are registered first and last-registration wins, a plugin may override any of them. Each command offloads its lifecycle work to a worker thread via `Task.Run` so save/restart work can block to completion without stalling the game-update thread; `!save` acknowledges immediately and then sends a completion, timeout, or failure reply after `ServerControl.SaveWorld()` returns.

## Types

### `SaveCommand` — class, public : `CommandModule`
Handles `!save` (bare root, no subcommand). Responds with "Saving world…" immediately, then calls `ServerControl.SaveWorld()` on a worker thread. When the blocking save call returns, it posts a game-thread chat reply: "World saved." on success, a timeout message on false, or a failure message if an exception was thrown (also logging the exception).

- **Methods:**
  - `Save()` — `[Command("", "Save the world")]`; captures the command context, acknowledges the caller, dispatches `ServerControl.SaveWorld` on a `Task`, and posts the final reply through `Game.RunOnGameThread`

### `RestartCommand` — class, public : `CommandModule`
Handles `!restart`. Responds with "Saving world and restarting the server…" then calls `ServerControl.SaveAndRestart()` on a worker thread (save → raise `Terminating(Restart)` → restart process via `execve`/`Process.Start`).

- **Methods:**
  - `Restart()` — `[Command("", "Save and restart the server")]`; responds to caller, dispatches `ServerControl.SaveAndRestart` on a `Task`

### `QuitCommand` — class, public : `CommandModule`
Handles `!quit`. Responds with "Shutting the server down without saving…" then calls `ServerControl.QuitWithoutSaving()` on a worker thread (raises `Terminating(Shutdown)` → dispose plugins → exit).

- **Methods:**
  - `Quit()` — `[Command("", "Shut the server down without saving")]`; responds to caller, dispatches `ServerControl.QuitWithoutSaving` on a `Task`

## Cross-references
- **Uses:**
  - `PluginSdk/Commands/CommandModule.cs` — base class; provides `Context` (including `Context.Respond`)
  - `PluginSdk/Commands/CommandAttribute.cs` — `[Command]` attribute
  - `PluginSdk/Commands/CommandRootAttribute.cs` — `[CommandRoot]` attribute
  - `Legacy/Launcher/Game.cs` — `Game.RunOnGameThread` for final `!save` replies
  - `Legacy/Launcher/ServerControl.cs` — `SaveWorld`, `SaveAndRestart`, `QuitWithoutSaving` implementations
  - `Shared/LogFile.cs` — logs unexpected `!save` failures
- **Used by:** [PluginLoader.cs](../Loader/PluginLoader.cs.md)
