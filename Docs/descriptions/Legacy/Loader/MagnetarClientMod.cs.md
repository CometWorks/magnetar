# Legacy/Loader/MagnetarClientMod.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Loader` · **Kind:** static helper · **Lines:** 100

## Summary
`MagnetarClientMod` manages the implicit Steam Workshop client companion mod that lets server-side PluginSdk features show mission-screen popups on clients. It adds the mod for ordinary Steam worlds, removes it when disabled, and skips it for crossplay/EOS-compatible worlds.

## Types
### MagnetarClientMod — static class, internal
Centralizes the Workshop id and all world-mod-list mutations for the client companion.

- **Constants:** `WorkshopId` (`3750200326`) and Workshop service `"Steam"`.
- **Methods:**
  - `GetWorkshopIdsForUpdate(configuredIds)` — returns the set Magnetar should pre-download, including the companion unless `-noimplicitmod` or crossplay prevents it.
  - `ApplyToCheckpoint(checkpoint)` — applies companion logic to a checkpoint's mod list.
  - `ApplyToModList(ref mods)` / `ApplyToModList(mods)` — adds, removes, or leaves the companion mod based on flags and dedicated config.
  - `IsCrossplayEnabled()` — checks `CrossPlatform`, `ConsoleCompatibility`, and EOS network type.
  - `IsMagnetarMod(mod)` — matches by Workshop id.
  - `CreateModItem()` — builds the `ModItem` with friendly name `MagnetarMod`.

## Cross-references
- **Uses:** `Flags`, `LogFile`, `MySandboxGame.ConfigDedicated`, `MyObjectBuilder_Checkpoint`.
- **Used by:** [MissionScreenSender.cs](../Integration/MissionScreenSender.cs.md), [PluginLoader.cs](PluginLoader.cs.md), [Patch_MyDefinitionManager.cs](../Patch/Patch_MyDefinitionManager.cs.md), [Patch_MySessionLoader.cs](../Patch/Patch_MySessionLoader.cs.md), [Patch_MyWorkshop.cs](../Patch/Patch_MyWorkshop.cs.md)
