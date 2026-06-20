# Shared/Config/Sources/RemoteHubConfig.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Config` · **Kind:** class · **Lines:** 14

## Summary
`RemoteHubConfig` is the configuration record for a GitHub-hosted plugin hub. It is serialised as a `<RemoteHub>` element inside `SourcesConfig`. The loader uses `Repo` and `Branch` to fetch the hub index via the GitHub API, and `Hash` / `LastCheck` to implement staleness-based caching so the hub index is not downloaded on every server start.

## Types

### RemoteHubConfig — class, public

Data-transfer object that describes one remote (GitHub) hub source. The `Trusted` flag controls whether plugins from this hub are loaded without an additional confirmation prompt.

- **Properties:** `Name` — display name for the hub; `Repo` — GitHub repository identifier (e.g. `"owner/repo"`); `Branch` — branch to read the hub index from; `LastCheck` — nullable `DateTime` recording the last successful check (used with `SourcesConfig.MaxSourceAge` to avoid redundant fetches); `Hash` — last-known commit or content hash of the hub index; `Enabled` — whether this hub is active; `Trusted` — whether plugins from this hub are implicitly trusted.

## Cross-references
- **Uses:** _(none — pure DTO)_
- **Used by:** [Program.cs](../../../Legacy/Program.cs.md), [ConfigManager.cs](../ConfigManager.cs.md), [SourcesConfig.cs](../SourcesConfig.cs.md), [PluginList.cs](../../PluginList.cs.md)
