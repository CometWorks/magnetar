# Legacy/Loader/SteamMods.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Loader` · **Kind:** static class · **Lines:** 200

## Summary
Downloads/updates Steam Workshop items through SE's registered Steam UGC service and reflected internal downloader. It first checks that the `"Steam"` UGC aggregate exists; missing services, downloader failures, and install-state failures produce warnings and let server startup continue. It also repairs legacy `*_legacy.bin` archives and supports hardened-mode trust checks without calling `Steamworks.NET` directly.

## Types

### SteamMods — static class, public
Reflection-bridged wrapper over SE's internal Steam workshop downloader. Exists so Magnetar can prefetch mod-plugin workshop content at server init without a public SE API.

- **Fields:**
  - `SteamWorkshopService` (const string) — SE UGC service name `"Steam"`.
  - `DownloadModsBlocking` (static `MethodInfo`) — lazily-resolved cache of the non-public `MyWorkshop.DownloadModsBlocking` method.
  - `installStateWarningLogged` (static bool) — suppresses repeated trust-check warnings.
- **Methods:**
  - `IsSteamWorkshopAvailable()` — Returns whether SE has registered the `"Steam"` UGC aggregate; catches service-access failures and returns false.
  - `Update(IEnumerable<ulong> ids)` — Returns early for an empty set. If Steam UGC is absent, warns and skips all updates. Otherwise invokes `UpdateInternal` on a `Parallel.Start` task while pumping `MyGameService.Update`; task exceptions, result failures, and synchronous exceptions are warnings rather than startup failures.
  - `IsModUntrusted(MyObjectBuilder_Checkpoint.ModItem mod)` — Non-Steam mods are untrusted. Steam mods are checked through `IMyUGCService.CreateWorkshopItem` and the item's `Installed` state; missing service or exceptions fail closed as untrusted and warn once.
  - `WarnInstallState(string message)` — Logs the first install-state warning only.
  - `UpdateInternal(List<MyObjectBuilder_Checkpoint.ModItem> mods)` — Mirrors `MyWorkshop.DownloadWorldModsBlockingInternal`, resolves the private downloader, repairs legacy archives on success, and restores SE log indentation in a `finally` block. A missing reflected method becomes a caught `MissingMethodException` in `Update`.
  - `RepairLegacyArchives(IEnumerable<MyObjectBuilder_Checkpoint.ModItem> mods)` — Iterates downloaded mods whose `ModItem` has `MyWorkshopItem` data, takes the item's `Folder`, and calls `LegacyWorkshopArchive.TryRepair` so early Workshop packages with `*_legacy.bin` are expanded before definitions/scripts load. Logs and continues if one mod cannot be checked.

## Cross-references
- **Uses:** `Pulsar.Shared.LogFile`; `Pulsar.Shared.Data.LegacyWorkshopArchive`; SE DS assemblies: `Sandbox.Engine.Networking` (`MyWorkshop`, `MyGameService`), `VRage.Game` (`ModItem`), `VRage.Utils.MyLog`, `VRage.GameServices` (`IMyUGCService`, `MyWorkshopItem`, `MyWorkshopItemState`, `MyGameServiceCallResult`, `WorkshopId`); `ParallelTasks`; `HarmonyLib.AccessTools`; BCL reflection/threading. External system: Steam Workshop through SE's service abstraction.
- **Used by:** [MagnetarClientMod.cs](MagnetarClientMod.cs.md), [PluginLoader.cs](PluginLoader.cs.md), [Patch_MySessionLoader.cs](../Patch/Patch_MySessionLoader.cs.md), [Patch_MyWorkshop.cs](../Patch/Patch_MyWorkshop.cs.md)
