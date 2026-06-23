# PluginSdk/MissionScreens.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk` · **Kind:** static class · **Lines:** 95

## Summary

Plugin-facing facade for opening Space Engineers mission-screen popups on connected clients from server-side plugin code, decoupled from the host launcher implementation. The dedicated-server host binds real sender delegates at startup via the internal `Bind` method; before binding (e.g. in unit tests) all calls are safe no-ops that return `false`. The host serializes the `MissionScreenContent` payload and sends it to the bundled MagnetarMod client receiver over a dedicated network channel; if a client does not have the MagnetarMod receiver enabled in the world, the game silently drops the packet. The file also publishes the channel/protocol constants shared between the server sender and the client receiver. Access to the internal `Bind` member is restricted to `MagnetarInterim`, `MagnetarLegacy`, and `PluginSdkTests` via `InternalsVisibleTo`.

## Types

### `MissionScreens` — static class, public

Plugin-facing facade for showing mission-screen popups to a single player (by identity id or Steam id) or to all players. Internally holds three private delegate fields (`Func<…, MissionScreenContent, bool>`), each initialized to a safe no-op lambda returning `false`. `Bind` replaces all three (null-safe). Each public targeting method validates its arguments (non-zero id, `content.HasContent`) before delegating to the corresponding field; the string-parameter overloads build a `MissionScreenContent` and forward to the struct overload.

- **Fields:**
  - `ChannelId` (`const ushort` = `48731`) — network channel id shared between the server sender and the MagnetarMod client receiver
  - `ProtocolVersion` (`const byte` = `1`) — payload protocol version for forward compatibility
  - `ShowMissionScreenPacket` (`const byte` = `1`) — packet-type discriminator for the show-mission-screen message
  - `showToPlayer` (`Func<long, MissionScreenContent, bool>`, private static) — invoked by `ShowToPlayer`; no-op returns `false` until bound
  - `showToSteam` (`Func<ulong, MissionScreenContent, bool>`, private static) — invoked by `ShowToSteam`; no-op returns `false` until bound
  - `showToAll` (`Func<MissionScreenContent, bool>`, private static) — invoked by `ShowToAll`; no-op returns `false` until bound
- **Properties:**
  - `IsHostSenderAvailable` (`bool`, public static, private setter) — `true` once the host has installed a server-side sender; does not prove the client has the MagnetarMod receiver enabled in the world
- **Methods:**
  - `ShowToPlayer(long, string, string, string, string, string = null) → bool` — convenience overload that builds a `MissionScreenContent` from the title/objective-prefix/objective/description/OK-caption strings and forwards to the struct overload
  - `ShowToPlayer(long playerIdentityId, MissionScreenContent content) → bool` — sends to one player by identity id; returns `false` if the id is `0`, the content is empty, or the host is unbound
  - `ShowToSteam(ulong, string, string, string, string, string = null) → bool` — string-parameter convenience overload forwarding to the struct overload
  - `ShowToSteam(ulong steamId, MissionScreenContent content) → bool` — sends to one player by Steam id; returns `false` if the id is `0`, the content is empty, or the host is unbound
  - `ShowToAll(string, string, string, string, string = null) → bool` — string-parameter convenience overload forwarding to the struct overload
  - `ShowToAll(MissionScreenContent content) → bool` — broadcasts to all connected players; returns `false` if the content is empty or the host is unbound
  - `Bind(Func<long, MissionScreenContent, bool>, Func<ulong, MissionScreenContent, bool>, Func<MissionScreenContent, bool>)` (internal static) — host-only; installs the three real sender delegates, falling back to the no-op defaults for null arguments, and sets `IsHostSenderAvailable` when at least one non-null delegate was supplied; called once at launcher startup

## Cross-references

- **Uses:** `PluginSdk/MissionScreenContent.cs` (the payload struct); the MagnetarMod client receiver (out-of-repo client mod that consumes the channel/protocol constants and renders the SE mission-screen popup)
- **Used by:** [MissionScreenSender.cs](../Legacy/Integration/MissionScreenSender.cs.md), [PluginLoader.cs](../Legacy/Loader/PluginLoader.cs.md)
