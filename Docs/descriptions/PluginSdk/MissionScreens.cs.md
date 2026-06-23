# PluginSdk/MissionScreens.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk` · **Kind:** static facade · **Lines:** 98

## Summary
`MissionScreens` is the plugin-facing API for showing Space Engineers mission-screen popups on clients. Plugins call this facade; the Magnetar host binds the actual sender at runtime.

## Types
### MissionScreens — static class, public
Facade over host-provided delegates for target-player, target-Steam-id, and broadcast sends.

- **Constants:** `ChannelId`, `ProtocolVersion`, and `ShowMissionScreenPacket`, shared with the client receiver mod.
- **Property:** `IsHostSenderAvailable` — true once the launcher binds at least one sender delegate.
- **Methods:**
  - `ShowToPlayer(playerIdentityId, ...)` / `ShowToPlayer(playerIdentityId, content)` — sends to one player identity.
  - `ShowToSteam(steamId, ...)` / `ShowToSteam(steamId, content)` — sends to one Steam id.
  - `ShowToAll(...)` / `ShowToAll(content)` — broadcasts to all online players.
  - `Bind(showToPlayer, showToSteam, showToAll)` — internal host hook that installs sender delegates; exposed to Magnetar launchers via `InternalsVisibleTo`.

## Cross-references
- **Uses:** [MissionScreenContent.cs](MissionScreenContent.cs.md); `InternalsVisibleTo` for `MagnetarInterim`, `MagnetarLegacy`, and tests.
- **Used by:** [MissionScreenSender.cs](../Legacy/Integration/MissionScreenSender.cs.md), [PluginLoader.cs](../Legacy/Loader/PluginLoader.cs.md)
