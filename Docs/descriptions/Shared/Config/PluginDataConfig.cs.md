# Shared/Config/PluginDataConfig.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Config` · **Kind:** abstract class · **Lines:** 10

## Summary
`PluginDataConfig` is the abstract base for per-plugin configuration records that are embedded in a `Profile`. It carries only the plugin's unique identifier string and uses `[XmlInclude]` attributes to register the concrete subtypes (`LocalFolderConfig` and `GitHubPluginConfig`) so that `XmlSerializer` can round-trip polymorphic collections inside `Profile` without extra type hints.

## Types

### PluginDataConfig — abstract class, public

Base record that provides the plugin `Id` shared across all plugin configuration flavours. The `[XmlInclude]` attributes on this class are required for XML deserialisation of `Profile` properties that hold `HashSet<GitHubPluginConfig>` / `HashSet<LocalFolderConfig>`.

- **Properties:** `Id` — stable string identifier for the plugin (typically the GitHub repository slug or a local GUID).

## Cross-references
- **Uses:** `Shared/Config/LocalFolderConfig.cs` (via `[XmlInclude]`), `Shared/Config/GitHubPluginConfig.cs` (via `[XmlInclude]`)
- **Used by:** [GitHubPluginConfig.cs](GitHubPluginConfig.cs.md), [LocalFolderConfig.cs](LocalFolderConfig.cs.md), [GitHubPlugin.cs](../Data/GitHubPlugin.cs.md), [LocalFolderPlugin.cs](../Data/LocalFolderPlugin.cs.md), [PluginData.cs](../Data/PluginData.cs.md), [Profile.cs](../Data/Profile.cs.md), [PluginList.cs](../PluginList.cs.md)
