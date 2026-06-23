# Legacy/Patch/Patch_ServerChat.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Patch` · **Kind:** static class, public · **Lines:** 56

## Summary
Prefix-patches `MyMultiplayerBase.OnChatMessageReceived_Server` to intercept player-typed chat before SE relays it. When a message on a player-typed channel (Global, Faction or Private) begins with `'!'` and is handled by a registered command root, the original handler is skipped so the command text is never relayed to other players or written to the chat log. Scripted/system channels (GlobalScripted, ChatBot, BroadcastController) are deliberately passed through untouched.

## Types

### Patch_ServerChat — static class, public
Harmony Prefix on `Sandbox.Engine.Multiplayer.MyMultiplayerBase.OnChatMessageReceived_Server(ChatMsg)` (the overload taking a single `ChatMsg`, pinned via `[HarmonyPatch([typeof(ChatMsg)])]`), applied in the `"Late"` patch category. The prefix:

1. Returns `true` (lets SE handle normally) unless the channel is `ChatChannel.Global`, `ChatChannel.Faction`, or `ChatChannel.Private`.
2. Returns `true` if the text is null/empty or does not start with `'!'`.
3. Returns `true` if `PluginLoader.Instance?.Commands` is null (no command service active).
4. Returns `true` if the sender Steam ID (`MyEventContext.Current.Sender.Value`) is `0` (server-internal or unauthenticated message).
5. Calls `CommandService.HandleChat(sender, text)`; returns `false` to suppress the original broadcast when the command was handled, otherwise `true`.

All exceptions are caught, logged to `LogFile.Error`, and the original method is allowed to proceed (returns `true`) to avoid killing the multiplayer subsystem on a handler bug.

- **Methods:** `Prefix(ChatMsg msg) — Harmony Prefix; routes '!'-prefixed Global/Faction/Private chat through CommandService; returns false when a command is handled`

## Cross-references
- **Uses:** `Legacy/Loader/PluginLoader.cs` (`PluginLoader.Instance.Commands`), `Legacy/Commands/CommandService.cs` (`CommandService.HandleChat`), `Shared/LogFile.cs`, `Sandbox.Engine.Multiplayer.MyMultiplayerBase.OnChatMessageReceived_Server` (patched target), `VRage.Network.MyEventContext.Current.Sender`, `Sandbox.Game.Gui.ChatChannel`
- **Used by:** _none within the repository_
