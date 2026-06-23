# Legacy/Patch/Patch_MyDefinitionManager.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Patch` · **Kind:** static class, public · **Lines:** 45

## Summary
Prefix-patches `MyDefinitionManager.LoadData` to augment SE's mod list before definitions are loaded. It first injects the MagnetarMod client companion via `MagnetarClientMod.ApplyToModList`, then appends a workshop mod item for every `ModPlugin` in the active Magnetar configuration profile that is not already present. This is the definition-loading half of client-mod support: the added workshop mod items are what SE subsequently uses to locate and load SBC definition files.

## Types

### Patch_MyDefinitionManager — static class, public
Harmony Prefix on `Sandbox.Definitions.MyDefinitionManager.LoadData(ref List<MyObjectBuilder_Checkpoint.ModItem> mods)`, applied in the `"Early"` patch category. The prefix:

1. Calls `MagnetarClientMod.ApplyToModList(ref mods)` to add (or strip, per `-noimplicitmod`/crossplay) the MagnetarMod client companion.
2. Builds a `HashSet<ulong>` of workshop IDs already present in the mod list.
3. Queries `ConfigManager.Instance.List.GetModPlugins(current, currentMods)` to enumerate `ModPlugin` entries that belong to the active profile (`ConfigManager.Instance.Profiles.Current`) and are not already in the list.
4. For each `ModPlugin`, logs the workshop ID and calls `mod.GetModItem()` (an extension in `Legacy/Extensions/ModPlugin.cs`) to produce a `MyObjectBuilder_Checkpoint.ModItem`, appending it to a copy of the list.
5. Replaces the `ref mods` parameter with the augmented list.

Errors are caught, logged via `LogFile.Error`, and re-thrown so the original exception still propagates.

- **Methods:** `Prefix(ref List<MyObjectBuilder_Checkpoint.ModItem> mods) — Harmony Prefix; injects the MagnetarMod companion and ModPlugin mod-items into the definition-loading mod list`

## Cross-references
- **Uses:** `Legacy/Loader/MagnetarClientMod.cs` (`ApplyToModList`), `Shared/Config/ConfigManager.cs` (`ConfigManager.Instance`), `Shared/Data/PluginList.cs` (`GetModPlugins`), `Shared/Data/Profile.cs` (`Profiles.Current`), `Legacy/Extensions/ModPlugin.cs` (`ModPlugin.GetModItem`), `Shared/LogFile.cs`, `Sandbox.Definitions.MyDefinitionManager.LoadData` (patched target)
- **Used by:** _none within the repository_
