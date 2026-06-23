# Legacy/Patch/Patch_MyWorkshop.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Patch` · **Kind:** Harmony patch · **Lines:** 27

## Summary
`Patch_MyWorkshop` intercepts SE's `MyWorkshop.DownloadWorldModsBlocking` path. Before download it ensures the Magnetar client companion mod is present in the world mod list; after a successful download it asks `SteamMods` to expand legacy Workshop archives before DS definition loading continues.

## Types
### Patch_MyWorkshop — static class, internal
Harmony patch in the `Early` category targeting `MyWorkshop.DownloadWorldModsBlocking`.

- **Methods:**
  - `Prefix(ref mods)` — calls `MagnetarClientMod.ApplyToModList` so the client companion mod is injected or removed according to runtime flags.
  - `Postfix(mods, __result)` — when the download result is OK, calls `SteamMods.RepairLegacyArchives(mods)` to expand any `*_legacy.bin` Workshop packages.

## Cross-references
- **Uses:** `MagnetarClientMod` and `SteamMods` (Legacy.Loader); SE DS assemblies `Sandbox.Engine.Networking.MyWorkshop`, `VRage.Game.MyObjectBuilder_Checkpoint.ModItem`, `VRage.GameServices.MyGameServiceCallResult`; Harmony.
- **Used by:** _none within the repository_
