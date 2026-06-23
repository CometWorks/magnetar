# Legacy/Loader/SteamMods.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Loader` · **Kind:** static class · **Lines:** 120

## Summary
Downloads/updates Steam Workshop items (mod-plugins referenced by the active profile) by reproducing SE's own blocking workshop-download path. Because the real `MyWorkshop.DownloadModsBlocking` is non-public, this class invokes it via Harmony `AccessTools` reflection, running it on a `ParallelTasks` task while pumping `MyGameService.Update()` on the calling thread so the Steam callbacks fire and the download completes. It mirrors the internals of `MyWorkshop.DownloadWorldModsBlocking` / `...Internal`, repairs legacy `*_legacy.bin` archives after successful downloads, and exposes a helper to test whether a mod is untrusted (not Steam-published or not installed).

## Types

### SteamMods — static class, public
Reflection-bridged wrapper over SE's internal Steam workshop downloader. Exists so Magnetar can prefetch mod-plugin workshop content at server init without a public SE API.

- **Fields:**
  - `DownloadModsBlocking` (static `MethodInfo`) — lazily-resolved cache of the non-public `MyWorkshop.DownloadModsBlocking` method.
- **Methods:**
  - `Update(IEnumerable<ulong> ids)` — Wraps each workshop id in a `MyObjectBuilder_Checkpoint.ModItem(id, "Steam")`; returns early if empty. Starts `UpdateInternal` on a `Parallel.Start` task and busy-waits, calling `MyGameService.Update()` and `Thread.Sleep(10)` until the task completes (so Steam callbacks are serviced). If the result is not `MyGameServiceCallResult.OK`, logs the task's exceptions (or the result code) via `LogFile.Error`. Modeled on `MyWorkshop.DownloadWorldModsBlocking`.
  - `IsModUntrusted(MyObjectBuilder_Checkpoint.ModItem mod)` — Returns true if the mod's `PublishedServiceName` is not `"Steam"` or `Steam.IsItemInstalled(mod.PublishedFileId)` is false.
  - `UpdateInternal(List<MyObjectBuilder_Checkpoint.ModItem> mods)` — Mirrors `MyWorkshop.DownloadWorldModsBlockingInternal`: increases the SE log indent, builds the `WorkshopId` list, lazily resolves `DownloadModsBlocking` via `AccessTools.Method`, invokes it (passing the mods list, a fresh `ResultData`, the `WorkshopId` list and a `CancelToken`), repairs legacy archives when the result is OK, decreases the indent, and returns the `MyWorkshop.ResultData`.
  - `RepairLegacyArchives(IEnumerable<MyObjectBuilder_Checkpoint.ModItem> mods)` — Iterates downloaded mods whose `ModItem` has `MyWorkshopItem` data, takes the item's `Folder`, and calls `LegacyWorkshopArchive.TryRepair` so early Workshop packages with `*_legacy.bin` are expanded before definitions/scripts load. Logs and continues if one mod cannot be checked.

## Cross-references
- **Uses:** `Pulsar.Shared` (`LogFile`), `Pulsar.Shared.Data.LegacyWorkshopArchive`, `Pulsar.Legacy.Loader`/`Steam` helper (`Steam.IsItemInstalled`); SE DS assemblies: `Sandbox.Engine.Networking` (`MyWorkshop`, `MyGameService`), `VRage.Game` (`MyObjectBuilder_Checkpoint.ModItem`), `VRage.Utils` (`MyLog`), `VRage.GameServices` (`MyGameServiceCallResult`, `WorkshopId`), `ParallelTasks` (`Parallel`, `Task`); `HarmonyLib` (`AccessTools`); BCL `System.Threading`. External system: Steam Workshop.
- **Used by:** [PluginLoader.cs](PluginLoader.cs.md), [Patch_MySessionLoader.cs](../Patch/Patch_MySessionLoader.cs.md), [Patch_MyWorkshop.cs](../Patch/Patch_MyWorkshop.cs.md)
