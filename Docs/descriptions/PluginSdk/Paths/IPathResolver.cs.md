# PluginSdk/Paths/IPathResolver.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Paths` · **Kind:** interface · **Lines:** 48

## Summary

Defines the backend contract for cross-platform, case-insensitive path resolution. On Windows the SE DS filesystem is case-insensitive by default, so the default implementation is a no-op shim. On Linux, where the filesystem is case-sensitive, the host installs a real backend (provided by the LinuxCompat plugin's path cache) that makes every path operation behave as if the filesystem is case-insensitive. Plugins call `PathResolver` (the static facade) unconditionally and never reference this interface directly; only implementors and the host need it.

## Types

### `IPathResolver` — interface, public

The single abstraction point for all path-normalization and case-resolution operations. The host swaps the active implementation once at startup (`PathResolver.Install`); the shim and any future real implementation both satisfy this contract.

- **Methods:**
  - `Normalize(string path) → string` — converts backslashes to forward slashes and trims whitespace; on Windows (shim) this is a pass-through
  - `ToWindowsPath(string path) → string` — rewrites a Linux-shaped path back to Windows-shape (`\` separators) for outbound paths exposed to mods; no-op on Windows
  - `GetFileName(string path) → string` — cross-platform `Path.GetFileName` that treats `\` as a separator on Linux
  - `GetFileNameWithoutExtension(string path) → string` — cross-platform `Path.GetFileNameWithoutExtension` with the same `\`-awareness
  - `ResolveContentFilePath(string relativePath, string rootPath) → string` — resolves a content-relative file path against an explicit root using case-insensitive directory/file matching where supported; on the shim simply combines `rootPath + relativePath`
  - `ResolveAbsolute(string absolutePath) → string` — converts an absolute path to its real on-disk casing; returns the input unchanged on a cache miss or on a case-insensitive OS

## Cross-references

- **Uses:** _(no internal dependencies; defines a pure contract)_
- **Used by:** [ReflectionPathResolver.cs](../../Legacy/Paths/ReflectionPathResolver.cs.md), [PathResolver.cs](PathResolver.cs.md), [ShimPathResolver.cs](ShimPathResolver.cs.md), [PathResolverTests.cs](../../PluginSdkTests/PathResolverTests.cs.md)
