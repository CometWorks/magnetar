# ConfigTerminal/Model/HubCatalog.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** enum, static class · **Lines:** 179

## Summary
Reads Magnetar's cached plugin catalogs — the protobuf-net blobs Magnetar downloads into `Sources/Hubs/*.bin` (a `PluginData[]`) and `Sources/Plugins/*.bin` (a single-element `PluginData[]`). It parses by wire-field number with `ProtoReader` so the tool never references `Shared`/protobuf-net, skipping unknown fields so schema growth degrades gracefully. Offline by design: it reflects exactly what Magnetar has already fetched.

## Types
### HubPluginKind — enum, internal
`GitHub`, `Mod`, `Obsolete`, `Unknown`.
### HubPluginInfo — sealed class, internal
One plugin entry read from a catalog cache — the subset of `PluginData` the tool needs: `Id` (the key stored in `Profile.GitHub`), `FriendlyName`, `Author`, `Tooltip`, `Description`, `Hidden`, `DependencyIds`, `Kind`, `RepoId` (`GitHubPlugin.RepoId`), `SourceLabel` (hub source name or .bin stem).
### HubCatalog — static class, internal
- **Consts:** `PluginData` `[ProtoMember]` field numbers (`FieldId`=1 … `FieldPlatforms`=10), `[ProtoInclude]` subtype markers (`IncludeObsolete`=100, `IncludeGitHub`=103, `IncludeMod`=104), and `GitHubFieldRepoId`=6.
- **Methods:**
  - `Parse(byte[] blob, string sourceLabel)` — walks the root bare-array (repeated length-delimited field 1), reading each element via `ReadPlugin` and tagging its `SourceLabel`.
  - `ReadFile(string binPath, string sourceLabel)` — reads and parses one `.bin`; empty list on any error or empty file.
  - `ReadPlugin(ProtoReader)` (private static) — decodes one `PluginData` message field-by-field, capturing id/name/flags/deps and the subtype kind (reading the GitHub submessage's `RepoId`); defaults `FriendlyName` and drops entries with no id.
  - `ReadGitHubRepoId(ProtoReader)` (private static) — extracts `RepoId` from the `GitHubPlugin` submessage.

## Cross-references
- **Uses:** `ProtoReader` (this module); `System.IO`, `System.Linq`.
- **Used by:** [MagnetarPlugins.cs](MagnetarPlugins.cs.md), [HubPluginsView.cs](../Ui/HubPluginsView.cs.md), [HubCatalogTests.cs](../../ConfigTerminalTests/HubCatalogTests.cs.md)
