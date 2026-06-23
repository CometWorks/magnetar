# Legacy/Integration/MissionScreenSender.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Integration` · **Kind:** static helper · **Lines:** 122

## Summary
`MissionScreenSender` is the dedicated-server side of the PluginSdk mission-screen facade. It serializes `MissionScreenContent` payloads and sends them over a secure multiplayer channel to the bundled `MagnetarMod` client receiver.

## Types
### MissionScreenSender — static class, internal
Sends mission-screen packets to one player, one Steam id, or all online players.

- **Methods:**
  - `ShowToPlayer(identityId, content)` — resolves the player's Steam id from `MySession.Static.Players` and delegates to `ShowToSteam`.
  - `ShowToSteam(steamId, content)` — validates content/session state and receiver mod presence, serializes the payload, and sends it on the game thread.
  - `ShowToAll(content)` — serializes once and sends to every online player with a Steam id.
  - `SendToSteamOnGameThread(steamId, payload)` — calls `MyAPIGateway.Multiplayer.SendMessageTo`.
  - `ReceiverModLoaded()` / `ContainsModName(value)` — detect `MagnetarMod` in the active world mod list.
  - `Serialize(content)` / `WriteString(writer, value)` — binary protocol writer for the client receiver.

## Cross-references
- **Uses:** `PluginSdk.MissionScreens`, `PluginSdk.MissionScreenContent`, `MagnetarClientMod`, `LogFile`, `Launcher.Game.RunOnGameThread`; SE APIs `MySession`, `MyAPIGateway`, `MyObjectBuilder_Checkpoint.ModItem`.
- **Used by:** [PluginLoader.cs](../Loader/PluginLoader.cs.md)
