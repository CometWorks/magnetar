# Legacy/Launcher/Folder.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Launcher` · **Kind:** static helper class · **Lines:** 161

## Summary
Locates the Space Engineers Dedicated Server `DedicatedServer64` installation directory so the launcher knows which game binaries to load and patch. It probes four sources in priority order — explicit `-ds64` command-line override, the Steam-supplied launch argument that points at `SpaceEngineersDedicated.exe`, the Steam library `libraryfolders.vdf` manifest, and finally the Windows registry uninstall key. Because Magnetar runs the DS on both native Windows (.NET Framework) and on Linux/.NET via the `se-linux-compat`/Proton path, the resolver tolerates Unix-style paths (rewriting `/...` to a `Z:` drive when not running natively) and gates the Windows-only registry/VDF probes accordingly.

## Types
### Folder — class, internal
Static-only utility (all members `static`) that resolves and validates the DS64 directory. A directory qualifies only if it exists and contains the full set of marker DS files, guaranteeing the launcher never targets a partial or wrong install.

- **Fields:**
  - `registryKey` (const) — format string for the per-app Steam uninstall registry path `SOFTWARE\...\Uninstall\Steam App {0}`; `{0}` is filled with `Steam.AppIdSe1DS`.
  - `registryName` (const) — registry value name `InstallLocation` read from that key.
  - `dsLauncher` (const) — `"SpaceEngineersDedicated.exe"`, the DS entry executable used both as a marker file and as the token searched for in Steam launch args.
  - `dsFiles` (static readonly HashSet) — the marker files (`SpaceEngineersDedicated.exe`, `SpaceEngineers.Game.dll`, `VRage.dll`, `Sandbox.Game.dll`) that all must be present for a directory to count as a valid DS64.
- **Methods:**
  - `GetDS64()` — public entry point; returns the first non-null result of `FromOverride()` → `FromSteamArgs()` → `FromSteamFiles()` → `FromRegistry()`, or null if none resolve.
  - `IsDS64(path)` — validates a candidate directory: exists and every file in `dsFiles` is present.
  - `TryConvertUnix(path)` — when not running natively (`Tools.IsNative()` false) and the path is an absolute Unix path (`/...`), prefixes it with `Z:` so the .NET Framework DS sees a Windows-style drive path; otherwise returns it unchanged.
  - `FromRegistry()` — Windows-only (guarded by `RuntimeInformation.IsOSPlatform(Windows)`, with CA1416 suppressed because the analyzer can't see the guard across net48/net10). Opens the 64-bit `HKLM` uninstall key for the DS app, reads `InstallLocation`, appends `DedicatedServer64`, and returns it if valid.
  - `FromOverride()` — scans `Environment.GetCommandLineArgs()` for `-ds64 <path>`; resolves relative paths against the executing assembly's directory, converts rooted Unix paths via `TryConvertUnix`, validates, and returns the full path.
  - `FromSteamArgs()` — finds launch args that contain both `DedicatedServer64` and the DS launcher exe name (how Steam passes the executable), converts them, takes their directory, and returns the first valid one.
  - `FromSteamFiles()` — native-only. Reads Steam's `steamapps/libraryfolders.vdf` (via `Gameloop.Vdf`), iterates each library, checks whether it lists the DS app id under `apps`, and if so builds the canonical `...common/SpaceEngineersDedicatedServer/DedicatedServer64` path and validates it.

## Cross-references
- **Uses:** `Pulsar.Shared` (`Tools.IsNative`, `Steam.AppIdSe1DS`, `Steam.GetSteamPath`); `Gameloop.Vdf` / `Gameloop.Vdf.Linq` (VDF parsing of `libraryfolders.vdf`); `Microsoft.Win32` registry; `System.Runtime.InteropServices.RuntimeInformation` — repo-relative: `Shared/` (Tools, Steam).
- **Used by:** [ModPlugin.cs](../Extensions/ModPlugin.cs.md), [Program.cs](../Program.cs.md)
