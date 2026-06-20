# Shared/Config/GitHubPluginConfig.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Config` · **Kind:** class · **Lines:** 6

## Summary
`GitHubPluginConfig` is the per-plugin configuration record stored inside a `Profile` for plugins sourced from GitHub releases. It extends `PluginDataConfig` with a single field that pins the desired release version, allowing a profile to freeze a plugin at a specific tag rather than always pulling the latest release.

## Types

### GitHubPluginConfig — class, public : `PluginDataConfig`

Thin data-transfer object that pairs a plugin ID (inherited from `PluginDataConfig`) with an optional pinned version string. When `SelectedVersion` is `null` or empty the loader uses the latest available release from the GitHub API.

- **Properties:** `SelectedVersion` — the GitHub release tag the user pinned for this plugin; `null` means "latest".

## Cross-references
- **Uses:** `Shared/Config/PluginDataConfig.cs`
- **Used by:** [PluginDataConfig.cs](PluginDataConfig.cs.md), [GitHubPlugin.cs](../Data/GitHubPlugin.cs.md), [Profile.cs](../Data/Profile.cs.md)
