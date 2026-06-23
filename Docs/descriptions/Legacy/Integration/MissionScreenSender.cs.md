# Legacy/Integration/MissionScreenSender.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Integration` · **Kind:** static class · **Lines:** 119

## Summary
Host-side sender that delivers plugin-declared mission-screen popups to clients over Space Engineers' multiplayer messaging API. It serializes a `MissionScreenContent` into a versioned binary packet and dispatches it via `MyAPIGateway.Multiplayer.SendMessageTo` on the dedicated server's game thread, addressed by Steam ID to a single player, all online players, or an identity resolved through `MySession.Static.Players`. Sending is gated on the bundled MagnetarMod world mod being enabled (the matching client-side receiver), detected by scanning `MySession.Static.Mods` for the known workshop id or a name containing `MagnetarMod`; without it the game silently drops the packet. This type is the host implementation that the PluginSdk `MissionScreens` facade binds to.

## Types
### `MissionScreenSender` — static class, internal
Internal launcher-side helper translating PluginSdk mission-screen requests into SE DS multiplayer packets. All public entry points validate input and the receiver-mod prerequisite, then marshal the actual `SendMessageTo` call onto the game thread via `LauncherGame.RunOnGameThread`. The wire format is a `BinaryWriter` (UTF-8) sequence beginning with `MissionScreens.ProtocolVersion` and `MissionScreens.ShowMissionScreenPacket`, followed by the five content strings (each null-coerced to empty) on `MissionScreens.ChannelId` (48731).
- **Methods:**
  - `ShowToPlayer(long identityId, MissionScreenContent content)` — public; resolves the player's Steam id from the identity via `MySession.Static.Players.TryGetSteamId` and delegates to `ShowToSteam`; returns `false` if the identity is `0` or no session is active.
  - `ShowToSteam(ulong steamId, MissionScreenContent content)` — public; sends to a single Steam id after validating non-zero id, `content.HasContent`, an active multiplayer/session, and `ReceiverModLoaded`; serializes once and queues `SendToSteamOnGameThread` on the game thread.
  - `ShowToAll(MissionScreenContent content)` — public; broadcasts to every online player from `MySession.Static.Players.GetOnlinePlayers()` (skipping null players and zero Steam ids), wrapped in try/catch that logs broadcast failures via `LogFile.Error`.
  - `SendToSteamOnGameThread(ulong steamId, byte[] payload)` — private; performs the reliable `MyAPIGateway.Multiplayer.SendMessageTo(MissionScreens.ChannelId, payload, steamId, true)` call, guarding against a null `Multiplayer` and logging per-recipient send failures.
  - `ReceiverModLoaded()` — private; returns `true` when any entry in `MySession.Static.Mods` matches `MagnetarClientMod.WorkshopId` or whose `Name`/`FriendlyName` contains `MagnetarMod`.
  - `ContainsModName(string value)` — private; case-insensitive substring check for `"MagnetarMod"`.
  - `Serialize(MissionScreenContent content)` — private; builds the binary packet (protocol version, packet type, then `ScreenTitle`, `CurrentObjectivePrefix`, `CurrentObjective`, `ScreenDescription`, `OkButtonCaption`).
  - `WriteString(BinaryWriter writer, string value)` — private; writes the string or `string.Empty` when null.

## Cross-references
- **Uses:** `PluginSdk/MissionScreens.cs` (channel/protocol/packet constants and the facade this implements), `PluginSdk/MissionScreenContent.cs` (payload struct), `Legacy/Loader/MagnetarClientMod.cs` (`WorkshopId` of the companion mod), `Pulsar.Legacy.Launcher.Game.RunOnGameThread` (game-thread marshalling), `Pulsar.Shared.LogFile` (error logging); SE DS APIs `Sandbox.ModAPI.MyAPIGateway.Multiplayer.SendMessageTo`, `Sandbox.Game.World.MySession.Static` (`Players`, `Mods`), `MyPlayer`, `VRage.Game.MyObjectBuilder_Checkpoint.ModItem`
- **Used by:** [PluginLoader.cs](../Loader/PluginLoader.cs.md)
