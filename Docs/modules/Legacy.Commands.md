# Module: Legacy.Commands

**Project:** `Legacy` · **Files:** 3 · **Source lines:** 297

## Purpose

Provides the server-side chat-command pipeline for the Legacy (.NET Framework 4.8 / Windows) build of Magnetar. It owns command registration (via CommandService implementing ICommandRegistrar), dispatch of incoming chat through CommandDispatcher, delivery of replies into the SE DS scripted-message API (ServerCommandResponder), and the four built-in lifecycle commands !save, !restart, !quit, and !stop (MagnetarCommands).

## Role in Magnetar

Sits between the Harmony chat-intercept patch (Legacy.Patch/Patch_ServerChat) and the PluginSdk command framework. CommandService is instantiated and held by PluginLoader (Legacy.Loader), installed as the ServerCommands.Registrar so plugins register through the SDK facade, and invoked by the Harmony prefix whenever a global-chat message begins with '!'. MagnetarCommands registers built-in server-lifecycle commands before plugins load, and ServerCommandResponder bridges PluginSdk reply objects onto SE DS MyVisualScriptLogicProvider.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `CommandService` | class | [`Legacy/Commands/CommandService.cs`](../descriptions/Legacy/Commands/CommandService.cs.md) | Host-side ICommandRegistrar: owns CommandRegistry and CommandDispatcher, resolves SE identity for callers, routes chat to registered command roots. |
| `SaveCommand` | class | [`Legacy/Commands/MagnetarCommands.cs`](../descriptions/Legacy/Commands/MagnetarCommands.cs.md) | Built-in !save command module; acknowledges caller, saves on a worker thread, then posts a completion/timeout reply. |
| `RestartCommand` | class | [`Legacy/Commands/MagnetarCommands.cs`](../descriptions/Legacy/Commands/MagnetarCommands.cs.md) | Routes one coordinated cluster restart request, or preserves standalone save/restart behavior. |
| `QuitCommand` | class | [`Legacy/Commands/MagnetarCommands.cs`](../descriptions/Legacy/Commands/MagnetarCommands.cs.md) | Routes one cluster shutdown request, or preserves standalone quit behavior. |
| `StopCommand` | class | [`Legacy/Commands/MagnetarCommands.cs`](../descriptions/Legacy/Commands/MagnetarCommands.cs.md) | Routes one save-first cluster shutdown request, or preserves standalone save/quit behavior. |
| `LifecycleCommandRouting` | static class | [`Legacy/Commands/MagnetarCommands.cs`](../descriptions/Legacy/Commands/MagnetarCommands.cs.md) | Builds correlated chat-origin requests and reports typed acknowledgements on the game thread. |
| `ServerCommandResponder` | class | [`Legacy/Commands/ServerCommandResponder.cs`](../descriptions/Legacy/Commands/ServerCommandResponder.cs.md) | ICommandResponder singleton that delivers CommandReply values into SE DS chat via MyVisualScriptLogicProvider. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Legacy/Commands/CommandService.cs`](../descriptions/Legacy/Commands/CommandService.cs.md) | 114 | `CommandService` is the host-side owner of the chat-command pipeline for the Legacy (.NET Framework 4.8 / Windows) build of Magnetar. |
| [`Legacy/Commands/MagnetarCommands.cs`](../descriptions/Legacy/Commands/MagnetarCommands.cs.md) | 146 | Declares four built-in chat-command modules — `!save`, `!restart`, `!quit`, and `!stop` — that Magnetar registers before plugins. |
| [`Legacy/Commands/ServerCommandResponder.cs`](../descriptions/Legacy/Commands/ServerCommandResponder.cs.md) | 37 | `ServerCommandResponder` is the `ICommandResponder` implementation that delivers command replies into the SE DS chat system. |

## Public API surface

- `CommandService.Register(Assembly) — scan an assembly for CommandModule types and attribute them to it`
- `CommandService.Register(Assembly, params Type[]) — register explicit CommandModule types`
- `CommandService.HandleChat(ulong steamId, string text) → bool — called by Patch_ServerChat; returns true when the message was a handled command`
- `ServerCommandResponder.Instance — shared ICommandResponder singleton passed to CommandDispatcher.Handle`

## Dependencies

**Uses modules:** [Legacy.Launcher](Legacy.Launcher.md), [PluginSdk.Commands](PluginSdk.Commands.md), [PluginSdk.Runtime](PluginSdk.Runtime.md)
**Used by modules:** [Legacy.Loader](Legacy.Loader.md), [Legacy.Patch](Legacy.Patch.md)
**External systems:** SE DS assemblies

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
