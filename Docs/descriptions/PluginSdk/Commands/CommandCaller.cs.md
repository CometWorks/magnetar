# PluginSdk/Commands/CommandCaller.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** readonly struct · **Lines:** 37

## Summary
`CommandCaller` is an immutable snapshot of the identity and permission level of the player (or server console) who issued a chat command. It is populated by the host-side chat hook before being passed to `CommandDispatcher.Handle`, so command handlers and the dispatcher always operate on a stable, value-type identity rather than live game objects. It wraps the SE DS `MyPromoteLevel` type from `VRage.Game.ModAPI` for permission checks.

## Types

### `CommandCaller` — readonly struct, public

Passed by-value (`in`) throughout the dispatch pipeline to avoid unnecessary boxing. The `IsConsole` flag distinguishes server-console invocations (which typically carry a zero `IdentityId` and a synthetic `SteamId`).

- **Properties:**
  - `SteamId` — Steam platform identifier of the caller.
  - `IdentityId` — In-game identity id used as the reply target; `0` for the server console.
  - `Name` — Display name shown in log messages.
  - `PromoteLevel` — `MyPromoteLevel` value consulted by `RegisteredCommand.IsVisibleTo` and `CommandRoot.IsAvailableTo` for permission filtering.
  - `IsConsole` — `true` when the command originates from the server console rather than a player's chat.

## Cross-references
- **Uses:** `VRage.Game.ModAPI.MyPromoteLevel` (SE DS assembly)
- **Used by:** [CommandService.cs](../../Legacy/Commands/CommandService.cs.md), [ServerCommandResponder.cs](../../Legacy/Commands/ServerCommandResponder.cs.md), [CommandContext.cs](CommandContext.cs.md), [CommandDispatcher.cs](CommandDispatcher.cs.md), [ICommandResponder.cs](ICommandResponder.cs.md), [CommandTests.cs](../../PluginSdkTests/CommandTests.cs.md)
