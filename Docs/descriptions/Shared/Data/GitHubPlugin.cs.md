# Shared/Data/GitHubPlugin.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Data` · **Kind:** class (partial, `PluginData` subclass) · **Lines:** 381

## Summary
`GitHubPlugin` is the `PluginData` implementation that compiles a plugin from C# source pulled directly from a GitHub repository archive. On `GetAssembly` it downloads the repo zip at a pinned commit, feeds the `.cs` files plus any NuGet references through Magnetar's `ICompiler` (Roslyn), caches the resulting `plugin.dll` and extracted assets/libs via `CacheManifest`, and loads the assembly. It supports alternate pinned versions (`GitHubSource`), an asset folder, NuGet package references, and runtime/game-version-aware cache invalidation. This is the primary delivery mechanism for community plugins in the Magnetar plugin list.

## Types
### GitHubPlugin — class, public, partial : `PluginData`, `[ProtoContract]`
Describes a GitHub-sourced plugin and drives its compile/cache/load lifecycle. ProtoBuf-serializable (transmitted in the plugin list) and XML-serializable (used as the schema for local `.xml` data files read by `LocalPlugin`/`LocalFolderPlugin`). The compile path lives in `CompileFromSource`/`InstallPackage*` and the cache decision in `GetAssembly`.

- **Properties:**
  - `IsLocal` => `false`, `IsCompiled` => `true` (overrides).
  - `Commit` `[ProtoMember(1)]` — default pinned commit SHA.
  - `SourceDirectories` `[ProtoMember(2)]`/`[XmlArray]` — repo subfolders to limit compilation to.
  - `AlternateVersions` `[ProtoMember(3)]` — array of `GitHubSource` selectable versions.
  - `AssetFolder` `[ProtoMember(4)]` — repo path whose files are extracted as non-code assets.
  - `NuGetReferences` `[ProtoMember(5)]` — `NuGetPackageList` of package ids / packages.config to restore.
  - `RepoId` `[ProtoMember(6)]` — `"user/repo"` identifier; falls back to `Id` if unset.
- **Fields:** `settings` (`GitHubPluginConfig` user selection), `assemblyName`, `manifest` (`CacheManifest`), `nuget` (`NuGetClient`), `resolver` (`AssemblyResolver`).
- **Methods:**
  - `GitHubPlugin()` — sets `Status = None`.
  - `static ClearGitHubCache()` — deletes the entire `<PulsarDir>/GitHub` cache tree (used after a Magnetar update so all plugins recompile).
  - `LoadData(config)` — if the config is a valid `GitHubPluginConfig` (selected version exists), deep-copies it into `settings`.
  - `IsValidConfig(githubConfig)` — true if no version selected, or the selected version name matches an `AlternateVersions` entry.
  - `InitPaths()` — parses `RepoId` into user/repo, cleans `SourceDirectories`, normalizes `AssetFolder` (trailing slash), derives a safe `assemblyName`, and loads the `CacheManifest`.
  - `CleanPaths(paths)` / `MakeSafeString(s)` — normalize directory separators/trailing slash; replace non-alphanumeric chars with `_`.
  - `GetAssembly()` — resolves selected version/repo/commit, checks `manifest.IsCacheValid` against game version, runtime, and asset/package requirements. On miss: reports progress, records commit/runtime/game version, clears assets, compiles via `CompileFromSource`, writes the DLL to disk, prunes unknown files, saves the manifest, marks `Updated`, and loads with `Assembly.LoadFile` (file load so `Assembly.Location` is populated for preloader plugins). On hit: prunes unknown files and loads the cached DLL. Sets `Version` from the loaded assembly.
  - `GetSelectedVersion()` — returns the `GitHubSource` matching the user's selected version, or null.
  - `CompileFromSource(repo, commit, assemblyName)` — fetches the repo archive via `GitHub.GetRepoArchive`, iterates zip entries through the overload, restores NuGet packages by id, then `compiler.Compile`.
  - `CompileFromSource(compiler, entry)` — per zip entry: if it is the packages.config, downloads and installs those packages; if it is an allowed `.cs` path, loads it into the compiler; if it is under the asset folder, extracts it to the cache via `CreateAsset`/`SaveAsset`.
  - `InstallPackages` / `InstallPackage(package, compiler)` — copies NuGet lib DLLs (`AssetType.Lib`) and content files (`AssetType.LibContent`) into the cache, and adds top-level lib files as compiler dependencies via `compiler.TryAddDependency`.
  - `IsAssetZipPath(path, out assetFilePath)` — true if the zip path falls under `AssetFolder`, yielding the relative asset path.
  - `AllowedZipPath(path)` — true for `.cs` files within `SourceDirectories` (or all if none specified).
  - `RemoveRoot(path)` — strips the leading archive root folder GitHub adds to every zip entry.
  - `UpdateProfile(draft, enabled)` — calls base then, if enabled, adds a `GitHubPluginConfig { Id }` to `draft.GitHub`.
  - `InvalidateCache()` — calls `manifest.Invalidate()` so the plugin recompiles next start.
  - `GetAssetPath()` — returns the absolute cached asset folder path, or null if no asset folder.

### GitHubPlugin.GitHubSource — class, public, `[ProtoContract]`
A selectable alternate version of the plugin: a named pin to a specific commit and optionally a different repo.
- **Properties:** `Name` `[ProtoMember(1)]`, `Commit` `[ProtoMember(2)]`, `Repo` `[ProtoMember(3)]`.
- **Methods:** parameterless ctor for serialization.

## Cross-references
- **Uses:** `PluginData` (Shared/Data/PluginData.cs); `CacheManifest`/`AssetFile` (sibling partials); `Profile`/`GitHubPluginConfig` (Shared/Data/Profile.cs, Shared.Config); `ICompiler`, `Tools.Compiler` (Compiler module / Roslyn); `GitHub.GetRepoArchive` (Shared.Network — GitHub API); `NuGetClient`, `NuGetPackage`, `NuGetPackageList` (Shared.Network — NuGet); `AssemblyResolver` (Shared.Network/Core); `ConfigManager`, `LogFile`, `PluginProgress` (Shared.Config/Core); ProtoBuf, `System.IO.Compression`.
- **Used by:** [GitHubPlugin.AssetFile.cs](GitHubPlugin.AssetFile.cs.md), [GitHubPlugin.CacheManifest.cs](GitHubPlugin.CacheManifest.cs.md), [LocalFolderPlugin.cs](LocalFolderPlugin.cs.md), [LocalPlugin.cs](LocalPlugin.cs.md), [PluginData.cs](PluginData.cs.md), [Updater.cs](../Updater.cs.md)
