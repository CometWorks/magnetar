# Module: MagnetarMod

**Project:** `MagnetarMod` · **Files:** 1 · **Source lines:** 114

## Purpose

The companion in-game Space Engineers world mod (a Data/Scripts mod) bundled with Magnetar. Its session component receives server-pushed content over a dedicated secure multiplayer channel and renders it on connected clients, currently mission-screen popups.

## Role in Magnetar

Client-side counterpart to the server-side mission-screen path: PluginSdk.MissionScreens (the plugin-facing facade) and Legacy.Integration.MissionScreenSender (the host sender) serialize and push payloads, and this mod's MagnetarModSession deserializes them and calls the SE ModAPI to show the popup. It shares the same channel id (48731), protocol version, packet-type discriminator, and five-string field layout as the server side.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `MagnetarModSession` | sealed class (MySessionComponentBase, NoUpdate session component) | [`MagnetarMod/src/Data/Scripts/MagnetarMod/MagnetarModSession.cs`](../descriptions/MagnetarMod/src/Data/Scripts/MagnetarMod/MagnetarModSession.cs.md) | Registers a secure message handler on channel 48731, deserializes versioned show-mission-screen frames from the server, and renders them via MyAPIGateway.Utilities.ShowMissionScreen on the game thread. |
| `MagnetarModSession.MissionScreenPacket` | sealed class (private nested data holder) | [`MagnetarMod/src/Data/Scripts/MagnetarMod/MagnetarModSession.cs`](../descriptions/MagnetarMod/src/Data/Scripts/MagnetarMod/MagnetarModSession.cs.md) | Mutable holder for the five deserialized payload strings (title, objective prefix, objective, description, OK-button caption), mirroring PluginSdk.MissionScreenContent. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`MagnetarMod/src/Data/Scripts/MagnetarMod/MagnetarModSession.cs`](../descriptions/MagnetarMod/src/Data/Scripts/MagnetarMod/MagnetarModSession.cs.md) | 114 | Client-side Space Engineers world-mod session component that receives server-pushed mission-screen popups and renders them through the SE ModAPI. |

## Public API surface

- `MagnetarMod.MagnetarModSession (public sealed MySessionComponentBase; entry point invoked by the SE engine, no callable plugin API)`
- `MagnetarModSession.LoadData() (public override)`
- `MagnetarModSession.UnloadData() (protected override)`

## Dependencies

**Uses modules:** _none_  
**Used by modules:** _none_  
**External systems:** SE ModAPI / VRage

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
