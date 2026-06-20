# Shared/Data/GitHubPlugin.CacheManifest.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Data` · **Kind:** nested class (partial of `GitHubPlugin`) · **Lines:** 241

## Summary
Defines `GitHubPlugin.CacheManifest`, the persistent on-disk cache record for a compiled GitHub plugin. It lives under `<PulsarDir>/GitHub/<user>/<repo>/` and tracks the compiled `plugin.dll`, the source commit, the runtime/game-version it was built against, and every extracted asset/NuGet file (`AssetFile[]`). Its central job is `IsCacheValid`: deciding whether the cached DLL can be reused or the plugin must be recompiled because the commit, runtime, game version, or any cached file changed. The manifest is serialized to `manifest.xml` via `XmlSerializer`.

## Types
### CacheManifest — class, public (nested in `GitHubPlugin`)
Owns the cache directory layout (`Assets`, `Bin`, `plugin.dll`, `manifest.xml`) and the dictionary of `AssetFile` records keyed by normalized relative path. Loaded once per plugin, it validates the cache, creates/saves assets during compilation, prunes unknown files, and persists itself. Also performs a one-time migration from the legacy `commit.sha1` file into the XML manifest.

- **Fields (constants):** `pluginFile`="plugin.dll", `manifestFile`="manifest.xml", `commitFile`="commit.sha1" (legacy), `assetFolder`="Assets", `libFolder`="Bin".
- **Fields:** `cacheDir`, `assetDir`, `libDir` — resolved directory paths; `assetFiles` — `Dictionary<string, AssetFile>` keyed by `GetAssetKey`.
- **Properties:**
  - `DllFile` — `[XmlIgnore]` absolute path of the compiled DLL.
  - `AssetFolder` / `LibDir` — exposed asset and lib directories.
  - `Commit` — source commit the cache was built from (serialized).
  - `Runtime` — `RuntimeInformation.FrameworkDescription` at build time; mismatch forces rebuild (key for the dual net48/net10 Magnetar fork).
  - `GameVersion` — `[XmlIgnore]` SE game version the cache targets.
  - `GameVersionString` — `[XmlElement("GameVersion")]`, `EditorBrowsable(Never)` string surrogate that serializes `GameVersion` as text.
  - `AssetFiles` — `[XmlArray]`/`[XmlArrayItem("File")]` array view over the dictionary; setter rebuilds the dictionary keyed by `GetAssetKey`.
- **Methods:**
  - `CacheManifest()` — parameterless ctor for deserialization.
  - `Init(string cacheDir)` — resolves dirs, assigns `BaseDir` on every loaded `AssetFile`, and migrates a legacy `commit.sha1` into `Commit` (then deletes it and re-saves).
  - `static Load(userName, repoName)` — builds the cache dir under `ConfigManager.Instance.PulsarDir/GitHub/...`, creates it, deserializes `manifest.xml` (or a fresh manifest on missing/corrupt file), calls `Init`, returns it.
  - `IsCacheValid(currentCommit, currentGameVersion, requiresAssets, requiresPackages)` — returns false if the DLL is missing, the commit differs, the runtime differs, the game version differs (when provided), required assets/packages are absent, or any `AssetFile.IsValid()` fails. The decisive gate for recompilation.
  - `ClearAssets()` — empties the asset dictionary (called before a rebuild).
  - `CreateAsset(file, type=Asset)` — normalizes the path, constructs an `AssetFile`, sets its base dir, captures file info, stores it by key, returns it.
  - `GetAssetKey(asset)` — builds the dictionary key: `Assets/<name>` for assets, `Bin/<name>` otherwise.
  - `SetBaseDir(asset)` — points the asset's `BaseDir` at `assetDir` (Asset) or `libDir` (Lib/LibContent).
  - `IsAssetValid(asset)` / `SaveAsset(asset, stream)` — thin delegations to `AssetFile.IsValid`/`Save`.
  - `Save()` — serializes the manifest to `manifest.xml`.
  - `DeleteUnknownFiles()` / `DeleteUnknownFiles(dir)` — walks `Assets` and `Bin`, deletes any on-disk file whose cache-relative path is not in `assetFiles` (cleans stale leftovers).
  - `Invalidate()` — nulls `Commit` and saves, forcing a recompile next launch.

## Cross-references
- **Uses:** `GitHubPlugin.AssetFile` (Shared/Data/GitHubPlugin.AssetFile.cs); `ConfigManager.Instance.PulsarDir` (Shared.Config); `LogFile` (Shared.Core logging); `System.Xml.Serialization`; `System.Runtime.InteropServices.RuntimeInformation`.
- **Used by:** [GitHubPlugin.AssetFile.cs](GitHubPlugin.AssetFile.cs.md), [GitHubPlugin.cs](GitHubPlugin.cs.md), [LocalFolderPlugin.cs](LocalFolderPlugin.cs.md), [LocalPlugin.cs](LocalPlugin.cs.md), [PluginData.cs](PluginData.cs.md), [Updater.cs](../Updater.cs.md)
