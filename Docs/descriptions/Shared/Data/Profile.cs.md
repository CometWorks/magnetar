# Shared/Data/Profile.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Data` · **Kind:** class · **Lines:** 81

## Summary
`Profile` is a named set of enabled plugins — the persisted selection a user activates. It holds four membership collections (GitHub plugins, dev folders, local DLLs, and Workshop mods), exposing the combined enabled-id list, per-plugin config lookup, add/remove operations used by `PluginData.UpdateProfile`, and a human-readable description. Profiles are serialized as part of Magnetar's configuration.

## Types
### Profile — class, public
A user-defined plugin selection keyed by a sanitized name. Stores configs (not just ids) for GitHub and dev-folder plugins so their settings travel with the profile, and bare ids for local DLLs and mods.

- **Properties:**
  - `Key` — `Tools.CleanFileName(Name)`, the filesystem-safe identifier.
  - `Name` — display name.
  - `GitHub` — `HashSet<GitHubPluginConfig>` of enabled GitHub plugins with settings.
  - `DevFolder` — `HashSet<LocalFolderConfig>` of enabled dev folders with settings.
  - `Local` — `HashSet<string>` of enabled local DLL ids.
  - `Mods` — `HashSet<ulong>` of enabled Workshop ids.
- **Methods:**
  - `Profile()` — parameterless ctor (deserialization; leaves collections null until `Validate`).
  - `Profile(name)` — sets name and initializes all four empty sets.
  - `GetPluginIDs(includeLocal=true)` — yields ids across GitHub, mods, and (optionally) dev folders and local plugins.
  - `Contains(id)` — whether any collection contains the id.
  - `GetDescription()` — builds a summary like "2 local plugins, 1 plugin, 3 mods".
  - `GetData(id)` — returns the `PluginDataConfig` for a GitHub or dev-folder plugin by id, or null.
  - `Remove(id)` — removes the id from GitHub/DevFolder/Local, and from Mods if it parses as a ulong.
  - `Validate()` — true only if `Name` and all four collections are non-null (guards against partially-deserialized profiles).

## Cross-references
- **Uses:** `GitHubPluginConfig`, `LocalFolderConfig`, `PluginDataConfig` (Shared.Config); `Tools.CleanFileName` (Shared.Core); referenced extensively by `PluginData.UpdateProfile` (Shared/Data/PluginData.cs) and subclasses.
- **Used by:** [PluginLoader.cs](../../Legacy/Loader/PluginLoader.cs.md), [Patch_MyDefinitionManager.cs](../../Legacy/Patch/Patch_MyDefinitionManager.cs.md), [Patch_MyScriptManager.cs](../../Legacy/Patch/Patch_MyScriptManager.cs.md), [ProfilesConfig.cs](../Config/ProfilesConfig.cs.md), [GitHubPlugin.cs](GitHubPlugin.cs.md), [LocalFolderPlugin.cs](LocalFolderPlugin.cs.md), [LocalPlugin.cs](LocalPlugin.cs.md), [ModPlugin.cs](ModPlugin.cs.md), [PluginData.cs](PluginData.cs.md), [PluginList.cs](../PluginList.cs.md)
