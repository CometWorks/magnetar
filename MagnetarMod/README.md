# MagnetarMod

Magnetar's companion world mod provides mission-screen popups and the **Magnetar Player Teleporter** for Space Engineers 1. The game loads the definitions, models and scripts from [src](src). For local testing, install that directory as a named local mod and enable it in an offline world. Magnetar normally adds the released companion to the world by Workshop ID. An unpublished checkout must be staged on both server and clients for multiplayer testing.

Server plugins can use `PluginSdk.MissionScreens` to open a player's local mission screen. The existing secure channel for that feature is `48731`.

## Place and link teleporter pads

1. In the large-grid G menu, search for **Magnetar Player Teleporter**. Place and finish two pads with their bases mounted to a floor. Each occupies one large-grid cell. Give travelers terminal access to both pads. Leave room for a standing engineer above each deck.
2. Step onto each unlinked boarding disc to display its block entity ID. A block keeps this ID across world saves. Copies, rebuilds and pasted blueprints receive new IDs.
3. Edit `Storage/<mod script scope>/TeleporterRoutes.xml` inside the **server world** while it is stopped, then start the world. The mod creates an empty file on first load. The implicit companion's scope is normally `3750200326.sbm_MagnetarMod`. A local mod folder gives it a different prefix.
4. Enter one directed route for each departure pad. Substitute the IDs from your own world. This example permits the outbound trip for anyone with block access and reserves the return trip for administrators.

```xml
<Routing>
  <Routes>
    <Route>
      <From>141787265624111434</From>
      <To>115692577277107903</To>
    </Route>
    <Route>
      <From>115692577277107903</From>
      <To>141787265624111434</To>
      <AdminOnly>true</AdminOnly>
    </Route>
  </Routes>
</Routing>
```

An empty route table leaves the blocks placeable and unlinked. Duplicate departures, zero IDs, self-links and unreadable configuration disable routing for that load and write a server log entry. Stepping onto a linked disc starts a ten-second HUD countdown. Stay on the disc to depart automatically or step off to cancel. Boarding again restarts the full countdown. After arrival, step off the destination before boarding it for a return trip. A failed departure also requires a fresh boarding attempt. Both endpoints must be working. The destination must have enough free space for that character. The transfer keeps the traveler's facing and accounts for the destination grid's velocity.

## Server integration

The server observes connected players stepping onto pads and runs the countdown for 600 simulation ticks. It checks the same living, controlled character on the source throughout boarding and checks route access at boarding and departure. At zero, a remote traveler’s client receives a one-use ticket on secure channel `48732` and returns it only while the engineer is still on that pad. The ticket expires after five simulation seconds. The server accepts it only from the boarding player for the current visit, rechecks access and destination clearance, then moves the character within that world. A departed client cannot initiate travel while the transport waits to detect its disconnect. A local host goes through the same departure checks directly.

A trusted server plugin or world script can subscribe with `MyAPIGateway.Utilities.RegisterMessageHandler(0x4D47545000000001L, handler)` after ModAPI initialization. Unregister the same delegate at world unload. The payload is a boxed `VRage.MyTuple<ulong, long, long, long>`.

| Member | Value |
| --- | --- |
| Item1 | Traveler's Steam ID |
| Item2 | Traveler's current identity ID |
| Item3 | Source block entity ID |
| Item4 | Destination block entity ID |

The mod emits this server-local event once for a completed trip after observing the same connected, living character within 0.35 m of the arrival point three simulation ticks after the move. The test uses destination-local coordinates so the confirmation follows the grid. A refused request or interrupted confirmation produces no completion. The event is transient and has no replay or persistence guarantee.

A cluster integration can map the confirmed directed route to a lobby-state transition. It owns the player's durable first-visit record and current lobby status separately, their initialization after a confirmed first lobby spawn, and any cross-node arrival acknowledgment and recovery. Picker suppression, partition placement, bubble enforcement, flags and rules also belong to the cluster. Stepping onto a pad, countdown ticks, cancellations, border crossings and disconnects never write lobby state in this mod.

## Development and verification

Build the mod project directly with `dotnet build MagnetarMod/MagnetarMod.csproj -c Release` from the repository root. The project targets .NET Framework 4.8 and C# 6 with MDK2 references and analyzers. The current solution, `Magnetar.slnx`, omits the mod project. Space Engineers compiles the world scripts on load.

The maintained [Blender source](assets/teleporter/PlayerTeleporter.blend) contains the finished mesh, a weld stage, an original convex standing surface and the orthographic icon camera. The meshes use native SEUT materials. The model is Z-up in Blender, Y-up in the game, and centered on a 2.5 m cell. The deck is 0.304 m above the cell bottom. On another workstation, relink the material library to the installed SEUT `Materials/Materials.blend` if necessary.

From the repository root, run the following with Blender, SEUT, Havok Content Tools and MWMBuilder configured. The tested Linux setup uses Blender 5.2.1 and the Wine-enabled SEUT exporter. The [exporter](assets/teleporter/export.py) writes a fresh build under `MagnetarMod/obj/teleporter`, checks the native logs and embedded collision, then stages the FBX, SEUT XML and HKT next to the source and the runtime MWMs under `src/Models/Cubes/large`. Both runtime models use the same collision.

```sh
blender --background MagnetarMod/assets/teleporter/PlayerTeleporter.blend \
  --python-exit-code 1 --python MagnetarMod/assets/teleporter/export.py
```

See the [in-game verification record](validation/Teleporter.md) for installation, interaction and multiplayer results.

## Workshop authentication

The **Upload MagnetarMod to Steam Workshop** workflow uses `STEAM_USERNAME` and the raw contents of the publishing account’s SteamCMD `config.vdf`. After logging in and quitting SteamCMD, upload the saved file directly from its Steam home. For example, run `gh secret set STEAM_CONFIG_VDF --repo CometWorks/magnetar < /path/to/Steam/config/config.vdf` with the actual path.

Select **Verify Steam login without publishing** when dispatching the workflow to test a new secret. A successful check authenticates on a GitHub-hosted runner and skips staging and publication. Leave the option unchecked to upload the mod.
