# PluginSdk/Commands/CommandReply.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** readonly struct · **Lines:** 70

## Summary
`CommandReply` is the value type that carries a fully-specified chat message from a command handler back to the host's `ICommandResponder`. It encapsulates the text, an optional `VRageMath.Color`, a `VRage.Game.MyFontEnum` font name, an optional author label (shown as the chat sender), and a broadcast flag. Factory methods (`Ok`, `Info`, `Error`, `Announce`) cover the common cases; fluent `With*` and `AsBroadcast` methods let handlers compose custom replies without mutating state.

## Types

### `CommandReply` — readonly struct, public

Immutable by design. `HasContent` guards against sending empty strings to the game's chat API. Handlers may return a `CommandReply` directly from a `[Command]`-decorated method; `CommandDispatcher.DispatchResult` handles the routing. `Broadcast = true` causes `ICommandResponder.Send` to deliver the message to all connected players rather than only the caller.

- **Properties:**
  - `Text` — The message body.
  - `Color` — Optional `VRageMath.Color`; `null` means use the font's default colour.
  - `Font` — `MyFontEnum` string constant.
  - `Author` — Chat sender label; `null` means use the command root's title.
  - `Broadcast` — `true` sends to all players.
  - `HasContent` — `true` when `Text` is non-null and non-empty.
- **Methods (static factories):**
  - `None` — Empty reply that sends nothing.
  - `Ok(string text)` — White (`MyFontEnum.White`) private reply.
  - `Info(string text)` — Blue (`MyFontEnum.Blue`) private reply.
  - `Error(string text)` — Red (`MyFontEnum.Red`) private reply.
  - `Announce(string text, Color? color)` — White broadcast to all players.
- **Methods (fluent):**
  - `WithColor(Color)`, `WithFont(string)`, `WithAuthor(string)`, `AsBroadcast(bool)` — Return new instances with the respective field changed.

## Cross-references
- **Uses:** `VRage.Game.MyFontEnum`, `VRageMath.Color` (SE DS assemblies)
- **Used by:** [MagnetarCommands.cs](../../Legacy/Commands/MagnetarCommands.cs.md), [ServerCommandResponder.cs](../../Legacy/Commands/ServerCommandResponder.cs.md), [CommandContext.cs](CommandContext.cs.md), [CommandDispatcher.cs](CommandDispatcher.cs.md), [ICommandResponder.cs](ICommandResponder.cs.md), [CommandTests.cs](../../PluginSdkTests/CommandTests.cs.md)
