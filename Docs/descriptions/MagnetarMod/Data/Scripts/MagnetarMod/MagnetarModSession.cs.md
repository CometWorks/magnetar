# MagnetarMod/Data/Scripts/MagnetarMod/MagnetarModSession.cs

**Project:** MagnetarMod · **Namespace:** `MagnetarMod` · **Kind:** session component · **Lines:** 101

## Summary
`MagnetarModSession` is the client-side receiver for Magnetar mission-screen packets. It registers a secure multiplayer message handler, decodes the PluginSdk mission-screen protocol, and opens Space Engineers' mission-screen popup on the client game thread.

## Types
### MagnetarModSession — class, public sealed : `MySessionComponentBase`
Session component with `NoUpdate` order.

- **Constants:** `ChannelId`, `ProtocolVersion`, and `ShowMissionScreenPacket`, matching `PluginSdk.MissionScreens`.
- **Fields:** `registered` tracks whether the secure message handler was installed.
- **Methods:**
  - `LoadData()` — registers `OnMessage` when multiplayer API is available.
  - `UnloadData()` — unregisters the handler.
  - `OnMessage(handlerId, data, sender, sentFromServer)` — accepts only server-origin packets, deserializes them, and invokes `ShowMissionScreen` on the game thread.
  - `TryDeserialize(data, out packet)` — validates protocol version/type and reads all string fields.
  - `EmptyToNull(value)` — converts empty strings to null for the game UI call.

### MissionScreenPacket — class, private sealed
Mutable DTO used only during deserialization.

## Cross-references
- **Uses:** Space Engineers mod API `MyAPIGateway`, `MySessionComponentBase`, `MySessionComponentDescriptor`; `MyLog` for decode errors; BCL binary IO.
- **Used by:** _none within the repository_
