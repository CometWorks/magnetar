# Shared/Network/NuGetPackage.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Network` · **Kind:** class · **Lines:** 124

## Summary
`NuGetPackage` represents a single NuGet package that has already been extracted to disk. Its sole job is to locate the `lib/` and `content/` files that are compatible with a given target framework and expose them as typed `Item` records. `NuGetClient` constructs one instance per downloaded package; consumers (plugin loaders, compiler) iterate `LibFiles` to add assembly references or `ContentFiles` to copy content into the plugin directory.

## Types

### `NuGetPackage` — class, public

Reads the on-disk package folder using NuGet's `PackageFolderReader` and `FrameworkReducer` during construction. `GetFileLists` is called from the constructor and populates both file arrays immediately, so the object is fully initialised by the time the constructor returns.

- **Fields:**
  - `installPath` — absolute path to the extracted package directory
  - `targetFramework` — the `NuGetFramework` to match when selecting TFM-specific items

- **Properties:**
  - `LibFiles` — `Item[]`; the library assemblies (from the package's `lib/<tfm>/` folder) that best match `targetFramework`
  - `ContentFiles` — `Item[]`; the content files (from the package's `content/<tfm>/` folder) that best match `targetFramework`

- **Methods:**
  - `NuGetPackage(string installPath, NuGetFramework targetFramework)` — constructor; stores fields and immediately calls `GetFileLists()`
  - `GetFileLists()` — creates a `PackageFolderReader` over `installPath`, then calls `GetItems` twice (once for `GetLibItems()`, once for `GetContentItems()`) and assigns the results to `LibFiles` and `ContentFiles`
  - `GetItems(IEnumerable<FrameworkSpecificGroup>, FrameworkReducer, NuGetFramework, bool)` — uses `FrameworkReducer.GetNearest` to find the closest TFM among the package's groups, then iterates the matching group and calls `GetPackageItem` for each entry; returns `Item[]` or empty array when no compatible TFM is found
  - `GetPackageItem(string path, NuGetFramework framework, bool content)` — resolves `path` to a full filesystem path, then tries `TrySplitPath` first on the TFM short folder name (e.g., `net48`, `net10.0`) and then on the generic folder name (`lib` or `content`); returns a new `Item` or `null` if the file does not exist
  - `TrySplitPath(string fullPath, string lastFolderName, out string folder, out string file)` — uses `LastIndexOf` to find the deepest occurrence of `lastFolderName` in `fullPath` (avoiding false matches on path prefixes like `content/Microsoft.NET.Sdk/content/`), splits the path into `folder` (up to and including the marker) and `file` (the relative remainder)

### `NuGetPackage.Item` — class, public (nested)

Lightweight value-style object describing one file within a package folder.

- **Properties:**
  - `FilePath` — path relative to `Folder`
  - `Folder` — absolute path to the folder containing the file (e.g., `.../packages/Foo.1.0.0/lib/net48`)
  - `FullPath` — `Path.Combine(Folder, FilePath)`; absolute path to the file

## Cross-references
- **Uses:**
  - External: `NuGet.Frameworks` (`NuGetFramework`, `FrameworkReducer`)
  - External: `NuGet.Packaging` (`PackageFolderReader`, `FrameworkSpecificGroup`)
- **Used by:** [GitHubPlugin.cs](../Data/GitHubPlugin.cs.md), [LocalFolderPlugin.cs](../Data/LocalFolderPlugin.cs.md), [NuGetClient.cs](NuGetClient.cs.md)
