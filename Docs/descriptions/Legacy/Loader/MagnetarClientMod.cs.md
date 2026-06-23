# Legacy/Loader/MagnetarClientMod.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Loader` · **Kind:** static class · **Lines:** 102

## Summary
Manages the bundled **MagnetarMod** client companion world mod (Steam workshop id `3750200326`), the script-side counterpart clients must load so that plugin-driven mission-screen popups have receiving code. Provides helpers to ensure the mod is present in the set of workshop ids prefetched for download and in a world's mod list, while honouring opt-out conditions: the `-noimplicitmod` flag removes it, and any crossplay configuration (cross-platform, console compatibility, or an EOS network type — where Steam workshop mods cannot be used) also removes/skips it. All decisions are logged to `LogFile`.

## Types

### MagnetarClientMod — static class, internal
Centralizes the policy for whether the implicit MagnetarMod client mod participates, and the mechanics of injecting it into a workshop-id set or a checkpoint mod list. It never throws on null inputs and is purely additive/subtractive over the collections it is given.

- **Fields:**
  - `WorkshopId` (public const `ulong`) — the MagnetarMod Steam workshop id (`3750200326`).
  - `WorkshopService` (private const `string`) — `"Steam"`, the service used when constructing the `ModItem`.
- **Methods:**
  - `GetWorkshopIdsForUpdate(IEnumerable<ulong> configuredIds)` — Returns a `HashSet<ulong>` seeded from `configuredIds` (null-safe). If `Flags.NoImplicitMod` or crossplay is enabled, removes `WorkshopId`; otherwise adds it. Logs the chosen action. Called by `PluginLoader.Init` to compute the ids passed to `SteamMods.Update`.
  - `ApplyToCheckpoint(MyObjectBuilder_Checkpoint checkpoint)` — Null-safe entry point that forwards `checkpoint.Mods` (by ref) to `ApplyToModList`.
  - `ApplyToModList(ref List<MyObjectBuilder_Checkpoint.ModItem> mods)` — Allocates the list if null, then delegates to the by-value overload.
  - `ApplyToModList(List<MyObjectBuilder_Checkpoint.ModItem> mods)` — Core mutator. With `Flags.NoImplicitMod` or crossplay enabled, `RemoveAll(IsMagnetarMod)` and logs any removal. Otherwise adds a freshly created `ModItem` only if not already present. No-op on a null list.
  - `IsCrossplayEnabled()` (private) — True when `MySandboxGame.ConfigDedicated` reports `CrossPlatform`, `ConsoleCompatibility`, or a `NetworkType` equal to `"eos"` (case-insensitive); false when no dedicated config is present.
  - `IsMagnetarMod(MyObjectBuilder_Checkpoint.ModItem mod)` (private) — Predicate matching `mod.PublishedFileId == WorkshopId`.
  - `CreateModItem()` (private) — Builds the `ModItem(WorkshopId, "Steam")` with `FriendlyName = "MagnetarMod"`.

## Cross-references
- **Uses:** `Pulsar.Shared` (`LogFile`); `Pulsar.Shared.Config` (`Flags.NoImplicitMod`); SE DS `Sandbox` (`MySandboxGame.ConfigDedicated`), `VRage.Game` (`MyObjectBuilder_Checkpoint`, `MyObjectBuilder_Checkpoint.ModItem`).
- **Used by:** [MissionScreenSender.cs](../Integration/MissionScreenSender.cs.md), [PluginLoader.cs](PluginLoader.cs.md), [Patch_MyDefinitionManager.cs](../Patch/Patch_MyDefinitionManager.cs.md), [Patch_MySessionLoader.cs](../Patch/Patch_MySessionLoader.cs.md), [Patch_MyWorkshop.cs](../Patch/Patch_MyWorkshop.cs.md)
