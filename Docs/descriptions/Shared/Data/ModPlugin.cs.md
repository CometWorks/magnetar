# Shared/Data/ModPlugin.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Data` · **Kind:** class (`PluginData` subclass) · **Lines:** 81

## Summary
`ModPlugin` is the `PluginData` for a Steam Workshop mod referenced by its numeric workshop id. Unlike the other plugin types it loads no assembly — mods are SE content packages, not managed plugins — so its job is to locate the mod's extracted folder under the configured mod directory (handling the legacy `*_legacy.bin` packaging) and to register the workshop id in the active `Profile`.

## Types
### ModPlugin — class, public : `PluginData`, `[ProtoContract]`
Represents a Workshop mod. `Id` is the decimal workshop id string; setting it also parses `WorkshopId`. ProtoBuf-serializable so it travels in the plugin list. Resolves and caches the on-disk mod location lazily.

- **Fields:** `modLocation` (cached resolved path); `isLegacy` (true when extraction fails and the mod is still represented by a single `*_legacy.bin` file).
- **Properties:**
  - `WorkshopId` — `[XmlIgnore]`, parsed from `Id`.
  - `Id` (override) — base setter plus `WorkshopId = ulong.Parse(Id)`.
  - `IsLocal` => `false`, `IsCompiled` => `false`.
  - `ModLocation` — lazily resolves `<ModDir>/<WorkshopId>`; if that folder exists but has no `Data` subfolder, looks for a `*_legacy.bin` file and asks `LegacyWorkshopArchive` to expand it in place. Falls back to pointing at the archive file only when extraction fails.
  - `Exists` — true if the mod folder exists, or (legacy) the `.bin` file exists.
- **Methods:**
  - `ModPlugin()` — parameterless ctor for serialization.
  - `GetAssembly()` => `null` (mods have no managed assembly).
  - `TryLoadAssembly(out a)` (override) — sets `a = null`, returns false (skips the base runtime/platform/load logic entirely).
  - `UpdateProfile(draft, enabled)` — base then, if enabled, adds `WorkshopId` to `draft.Mods`.

## Cross-references
- **Uses:** `PluginData` (Shared/Data/PluginData.cs); `LegacyWorkshopArchive` (Shared/Data/LegacyWorkshopArchive.cs); `Profile` (Shared/Data/Profile.cs); `ConfigManager.Instance.ModDir` (Shared.Config); ProtoBuf; `System.IO`. Indirectly tied to Steam Workshop mod content laid out by the SE DS.
- **Used by:** [ModPlugin.cs](../../Legacy/Extensions/ModPlugin.cs.md), [Patch_MyDefinitionManager.cs](../../Legacy/Patch/Patch_MyDefinitionManager.cs.md), [Patch_MyScriptManager.cs](../../Legacy/Patch/Patch_MyScriptManager.cs.md), [PluginData.cs](PluginData.cs.md), [Loader.cs](../Loader.cs.md), [PluginList.cs](../PluginList.cs.md)
