# Shared/Data/PluginStatus.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Data` · **Kind:** enum · **Lines:** 12

## Summary
`PluginStatus` enumerates the load/health states a `PluginData` can be in, used to drive the status column in the plugin UI and to gate loading. `PluginData.StatusString` maps these to display strings.

## Types
### PluginStatus — enum, public
- **Values:** `None` (default), `Network` (download/web failure), `Updated` (recompiled this launch), `Error` (load/build failed), `Blocked` (blocked by OS / explicitly blocked), `Runtime` (incompatible .NET runtime), `Platform` (incompatible OS).

## Cross-references
- **Uses:** none.
- **Used by:** [PluginInstance.cs](../../Legacy/Loader/PluginInstance.cs.md), [GitHubPlugin.cs](GitHubPlugin.cs.md), [LocalFolderPlugin.cs](LocalFolderPlugin.cs.md), [LocalPlugin.cs](LocalPlugin.cs.md), [PluginData.cs](PluginData.cs.md)
