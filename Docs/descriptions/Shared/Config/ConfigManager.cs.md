# Shared/Config/ConfigManager.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Config` · **Kind:** class · **Lines:** 90

## Summary
`ConfigManager` is the singleton root of all runtime configuration for Magnetar. It owns every config sub-system (core settings, plugin sources, profiles, the plugin list, and download statistics) and is the single authoritative place callers query to find out where the game, mod, and Pulsar directories are. It is initialised in two phases: `EarlyInit` runs before the game DLLs are loaded (only `CoreConfig` is needed at that point), and `Init` runs once the game environment is known and boots the remaining configs and the `PluginList`. It also manages the `instance.id` file — the on-disk anchor for telemetry consent (see [`ConsentManager`](../Votes/ConsentManager.cs.md)).

## Types

### ConfigManager — class, public

Singleton that aggregates all configuration state for a Magnetar installation. It is populated in two ordered calls (`EarlyInit` → `Init`) so that `CoreConfig` (needed for network timeouts and consent) is available before the heavier profile/sources/plugin machinery.

- **Fields:** `HarmonyVersion` — compile-time constant recording the expected Harmony version (`2.4.2.0`).
- **Properties:** `Instance` — static singleton accessor; `List` — the `PluginList` built from sources and the active profile; `Core` — low-level core config (timeouts, consent state); `Sources` — the `SourcesConfig` describing all plugin / hub / mod sources; `Profiles` — named plugin-enable profiles; `Votes` — cached `PluginVotes` downloaded from the stats server; `GameVersion` — the SE DS version detected at startup; `PulsarDir` — path to the Magnetar/Pulsar data directory; `GameDir` — path to the SE DS installation; `ModDir` — path to the SE DS mods directory; `SafeMode` — disables plugin loading when `true`; `HasLocal` — set by the loader when at least one local plugin source is present.
- **Private members:** `InstanceIdPath` — computed property resolving `<PulsarDir>/instance.id`.
- **Methods:**
  - `EarlyInit(pulsarDir)` — creates `Instance` and loads `CoreConfig`.
  - `Init(gameDir, modDir, gameVersion, defaultHubs)` — populates the remaining properties, loads `ProfilesConfig`, `SourcesConfig`, and constructs `PluginList`.
  - `HasInstanceId()` — `true` when `instance.id` exists on disk; the presence of this file *is* the record of granted telemetry consent.
  - `ReadInstanceId()` — returns the trimmed UUID from `instance.id`, or `null` if absent.
  - `CreateInstanceId()` — returns the existing id if present, otherwise generates a new UUID4 (`Guid.NewGuid().ToString("D")`), writes it to `instance.id`, and returns it.
  - `DeleteInstanceId()` — removes `instance.id` if present (used by consent withdrawal).
  - `UpdatePlayerVotes()` — fires a background `Task` to download `PluginVotes` from the stats server via `VotesClient`.

> **Note.** The legacy `GetOrCreateInstallId()` method and the `installIdLock` it guarded have been removed; the anonymous identity now lives in the standalone `instance.id` file rather than in `CoreConfig.InstallId`.

## Cross-references
- **Uses:** `Shared/Config/CoreConfig.cs`, `Shared/Config/SourcesConfig.cs`, `Shared/Config/ProfilesConfig.cs`, `Shared/Config/Sources/RemoteHubConfig.cs`, `Shared/PluginList.cs`, `Pulsar.Shared.Votes.VotesClient`, `Pulsar.Shared.Votes.Model.PluginVotes`; `System.IO` (instance.id file)
- **Used by:** [PluginLoader.cs](../../Legacy/Loader/PluginLoader.cs.md), [Patch_MyDefinitionManager.cs](../../Legacy/Patch/Patch_MyDefinitionManager.cs.md), [Patch_MyScriptManager.cs](../../Legacy/Patch/Patch_MyScriptManager.cs.md), [Program.cs](../../Legacy/Program.cs.md), [GitHubPlugin.CacheManifest.cs](../Data/GitHubPlugin.CacheManifest.cs.md), [GitHubPlugin.cs](../Data/GitHubPlugin.cs.md), [LocalFolderPlugin.cs](../Data/LocalFolderPlugin.cs.md), [ModPlugin.cs](../Data/ModPlugin.cs.md), [PluginData.cs](../Data/PluginData.cs.md), [Loader.cs](../Loader.cs.md), [GitHub.cs](../Network/GitHub.cs.md), [NuGetClient.cs](../Network/NuGetClient.cs.md), [Updater.cs](../Updater.cs.md), [ConsentManager.cs](../Votes/ConsentManager.cs.md)
