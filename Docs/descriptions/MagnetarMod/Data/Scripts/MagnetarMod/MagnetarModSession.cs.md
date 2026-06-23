# MagnetarMod/Data/Scripts/MagnetarMod/MagnetarModSession.cs

**Project:** MagnetarMod · **Namespace:** `MagnetarMod` · **Kind:** sealed class (SE session component) · **Lines:** 113

## Summary

Client-side Space Engineers world-mod session component that receives server-pushed mission-screen popups and renders them through the SE ModAPI. Decorated with `[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]` and derived from `MySessionComponentBase`, it registers a secure multiplayer message handler on channel `48731` via `MyAPIGateway.Multiplayer.RegisterSecureMessageHandler` during `LoadData` and unregisters it in `UnloadData`. Incoming server messages are accepted only when sent from the server, deserialized from a versioned binary frame (protocol version `1`, packet type `1` followed by five UTF-8 length-prefixed strings), then displayed by marshalling onto the game thread (`MyAPIGateway.Utilities.InvokeOnGameThread`) and calling `MyAPIGateway.Utilities.ShowMissionScreen`. It is the in-game receiver counterpart to the server-side `PluginSdk.MissionScreens` facade and the `Pulsar.Legacy.Integration.MissionScreenSender` host sender, sharing the same channel id, protocol version, and packet-type/string-field layout.

## Types

### `MagnetarModSession` — sealed class, public : `MySessionComponentBase`

The mod's single session component, attributed `[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]` (no per-tick update). It guards every ModAPI access against null `MyAPIGateway.Multiplayer`/`MyAPIGateway.Utilities` so it is inert in non-multiplayer or partially-initialized contexts, and tracks whether the message handler was registered so `UnloadData` only unregisters when needed.

- **Fields:**
  - `ChannelId` (`const ushort` = `48731`, private) — secure-message channel id, matching `PluginSdk.MissionScreens.ChannelId`
  - `ProtocolVersion` (`const byte` = `1`, private) — expected payload protocol version; non-matching frames are rejected
  - `ShowMissionScreenPacket` (`const byte` = `1`, private) — expected packet-type discriminator; non-matching frames are rejected
  - `registered` (`bool`, private) — tracks whether the secure message handler is currently registered, so `UnloadData` unregisters at most once
- **Methods:**
  - `LoadData() → void` (public override) — returns early if `MyAPIGateway.Multiplayer` is null; otherwise registers `OnMessage` as the secure message handler for `ChannelId` and sets `registered`
  - `UnloadData() → void` (protected override) — returns early unless `registered` and `MyAPIGateway.Multiplayer` is non-null; otherwise unregisters `OnMessage` from `ChannelId` and clears `registered`
  - `OnMessage(ushort handlerId, byte[] data, ulong sender, bool sentFromServer) → void` (private) — secure-message callback; ignores messages not `sentFromServer`, with null/empty `data`, that fail to deserialize, or when `MyAPIGateway.Utilities` is null; otherwise marshals onto the game thread via `InvokeOnGameThread` (label `"MagnetarMod.ShowMissionScreen"`) and calls `ShowMissionScreen` with the packet's title, objective prefix, objective, description, a null screenshot, and OK-button caption (each passed through `EmptyToNull`)
  - `TryDeserialize(byte[] data, out MissionScreenPacket packet) → bool` (private static) — reads the binary frame with a `MemoryStream`/`BinaryReader` (UTF-8); returns `false` if the version or packet-type byte mismatches; on success populates the five string fields in order (title, objective prefix, objective, description, OK caption); on exception logs `"[MagnetarMod] Failed to decode mission screen packet: …"` via `MyLog.Default.WriteLine` and returns `false`
  - `EmptyToNull(string value) → string` (private static) — returns `null` for null/empty input, otherwise the value, so blank fields are passed to `ShowMissionScreen` as `null`

### `MissionScreenPacket` — sealed class, private (nested)

Plain mutable data holder for the deserialized show-mission-screen payload; its five string fields mirror `PluginSdk.MissionScreenContent` and the host sender's wire order.

- **Fields:**
  - `ScreenTitle` (`string`, public) — mission-screen title
  - `CurrentObjectivePrefix` (`string`, public) — objective label prefix
  - `CurrentObjective` (`string`, public) — current objective text
  - `ScreenDescription` (`string`, public) — main description body
  - `OkButtonCaption` (`string`, public) — caption for the dismiss/OK button

## Cross-references

- **Uses:** SE ModAPI / VRage (`Sandbox.ModAPI.MyAPIGateway.Multiplayer` secure message handlers, `MyAPIGateway.Utilities.InvokeOnGameThread`/`ShowMissionScreen`, `VRage.Game.Components.MySessionComponentBase`, `MyUpdateOrder`, `VRage.Utils.MyLog`); shares the channel/protocol/packet constants and string layout with `PluginSdk/MissionScreens.cs` and `PluginSdk/MissionScreenContent.cs`; consumes the binary frames produced by `Legacy/Integration/MissionScreenSender.cs`
- **Used by:** _none within the repository_
