# Module: Shared.Data

**Project:** `Shared` · **Files:** 10 · **Source lines:** 1685

## Purpose

Defines the plugin-entry data model for Magnetar's plugin list. PluginData is the abstract base for every plugin kind (GitHub-compiled, local source folder, local DLL, Steam Workshop mod, and an obsolete placeholder), each knowing how to produce its assembly, gate on runtime/platform, update profiles, and report status. It also models user-activated profiles and the on-disk compile cache for GitHub plugins.

## Role in Magnetar

This is the core domain layer the launcher/loader operates on: the plugin list is a collection of PluginData instances, and loading a plugin means calling TryLoadAssembly/GetAssembly on them. GitHubPlugin drives Roslyn compilation from a downloaded repo archive plus NuGet restore, caching results via CacheManifest; LocalFolderPlugin compiles a dev folder each launch; LocalPlugin loads a prebuilt DLL; ModPlugin locates Workshop mod content. Profiles persist which plugins are enabled. The runtime/platform gating (NETFramework vs NETCoreApp, Windows vs Linux) is central to the dual net48/net10 Magnetar fork.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `PluginData` | class | [`Shared/Data/PluginData.cs`](../descriptions/Shared/Data/PluginData.cs.md) | Abstract base for all plugin entries: metadata, status, safe assembly loading with runtime/platform gating, profile updates, fuzzy search ranking. |
| `GitHubPlugin` | class | [`Shared/Data/GitHubPlugin.cs`](../descriptions/Shared/Data/GitHubPlugin.cs.md) | Compiles a plugin from a GitHub repo archive at a pinned commit via Roslyn + NuGet, caching the DLL and assets. |
| `GitHubPlugin.CacheManifest` | class | [`Shared/Data/GitHubPlugin.CacheManifest.cs`](../descriptions/Shared/Data/GitHubPlugin.CacheManifest.cs.md) | Persistent on-disk cache record (manifest.xml) deciding whether a compiled GitHub plugin can be reused or must rebuild. |
| `GitHubPlugin.AssetFile` | class | [`Shared/Data/GitHubPlugin.AssetFile.cs`](../descriptions/Shared/Data/GitHubPlugin.AssetFile.cs.md) | One cached file (asset/lib/content) with length+hash integrity metadata and save/validate helpers. |
| `GitHubPlugin.GitHubSource` | class | [`Shared/Data/GitHubPlugin.cs`](../descriptions/Shared/Data/GitHubPlugin.cs.md) | A named selectable alternate version pinning a commit and optional repo. |
| `LocalFolderPlugin` | class | [`Shared/Data/LocalFolderPlugin.cs`](../descriptions/Shared/Data/LocalFolderPlugin.cs.md) | Compiles a plugin from a local source folder each launch, using git ls-files (or fallback) and optional NuGet restore. |
| `LocalPlugin` | class | [`Shared/Data/LocalPlugin.cs`](../descriptions/Shared/Data/LocalPlugin.cs.md) | Loads a prebuilt local DLL, detecting its target runtime via Mono.Cecil and reading sidecar XML metadata. |
| `ModPlugin` | class | [`Shared/Data/ModPlugin.cs`](../descriptions/Shared/Data/ModPlugin.cs.md) | Steam Workshop mod entry by numeric id; loads no assembly, only locates mod content (incl. legacy *_legacy.bin). |
| `ObsoletePlugin` | class | [`Shared/Data/ObsoletePlugin.cs`](../descriptions/Shared/Data/ObsoletePlugin.cs.md) | No-op placeholder PluginData kept as a ProtoBuf subtype for removed/superseded plugins. |
| `Profile` | class | [`Shared/Data/Profile.cs`](../descriptions/Shared/Data/Profile.cs.md) | Named set of enabled plugins (GitHub configs, dev folders, local DLLs, mods) with add/remove and description helpers. |
| `PluginStatus` | enum | [`Shared/Data/PluginStatus.cs`](../descriptions/Shared/Data/PluginStatus.cs.md) | Load/health states (None, Network, Updated, Error, Blocked, Runtime, Platform) driving status display and load gating. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Shared/Data/GitHubPlugin.AssetFile.cs`](../descriptions/Shared/Data/GitHubPlugin.AssetFile.cs.md) | 77 | Defines `GitHubPlugin.AssetFile`, the XML-serializable record describing one cached file that belongs to a compiled GitHub plugin: either a non-code asset extracted from the source archive, a NuGet library DLL, or NuGet content. |
| [`Shared/Data/GitHubPlugin.CacheManifest.cs`](../descriptions/Shared/Data/GitHubPlugin.CacheManifest.cs.md) | 241 | Defines `GitHubPlugin.CacheManifest`, the persistent on-disk cache record for a compiled GitHub plugin. |
| [`Shared/Data/GitHubPlugin.cs`](../descriptions/Shared/Data/GitHubPlugin.cs.md) | 381 | `GitHubPlugin` is the `PluginData` implementation that compiles a plugin from C# source pulled directly from a GitHub repository archive. |
| [`Shared/Data/LocalFolderPlugin.cs`](../descriptions/Shared/Data/LocalFolderPlugin.cs.md) | 334 | `LocalFolderPlugin` is the developer-facing `PluginData` that compiles a plugin from a local source folder on every launch (no GitHub download, no cache). |
| [`Shared/Data/LocalPlugin.cs`](../descriptions/Shared/Data/LocalPlugin.cs.md) | 109 | `LocalPlugin` is the `PluginData` for a pre-compiled plugin DLL sitting on disk (not compiled by Magnetar, not from GitHub). |
| [`Shared/Data/ModPlugin.cs`](../descriptions/Shared/Data/ModPlugin.cs.md) | 81 | `ModPlugin` is the `PluginData` for a Steam Workshop mod referenced by its numeric workshop id. |
| [`Shared/Data/ObsoletePlugin.cs`](../descriptions/Shared/Data/ObsoletePlugin.cs.md) | 15 | `ObsoletePlugin` is a placeholder `PluginData` registered as a ProtoBuf subtype so the plugin-list deserializer can tolerate plugins that have been removed or superseded. |
| [`Shared/Data/PluginData.cs`](../descriptions/Shared/Data/PluginData.cs.md) | 354 | `PluginData` is the abstract base for every kind of plugin entry in Magnetar's plugin list: GitHub-compiled (`GitHubPlugin`), local source folder (`LocalFolderPlugin`), local DLL (`LocalPlugin`), Steam Workshop mod (`ModPlugin`), and the placeholder `ObsoletePlugin`. |
| [`Shared/Data/PluginStatus.cs`](../descriptions/Shared/Data/PluginStatus.cs.md) | 12 | `PluginStatus` enumerates the load/health states a `PluginData` can be in, used to drive the status column in the plugin UI and to gate loading. |
| [`Shared/Data/Profile.cs`](../descriptions/Shared/Data/Profile.cs.md) | 81 | `Profile` is a named set of enabled plugins — the persisted selection a user activates. |

## Public API surface

- `PluginData.TryLoadAssembly(out Assembly)`
- `PluginData.GetAssembly()`
- `PluginData.IsSupportedRuntime() / IsSupportedPlatform()`
- `PluginData.UpdateProfile(Profile, bool)`
- `PluginData.Rank(string)`
- `PluginData.GetConfigPath(string, string)`
- `PluginData.GetAssetPath()`
- `GitHubPlugin.ClearGitHubCache()`
- `GitHubPlugin.InitPaths()`
- `GitHubPlugin.CacheManifest.Load(user, repo) / IsCacheValid(...)`
- `Profile.GetPluginIDs(bool) / Contains(string) / GetData(string) / Remove(string) / Validate()`

## Dependencies

**Uses modules:** [Compiler](Compiler.md), [Shared.Config](Shared.Config.md), [Shared.Core](Shared.Core.md), [Shared.Network](Shared.Network.md)  
**Used by modules:** [Legacy.Integration](Legacy.Integration.md), [Legacy.Loader](Legacy.Loader.md), [Legacy.Patch](Legacy.Patch.md), [Shared.Config](Shared.Config.md), [Shared.Core](Shared.Core.md), [Shared.Votes](Shared.Votes.md)  
**External systems:** FuzzySharp; GitHub; Mono.Cecil; NuGet; ProtoBuf; Steam

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
