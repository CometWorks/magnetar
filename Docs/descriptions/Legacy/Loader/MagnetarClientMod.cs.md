# Legacy/Loader/MagnetarClientMod.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Loader` · **Kind:** static class · **Lines:** 128

## Summary
Manages the bundled **MagnetarMod** client companion world mod (Steam workshop id `3750200326`). It adds the receiver only when implicit mods are enabled, crossplay is off, and SE has a Steam Workshop service. If Steam UGC is unavailable it removes/skips the companion, warns once, and allows startup to continue with mission-screen popups unavailable.

## Types

### MagnetarClientMod — static class, internal
Centralizes the policy for whether the implicit MagnetarMod client mod participates, and the mechanics of injecting it into a workshop-id set or a checkpoint mod list. It never throws on null inputs and is purely additive/subtractive over the collections it is given.

- **Fields:**
  - `WorkshopId` (public const `ulong`) — the MagnetarMod Steam workshop id (`3750200326`).
  - `WorkshopService` (private const `string`) — `"Steam"`, the service used when constructing the `ModItem`.
  - `steamUnavailableWarningLogged` (private static bool) — suppresses duplicate missing-service warnings across load patches.
- **Methods:**
  - `GetWorkshopIdsForUpdate(IEnumerable<ulong> configuredIds)` — Returns a null-safe ID set. Removes MagnetarMod when disabled, under crossplay, or when Steam Workshop is unavailable; otherwise adds it for prefetch.
  - `ApplyToCheckpoint(MyObjectBuilder_Checkpoint checkpoint)` — Null-safe entry point that forwards `checkpoint.Mods` (by ref) to `ApplyToModList`.
  - `ApplyToModList(ref List<MyObjectBuilder_Checkpoint.ModItem> mods)` — Allocates the list if null, then delegates to the by-value overload.
  - `ApplyToModList(List<MyObjectBuilder_Checkpoint.ModItem> mods)` — Removes MagnetarMod when disabled, under crossplay, or without Steam Workshop; otherwise adds it once. No-op on null.
  - `IsCrossplayEnabled()` (private) — True when `MySandboxGame.ConfigDedicated` reports `CrossPlatform`, `ConsoleCompatibility`, or a `NetworkType` equal to `"eos"` (case-insensitive); false when no dedicated config is present.
  - `WarnSteamUnavailable()` (private) — Warns once that the companion and mission-screen popups are unavailable while allowing startup to continue.
  - `IsMagnetarMod(MyObjectBuilder_Checkpoint.ModItem mod)` (private) — Predicate matching `mod.PublishedFileId == WorkshopId`.
  - `CreateModItem()` (private) — Builds the `ModItem(WorkshopId, "Steam")` with `FriendlyName = "MagnetarMod"`.

## Cross-references
- **Uses:** `Pulsar.Shared.LogFile`; `Flags.NoImplicitMod`; `SteamMods.IsSteamWorkshopAvailable`; SE DS `MySandboxGame.ConfigDedicated` and `MyObjectBuilder_Checkpoint.ModItem`.
- **Used by:** [MissionScreenSender.cs](../Integration/MissionScreenSender.cs.md), [PluginLoader.cs](PluginLoader.cs.md), [Patch_MyDefinitionManager.cs](../Patch/Patch_MyDefinitionManager.cs.md), [Patch_MySessionLoader.cs](../Patch/Patch_MySessionLoader.cs.md), [Patch_MyWorkshop.cs](../Patch/Patch_MyWorkshop.cs.md)
