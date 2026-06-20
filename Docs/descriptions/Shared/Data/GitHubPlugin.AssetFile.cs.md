# Shared/Data/GitHubPlugin.AssetFile.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Data` · **Kind:** nested class (partial of `GitHubPlugin`) · **Lines:** 77

## Summary
Defines `GitHubPlugin.AssetFile`, the XML-serializable record describing one cached file that belongs to a compiled GitHub plugin: either a non-code asset extracted from the source archive, a NuGet library DLL, or NuGet content. It tracks the file's length and content hash so the cache can detect tampering or staleness, and provides helpers to read those attributes from disk, validate them, and write a stream to the correct on-disk location. This is the unit of validation used by `GitHubPlugin.CacheManifest`.

## Types
### AssetFile — class, public (nested in `GitHubPlugin`)
Represents a single cached file with identity (`Name`), integrity metadata (`Hash`, `Length`), a classification (`Type`), and a non-serialized base directory (`BaseDir`) that the manifest assigns so paths can resolve. Used both when populating the cache (compute hash/length, save stream) and when checking the cache (compare hash/length against disk).

- **Properties:**
  - `Name` — relative file path within its base directory (serialized).
  - `Hash` — content hash computed via `Tools.GetFileHash`.
  - `Length` — file size in bytes.
  - `Type` — `AssetType` classification.
  - `BaseDir` — `[XmlIgnore]` root directory injected by the manifest (`Assets` or `Bin` dir).
  - `NormalizedFileName` — `Name` with backslashes converted to `/` and leading slashes trimmed; used as the dictionary key suffix.
  - `FullPath` — absolute path = `Path.GetFullPath(Path.Combine(BaseDir, Name))`.
- **Methods:**
  - `AssetFile()` — parameterless ctor for XML deserialization.
  - `AssetFile(string file, AssetType type)` — sets `Name` and `Type`.
  - `GetFileInfo()` — if the file exists, records its `Length` and `Hash` from disk.
  - `IsValid()` — returns true only if the file exists and its on-disk length and hash both match the stored values; used to detect corruption/staleness.
  - `Save(Stream stream)` — creates the target directory, copies `stream` into the file, then calls `GetFileInfo()` to record length/hash.

### AssetFile.AssetType — enum, public
Classifies a cached file. Values: `Asset` (non-code asset from the repo asset folder), `Lib` (NuGet library DLL placed in the `Bin` folder), `LibContent` (NuGet content file). The classification decides which base directory the file lives in and how the manifest validates cache completeness.

## Cross-references
- **Uses:** `Tools.GetFileHash` (Shared.Core helpers); `System.IO`; `System.Xml.Serialization`.
- **Used by:** [GitHubPlugin.CacheManifest.cs](GitHubPlugin.CacheManifest.cs.md), [GitHubPlugin.cs](GitHubPlugin.cs.md), [LocalFolderPlugin.cs](LocalFolderPlugin.cs.md), [LocalPlugin.cs](LocalPlugin.cs.md), [PluginData.cs](PluginData.cs.md), [Updater.cs](../Updater.cs.md)
