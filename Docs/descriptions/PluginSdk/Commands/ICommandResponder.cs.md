# PluginSdk/Commands/ICommandResponder.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** interface · **Lines:** 18

## Summary
`ICommandResponder` is the abstraction between the command dispatch pipeline and the actual SE DS chat API. The host implements it to route `CommandReply` values into the game's chat system; test harnesses implement it to capture replies without a live game. By depending on this interface rather than on the SE DS chat API directly, `CommandDispatcher` and `CommandContext` are fully testable in isolation.

## Types

### `ICommandResponder` — interface, public

Single method; both parameters are passed by-value (`in`) to avoid allocation when the caller and reply are structs.

- **Methods:**
  - `Send(in CommandReply reply, in CommandCaller caller)` — Delivers `reply`. When `reply.Broadcast` is `false` the message is directed to `caller` only (using `caller.IdentityId` as the target); when `true` it goes to all connected players.

## Cross-references
- **Uses:** `PluginSdk/Commands/CommandReply.cs`, `PluginSdk/Commands/CommandCaller.cs`
- **Used by:** [ServerCommandResponder.cs](../../Legacy/Commands/ServerCommandResponder.cs.md), [CommandContext.cs](CommandContext.cs.md), [CommandDispatcher.cs](CommandDispatcher.cs.md), [CommandTests.cs](../../PluginSdkTests/CommandTests.cs.md)
