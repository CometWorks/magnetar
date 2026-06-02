# Path Resolver: Case-Insensitive Paths on Linux

`PluginSdk.Paths.PathResolver` is a static facade that resolves filesystem
paths case-insensitively, so a plugin that touches files works unchanged on
both Windows and Linux. You call the same methods everywhere; the host wires
the right backend behind them.

## The problem it solves

Space Engineers and its mods are authored on Windows, where the filesystem is
**case-insensitive**: `Content\Textures\Foo.dds` opens the file regardless of
the on-disk casing. On Linux the filesystem is **case-sensitive**, so a literal
Windows-style path with the wrong casing simply fails to open. A plugin that
builds paths from game data (mod paths, content-relative asset names, atlas
manifests) breaks on Linux unless it fixes the casing itself.

The `se-linux-compat` plugin maintains a two-level case-insensitive path cache
for exactly this. `PathResolver` exposes that cache to **every** plugin, so you
don't reinvent it — and on Windows the same calls are cheap pass-throughs.

## How it behaves on each platform

| Platform | Backend | Behaviour |
|---|---|---|
| **Linux** (se-linux-compat loaded) | forwards to the LinuxCompat path cache | real case-insensitive resolution against the on-disk tree |
| **Windows** (se-linux-compat absent) | built-in no-op shim | paths pass through unchanged (the OS is already case-insensitive) |

The host installs the backend once at startup, **before** plugins are
initialised, so you may call `PathResolver` from your `Init()`. Until a backend
is installed the shim is active, so the calls are always safe.

> The backend is bound by reflection against the LinuxCompat assembly. You never
> reference `se-linux-compat` from your plugin — you only depend on `PluginSdk`,
> and the indirection keeps the actual cache living in (and updated with) the
> compat plugin.

## The API

```csharp
using PluginSdk.Paths;

// Resolve a content-relative path against a known root (mod/content dir):
string real = PathResolver.ResolveContentFilePath("Textures/Foo.dds", modRoot);

// Resolve an absolute path to its real on-disk casing:
string abs = PathResolver.ResolveAbsolute(@"C:\Game\Content\Data\Blocks.sbc");

// Cross-platform filename helpers (treat '\' as a separator even on Linux):
string name = PathResolver.GetFileName(@"Textures\Foo.dds");          // Foo.dds
string stem = PathResolver.GetFileNameWithoutExtension(@"a\b\c.mwm"); // c

// Lower-level helpers:
string norm = PathResolver.Normalize(@"a\b\c");   // forward slashes, trimmed
string win  = PathResolver.ToWindowsPath("/tmp/x"); // Windows-shape, mod egress
```

| Method | Use it when |
|---|---|
| `ResolveContentFilePath(relative, root)` | You have a content/mod-relative path and the root it lives under. Returns the real-cased full path. Most common entry point. |
| `ResolveAbsolute(absolute)` | You already have a full path (e.g. built from `ModContext.ModPath`) and need its real on-disk casing before opening it. |
| `GetFileName(path)` / `GetFileNameWithoutExtension(path)` | You need the leaf of a path that may use `\` separators from game data — `System.IO.Path` only treats `\` as a separator on Windows. |
| `Normalize(path)` | You need separators flipped to `/` and whitespace trimmed (what the resolver does internally before matching). |
| `ToWindowsPath(path)` | You are handing a path **back to a mod** and want it in Windows shape (drive letter, backslashes). Read-only egress — do not feed the result back to the filesystem. |
| `IsCaseInsensitiveResolverActive` | Diagnostics: `true` when the real LinuxCompat cache is wired, `false` while the shim is active. |

On the shim (Windows) every method is a pass-through except the filename
helpers, which delegate to `System.IO.Path`, and `ResolveContentFilePath`,
which does a plain `Path.Combine` with the root.

## Typical use

```csharp
using System.IO;
using PluginSdk.Paths;

// A mod/content asset whose casing came from game data and may not match disk:
string path = PathResolver.ResolveContentFilePath(relativeAssetPath, modRoot);
if (File.Exists(path))
    using (var s = File.OpenRead(path)) { /* ... */ }
```

Write the path-handling code once, against `PathResolver`. It resolves
correctly on Linux and stays a cheap no-op on Windows — no platform checks in
your plugin.

## Notes and limits

- Resolution returns the **input unchanged on a miss** (path not found in the
  tree). Always check existence after resolving; a returned path is not a
  guarantee the file exists.
- The LinuxCompat cache is populated lazily as the game initialises the
  filesystem. Calling `PathResolver` extremely early (before the game's
  `MyFileSystem` is up) degrades gracefully to pass-through until the cache is
  ready — fine for `Init()` and anything later.
- `ToWindowsPath` is for **egress to mods only**. The engine never round-trips
  those strings through the filesystem; don't open files with them.
- There is nothing to register and no host id to pass — the facade is global and
  always present. You only need `using PluginSdk.Paths;`.
