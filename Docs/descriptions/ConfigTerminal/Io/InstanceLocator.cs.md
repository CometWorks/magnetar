# ConfigTerminal/Io/InstanceLocator.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Io` · **Kind:** static class, sealed class · **Lines:** 161

## Summary
Resolves the default DS data dir, Magnetar config dir, Magnetar launcher executable, and DS install (`DedicatedServer64`) locations using the same per-platform semantics Magnetar itself uses, so a non-standard deployment resolves end to end. Explicit CLI values always win: `ResolveDefaults` only fills fields the user left unset and nothing silently falls back past a value the user gave. On Windows it also enumerates the launcher variants (Legacy/Interim) actually installed under `%APPDATA%\Magnetar` so the operator can pick which one to configure.

## Types
### MagnetarLauncher — sealed class, internal
A Windows Magnetar launcher variant the tool can configure.

- **Fields:**
  - `Name` (string) — assembly/exe base name, e.g. `"MagnetarLegacy"`.
  - `Label` (string) — human label shown in the picker.
  - `ConfigDir` (string) — where this launcher reads its `config.xml` / pid.
  - `ExePath` (string) — launcher executable to start/stop.

### InstanceLocator — static class, internal
Path-resolution helpers keyed off `PlatformPaths.IsWindows`, XDG env vars, and the user profile.

- **Properties:**
  - `Home` (private) — `Environment.SpecialFolder.UserProfile`.
- **Methods:**
  - `DefaultDataDir()` — DS data dir (cfg + Saves): `%APPDATA%\SpaceEngineersDedicated` on Windows, else `~/.config/SpaceEngineersDedicated`.
  - `DefaultMagnetarConfigDir()` — Magnetar config dir (config.xml, logs, `magnetar.pid`): `%APPDATA%\Magnetar\MagnetarLegacy` on Windows; on Linux honors `XDG_CONFIG_HOME` (appending `Magnetar`) else `~/.config/Magnetar`.
  - `DefaultMagnetarExe()` — launcher executable to spawn: `%APPDATA%\Magnetar\MagnetarLegacy.exe` on Windows; on Linux honors `XDG_DATA_HOME` (else `~/.local/share`) with `Magnetar/MagnetarInterim`.
  - `WindowsMagnetarRoot()` (private) — the `%APPDATA%\Magnetar` deployment root.
  - `PresentWindowsLaunchers()` — returns the `MagnetarLauncher`s whose `.exe` exists under the Windows Magnetar root (Legacy first, then Interim); empty off Windows. Each launcher's `ConfigDir` is filled via `ResolveLauncherConfigDir`.
  - `ResolveLauncherConfigDir(root, name)` (private) — mirrors the launcher's own resolution (`Legacy\Program.cs` `GetConfigDir`): the folder named after the launcher if it exists, else the shared `MagnetarLegacy` fallback.
  - `DetectDs64()` — best-effort DS install auto-detection; returns the full path of the first candidate that `IsDs64`, else `null`.
  - `Ds64Candidates()` (private) — yields platform candidate paths: the Steam `common\SpaceEngineersDedicatedServer\DedicatedServer64` path under Program Files on Windows, or under `~/.steam/steam` and `~/.local/share/Steam` on Linux.
  - `IsDs64(dir)` (private) — true when the dir exists and contains `SpaceEngineersDedicated.exe` or `VRage.dll` (the launcher is the reliable marker).
  - `ResolveDefaults(InstanceBinding)` — fills any unset `DataDir`, `MagnetarConfigDir`, `MagnetarExePath`, `Ds64Dir` with their resolved defaults (`??=`) and returns the binding.

## Cross-references
- **Uses:** `PlatformPaths` (this module); `InstanceBinding` (`ConfigTerminal/Model/`); `System.IO.Path`/`File`/`Directory`; `System.Environment` (`SpecialFolder`, env vars).
- **Used by:** [Cli.cs](../Cli.cs.md), [Program.cs](../Program.cs.md), [InstancePickerDialog.cs](../Ui/InstancePickerDialog.cs.md)
