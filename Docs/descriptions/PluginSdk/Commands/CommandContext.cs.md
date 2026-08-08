# PluginSdk/Commands/CommandContext.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** sealed class · **Lines:** 55

## Summary
`CommandContext` is the per-invocation environment that a command handler accesses through `CommandModule.Context`. It bundles the caller identity, the argument list, and three overloads of `Respond` that cover the most common reply patterns (plain text, coloured text, full `CommandReply`). A fresh instance is constructed by `CommandDispatcher.ExecuteCommand` for every invocation, keeping handler state isolated. It holds an `ICommandResponder` reference so the handler never needs to know whether replies go to real game chat or a test capture sink.

## Types

### `CommandContext` — sealed class, public

Handlers access this object through the inherited `CommandModule.Context` property. The `Respond` overloads produce `CommandReply` values stamped with the root's `Prefix` as the author, so replies from the same module always share the same chat sender label. The `VRageMath.Color` and `VRage.Game.MyFontEnum` types used in overloads are SE DS types.

- **Fields:** `responder` (private `ICommandResponder`) — the sink that routes replies to game chat.
- **Properties:**
  - `Caller` — `CommandCaller` identifying who issued the command.
  - `Prefix` — The matched root prefix (e.g., `"ess"`); used as the default author label for all replies.
  - `Args` — `IReadOnlyList<string>` of argument words remaining after the command path was consumed.
  - `RawArgs` — `Args` joined by single spaces, for handlers that need the raw tail text.
- **Methods:**
  - `Respond(string text)` — Sends a plain white reply to the caller.
  - `Respond(string text, Color color, string font)` — Sends a coloured reply to the caller.
  - `Respond(in CommandReply reply)` — Sends a fully specified reply; fills `Author` from `Prefix` when the reply's `Author` is `null`.

## Cross-references
- **Uses:** `PluginSdk/Commands/CommandCaller.cs`, `PluginSdk/Commands/CommandReply.cs`, `PluginSdk/Commands/ICommandResponder.cs`, `VRage.Game.MyFontEnum`, `VRageMath.Color` (SE DS assemblies)
- **Used by:** [MagnetarCommands.cs](../../Legacy/Commands/MagnetarCommands.cs.md), [CommandDispatcher.cs](CommandDispatcher.cs.md), [CommandModule.cs](CommandModule.cs.md), [RegisteredCommand.cs](RegisteredCommand.cs.md)
