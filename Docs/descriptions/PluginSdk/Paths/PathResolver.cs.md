# PluginSdk/Paths/PathResolver.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Paths` · **Kind:** static class · **Lines:** 48

## Summary

Plugin-facing static facade for cross-platform, case-insensitive path resolution. Plugins call these methods unconditionally without caring whether the server is running on Windows or Linux. At process start the static field is initialized to a `ShimPathResolver` (a pass-through correct on Windows). When the LinuxCompat plugin is loaded, the host calls `Install` once to swap in its real case-insensitive backend. All subsequent calls transparently delegate to whichever backend is active.

## Types

### `PathResolver` — static class, public

Thin delegation layer over `IPathResolver`. Holds a single private `_backend` field that starts as `ShimPathResolver` and may be replaced exactly once at startup. Every public method simply forwards to `_backend`, giving plugins a stable, no-overhead API surface while allowing the host to inject the real Linux path resolver.

- **Fields:**
  - `_backend` (`IPathResolver`, private static) — the currently active resolver; initialized to `new ShimPathResolver()`

- **Properties:**
  - `IsCaseInsensitiveResolverActive` (`bool`, public static) — returns `true` when `_backend` is not the shim, i.e. a real case-insensitive resolver (e.g. LinuxCompat) has been installed; plugins can branch on this to skip unnecessary work on Windows

- **Methods:**
  - `Install(IPathResolver backend)` — host-only; replaces `_backend`; a `null` argument resets to the shim (last-wins semantics, not guarded beyond that)
  - `Normalize(string path) → string` — delegates to `_backend.Normalize`
  - `ToWindowsPath(string path) → string` — delegates to `_backend.ToWindowsPath`
  - `GetFileName(string path) → string` — delegates to `_backend.GetFileName`
  - `GetFileNameWithoutExtension(string path) → string` — delegates to `_backend.GetFileNameWithoutExtension`
  - `ResolveContentFilePath(string relativePath, string rootPath) → string` — delegates to `_backend.ResolveContentFilePath`
  - `ResolveAbsolute(string absolutePath) → string` — delegates to `_backend.ResolveAbsolute`

## Cross-references

- **Uses:** `PluginSdk/Paths/IPathResolver.cs`, `PluginSdk/Paths/ShimPathResolver.cs`
- **Used by:** [PathResolverBinder.cs](../../Legacy/Paths/PathResolverBinder.cs.md), [PathResolverTests.cs](../../PluginSdkTests/PathResolverTests.cs.md)
