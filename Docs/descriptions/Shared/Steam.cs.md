# Shared/Steam.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared` · **Kind:** static class · **Lines:** 71

## Summary
Thin Steam helper for the Dedicated Server: resolves the Steam install path cross-platform and redirects `Steamworks.NET` assembly resolution to a bundled copy. Workshop capability and item state are handled through SE's registered game-service APIs in `Legacy.Loader`, leaving this shared helper without a direct `Steamworks.NET` compile-time dependency.

## Types
### `Steam` — static class, public
Holds SE app-id constants and stateless Steam utility methods.
- **Fields (const):** `AppIdSe1=244850` (SE1 game), `AppIdSe1DS=298740` (SE1 Dedicated Server), `AppIdSe2=1133870` (SE2); `registryKey=@"SOFTWARE\Valve\Steam"`, `registryName="SteamPath"`, `Steamworks="Steamworks.NET"`.
- **Methods:** `SteamworksResolver(string baseDir)` — returns a `ResolveEventHandler` that loads `Steamworks.NET.dll` from `baseDir` when that assembly is requested (and only that assembly); `GetSteamPath()` — Windows: reads the registry path; Linux: returns `~/.steam/steam` if it exists, else null (uses `RuntimeInformation.IsOSPlatform` so it compiles for both net48 and net10.0); `GetWindowsSteamPath()` — opens HKCU 64-bit `SOFTWARE\Valve\Steam` and reads `SteamPath` (guarded by `#pragma warning disable CA1416` for the cross-TFM Windows-only registry call).

## Cross-references
- **Uses:** `Microsoft.Win32.Registry`; `System.Runtime.InteropServices.RuntimeInformation`; external Steam installation layout and the runtime `Steamworks.NET` assembly name.
- **Used by:** [ModPlugin.cs](../Legacy/Extensions/ModPlugin.cs.md), [Folder.cs](../Legacy/Launcher/Folder.cs.md), [MagnetarClientMod.cs](../Legacy/Loader/MagnetarClientMod.cs.md), [SteamMods.cs](../Legacy/Loader/SteamMods.cs.md), [Patch_Compile.cs](../Legacy/Patch/Patch_Compile.cs.md), [Program.cs](../Legacy/Program.cs.md), [ConsentManager.cs](Votes/ConsentManager.cs.md)
