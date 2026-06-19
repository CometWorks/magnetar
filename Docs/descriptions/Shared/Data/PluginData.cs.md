# Shared/Data/PluginData.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Data` · **Kind:** abstract class · **Lines:** 354

## Summary
`PluginData` is the abstract base for every kind of plugin entry in Magnetar's plugin list: GitHub-compiled (`GitHubPlugin`), local source folder (`LocalFolderPlugin`), local DLL (`LocalPlugin`), Steam Workshop mod (`ModPlugin`), and the placeholder `ObsoletePlugin`. It centralizes metadata (id, friendly name, author, dependencies, group), status, the safe assembly-loading flow with runtime/platform gating and rich error handling, profile membership updates, fuzzy search ranking, and config/asset path helpers. It is both XML- and ProtoBuf-serializable so the same model serves on-disk data files and over-the-wire plugin lists.

## Types
### PluginData — abstract class, public : `IEquatable<PluginData>`, `[ProtoContract]`
The shared plugin model and load orchestrator. Equality and hashing are by `Id`. ProtoBuf includes register the concrete subtypes (`ObsoletePlugin`=100, `GitHubPlugin`=103, `ModPlugin`=104) and XML includes register `GitHubPlugin`/`ModPlugin`. Subclasses override `IsLocal`, `IsCompiled`, `GetAssembly`, and optionally `LoadData`, `TryLoadAssembly`, `UpdateProfile`, `InvalidateCache`, `GetAssetPath`.

- **Fields:** `Source` — origin label (e.g. plugin list source).
- **Properties:**
  - `IsLocal`, `IsCompiled` — abstract classification.
  - `Version` — `[XmlIgnore]` loaded assembly version (protected setter).
  - `Status` — `[XmlIgnore]` `PluginStatus`; `StatusString` — human-readable status text mapping.
  - `Id` `[ProtoMember(1)]`, `FriendlyName` `[ProtoMember(2)]` (default "Unknown"), `Hidden` `[ProtoMember(3)]`, `GroupId` `[ProtoMember(4)]`, `Tooltip` `[ProtoMember(5)]`, `Author` `[ProtoMember(6)]`, `Description` `[ProtoMember(7)]`, `Runtimes` `[ProtoMember(8)]`, `Platforms` `[ProtoMember(10)]`, `DependencyIds` `[ProtoMember(9)]`/`[XmlArray]`.
  - `Dependencies`, `Group` — `[XmlIgnore]` resolved lists built at load time.
  - `Enabled` — `[XmlIgnore]` whether the current profile contains this `Id`.
- **Methods:**
  - `LoadData(config)` — virtual no-op; subclasses apply user settings.
  - `GetAssembly()` — abstract; produce the plugin assembly.
  - `TryLoadAssembly(out a)` — gates on `IsSupportedRuntime`/`IsSupportedPlatform` (setting `Runtime`/`Platform` status), respects pre-existing `Error`/`Blocked` status, calls `GetAssembly`, and converts exceptions to status: aggregates each inner build error, treats `MemberAccessException` as stale cache (`InvalidateCache`), Windows "loadFromRemoteSources" `NotSupportedException` as a blocked-file error, `WebException` as `Network`, else generic `Error`.
  - `IsSupportedRuntime()` — true if no runtime restriction or it matches the compiled target (`NETFramework` under `#if NETFRAMEWORK`, else `NETCoreApp`) — the core net48/net10 compatibility check.
  - `IsSupportedPlatform()` — true if no platform restriction or it matches the OS; uses `RuntimeInformation.IsOSPlatform(Windows)` (so it compiles for both targets and treats Proton/Wine as Windows).
  - `Equals`/`GetHashCode`/`==`/`!=` — by `Id`.
  - `ToString()` => `Id|FriendlyName`.
  - `Error(msg=null)` — sets `Status = Error`; unless `Flags.CheckAllPlugins`, shows a message and calls `Environment.Exit(1)` (fatal on a real launch).
  - `Rank(query)` — search score combining `StrictRank` (substring hits, weighted by `int.MaxValue`) and `FuzzyRank`.
  - `StrictRank(terms)` / `FuzzyRank(terms)` / `GetFinalScore(...)` — substring scoring over name/author, and FuzzySharp scoring (`Fuzz.PartialRatio`/`Ratio`/`TokenSetRatio`) over name/author/tooltip with a missing-field penalty.
  - `UpdateProfile(profile, enabled)` — when enabling, disables grouped siblings and recursively enables dependencies (notes it can't handle cyclic deps); when disabling, removes `Id` from the profile.
  - `InvalidateCache()` — virtual no-op (overridden by `GitHubPlugin`).
  - `GetAssetPath()` — virtual null (overridden by subclasses).
  - `GetConfigPath(name, extension=null)` — returns a per-plugin path under `<PulsarDir>/Data`; creates a directory when no extension is given, otherwise appends the extension.

## Cross-references
- **Uses:** `PluginStatus` (Shared/Data/PluginStatus.cs); `Profile`/`PluginDataConfig` (Shared/Data/Profile.cs, Shared.Config); `ConfigManager`, `Flags`, `Tools`, `LogFile` (Shared.Config/Core); `FuzzySharp` (`Fuzz`); ProtoBuf; `System.Reflection`, `System.Net.WebException`, `System.Runtime.InteropServices`.
- **Used by:** [GitHubPlugin.cs](GitHubPlugin.cs.md), [LocalFolderPlugin.cs](LocalFolderPlugin.cs.md), [PluginVotes.cs](../Votes/Model/PluginVotes.cs.md), [ObsoletePlugin.cs](ObsoletePlugin.cs.md), [PluginList.cs](../PluginList.cs.md), [LocalPlugin.cs](LocalPlugin.cs.md), [ModPlugin.cs](ModPlugin.cs.md), [PluginInstance.cs](../../Legacy/Loader/PluginInstance.cs.md), [Loader.cs](../Loader.cs.md)
