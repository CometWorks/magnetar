# Module: Shared.Config

**Project:** `Shared` · **Files:** 12 · **Source lines:** 530

## Purpose

Shared.Config owns all persistent configuration for a Magnetar/Pulsar installation. It provides a two-phase singleton initialisation (EarlyInit → Init) that gates heavier config loading behind game-environment discovery, and defines the full XML-serialised object model for core settings, plugin/hub/mod sources, and named plugin-enable profiles.

## Role in Magnetar

Acts as the configuration backbone shared by both the Legacy (Windows/.NET 4.8) and Interim (Linux/.NET 10) launchers and loaders. ConfigManager is the single entry point other modules query for directory paths, the active profile, and the resolved plugin list. All source and profile types feed directly into PluginList construction.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `ConfigManager` | class | [`Shared/Config/ConfigManager.cs`](../descriptions/Shared/Config/ConfigManager.cs.md) | Singleton root that aggregates all runtime configuration (core, sources, profiles, plugin list, stats), exposes game/Pulsar directory paths, and manages the instance.id consent anchor. |
| `CoreConfig` | class | [`Shared/Config/CoreConfig.cs`](../descriptions/Shared/Config/CoreConfig.cs.md) | Serialises low-level per-install settings (network timeouts, tri-state telemetry consent) to config.xml; the anonymous identity now lives in the separate instance.id file. |
| `SourcesConfig` | class | [`Shared/Config/SourcesConfig.cs`](../descriptions/Shared/Config/SourcesConfig.cs.md) | Registry of all plugin and mod sources (local/remote hubs, direct plugins, Workshop mods) serialised to Sources/sources.xml. |
| `ProfilesConfig` | class | [`Shared/Config/ProfilesConfig.cs`](../descriptions/Shared/Config/ProfilesConfig.cs.md) | Manages the Profiles/ directory of named plugin-enable Profile XML files, including backup-and-reset on corruption. |
| `PluginDataConfig` | class | [`Shared/Config/PluginDataConfig.cs`](../descriptions/Shared/Config/PluginDataConfig.cs.md) | Abstract base for per-plugin config records embedded in a Profile; registers concrete subtypes for XmlSerializer via [XmlInclude]. |
| `GitHubPluginConfig` | class | [`Shared/Config/GitHubPluginConfig.cs`](../descriptions/Shared/Config/GitHubPluginConfig.cs.md) | Profile entry for a GitHub-hosted plugin, adding an optional pinned release version. |
| `LocalFolderConfig` | class | [`Shared/Config/LocalFolderConfig.cs`](../descriptions/Shared/Config/LocalFolderConfig.cs.md) | Profile entry for a dev-mode local-folder plugin, adding a data file path and debug-build flag. |
| `RemoteHubConfig` | class | [`Shared/Config/Sources/RemoteHubConfig.cs`](../descriptions/Shared/Config/Sources/RemoteHubConfig.cs.md) | Source record for a GitHub-hosted hub index with staleness-cache fields (LastCheck, Hash) and a Trusted flag. |
| `RemotePluginConfig` | class | [`Shared/Config/Sources/RemotePluginConfig.cs`](../descriptions/Shared/Config/Sources/RemotePluginConfig.cs.md) | Source record for a directly-registered GitHub plugin with staleness caching and a Trusted flag. |
| `LocalHubConfig` | class | [`Shared/Config/Sources/LocalHubConfig.cs`](../descriptions/Shared/Config/Sources/LocalHubConfig.cs.md) | Source record for a local filesystem hub directory with a content Hash for change detection. |
| `LocalPluginConfig` | class | [`Shared/Config/Sources/LocalPluginConfig.cs`](../descriptions/Shared/Config/Sources/LocalPluginConfig.cs.md) | Source record for a directly-registered local plugin folder. |
| `ModConfig` | class | [`Shared/Config/Sources/ModConfig.cs`](../descriptions/Shared/Config/Sources/ModConfig.cs.md) | Source record for a Steam Workshop mod identified by its numeric Workshop item ID. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Shared/Config/ConfigManager.cs`](../descriptions/Shared/Config/ConfigManager.cs.md) | 90 | `ConfigManager` is the singleton root of all runtime configuration for Magnetar. |
| [`Shared/Config/CoreConfig.cs`](../descriptions/Shared/Config/CoreConfig.cs.md) | 74 | `CoreConfig` persists the fundamental installation-level settings to `config.xml` in the Pulsar/Magnetar data directory. |
| [`Shared/Config/GitHubPluginConfig.cs`](../descriptions/Shared/Config/GitHubPluginConfig.cs.md) | 6 | `GitHubPluginConfig` is the per-plugin configuration record stored inside a `Profile` for plugins sourced from GitHub releases. |
| [`Shared/Config/LocalFolderConfig.cs`](../descriptions/Shared/Config/LocalFolderConfig.cs.md) | 7 | `LocalFolderConfig` is the per-plugin configuration record stored inside a `Profile` for plugins sourced from a local development folder (the "DevFolder" feature). |
| [`Shared/Config/PluginDataConfig.cs`](../descriptions/Shared/Config/PluginDataConfig.cs.md) | 10 | `PluginDataConfig` is the abstract base for per-plugin configuration records that are embedded in a `Profile`. |
| [`Shared/Config/ProfilesConfig.cs`](../descriptions/Shared/Config/ProfilesConfig.cs.md) | 156 | `ProfilesConfig` manages the on-disk lifecycle of named plugin-enable profiles. |
| [`Shared/Config/Sources/LocalHubConfig.cs`](../descriptions/Shared/Config/Sources/LocalHubConfig.cs.md) | 9 | `LocalHubConfig` is the configuration record for a locally-stored plugin hub — a directory on the filesystem that acts as a hub catalogue. |
| [`Shared/Config/Sources/LocalPluginConfig.cs`](../descriptions/Shared/Config/Sources/LocalPluginConfig.cs.md) | 8 | `LocalPluginConfig` is the configuration record for a plugin that is installed directly from a local filesystem folder, without going through a hub or GitHub. |
| [`Shared/Config/Sources/ModConfig.cs`](../descriptions/Shared/Config/Sources/ModConfig.cs.md) | 8 | `ModConfig` is the configuration record for a Steam Workshop mod source. |
| [`Shared/Config/Sources/RemoteHubConfig.cs`](../descriptions/Shared/Config/Sources/RemoteHubConfig.cs.md) | 14 | `RemoteHubConfig` is the configuration record for a GitHub-hosted plugin hub. |
| [`Shared/Config/Sources/RemotePluginConfig.cs`](../descriptions/Shared/Config/Sources/RemotePluginConfig.cs.md) | 14 | `RemotePluginConfig` is the configuration record for a GitHub-hosted plugin that is registered directly as a source (not via a hub). |
| [`Shared/Config/SourcesConfig.cs`](../descriptions/Shared/Config/SourcesConfig.cs.md) | 134 | `SourcesConfig` is the XML-serialised registry of all plugin and mod sources available to Magnetar. |

## Public API surface

- `ConfigManager.EarlyInit(string pulsarDir)`
- `ConfigManager.Init(string gameDir, string modDir, Version gameVersion, RemoteHubConfig[] defaultHubs)`
- `ConfigManager.Instance`
- `ConfigManager.HasInstanceId() → bool`
- `ConfigManager.ReadInstanceId() → string`
- `ConfigManager.CreateInstanceId() → string`
- `ConfigManager.DeleteInstanceId()`
- `ConfigManager.UpdatePlayerVotes()`
- `CoreConfig.Load(string mainDirectory)`
- `CoreConfig.Save()`
- `SourcesConfig.Load(string mainDirectory, RemoteHubConfig[] defaultHubs)`
- `SourcesConfig.Save()`
- `ProfilesConfig.Load(string mainDirectory)`
- `ProfilesConfig.Save(string key = null)`
- `ProfilesConfig.Add(Profile)`
- `ProfilesConfig.Remove(string key)`
- `ProfilesConfig.Rename(string key, string newName)`

## Dependencies

**Uses modules:** [Shared.Core](Shared.Core.md), [Shared.Data](Shared.Data.md), [Shared.Votes](Shared.Votes.md)  
**Used by modules:** [Legacy.Launcher](Legacy.Launcher.md), [Legacy.Loader](Legacy.Loader.md), [Legacy.Patch](Legacy.Patch.md), [Shared.Core](Shared.Core.md), [Shared.Data](Shared.Data.md), [Shared.Network](Shared.Network.md), [Shared.Votes](Shared.Votes.md)  
**External systems:** GitHub

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
