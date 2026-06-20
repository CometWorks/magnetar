# Shared/Config/LocalFolderConfig.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Config` · **Kind:** class · **Lines:** 8

## Summary
`LocalFolderConfig` is the per-plugin configuration record stored inside a `Profile` for plugins sourced from a local development folder (the "DevFolder" feature). It extends `PluginDataConfig` with the path to an explicit data file and a debug-build flag, supporting the developer workflow where a plugin is compiled locally and loaded directly without going through GitHub releases.

## Types

### LocalFolderConfig — class, public : `PluginDataConfig`

Minimal data-transfer object for a dev-mode plugin entry. The `DataFile` property identifies which compiled assembly or manifest file in the local folder to load, and `DebugBuild` tells the Roslyn-based loader whether to use debug symbols.

- **Properties:** `DataFile` — path (relative or absolute) to the plugin data file within the development folder; `DebugBuild` — whether to load the debug build of the plugin (default `true`).

## Cross-references
- **Uses:** `Shared/Config/PluginDataConfig.cs`
- **Used by:** [PluginDataConfig.cs](PluginDataConfig.cs.md), [LocalFolderPlugin.cs](../Data/LocalFolderPlugin.cs.md), [Profile.cs](../Data/Profile.cs.md)
