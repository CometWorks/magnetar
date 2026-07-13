# ConfigTerminal/Io/PlatformPaths.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Io` · **Kind:** static class · **Lines:** 29

## Summary
Platform helpers used throughout the Io module: OS detection (`IsWindows`/`IsLinux`) and filesystem-correct path comparison (case-insensitive on Windows, case-sensitive on Linux) for world-folder identity checks.

## Types
### PlatformPaths — static class, internal
- **Properties:**
  - `IsWindows` / `IsLinux` (bool) — from `RuntimeInformation.IsOSPlatform`.
  - `PathComparer` (`StringComparer`) — `OrdinalIgnoreCase` on Windows, else `Ordinal`.
  - `PathComparison` (`StringComparison`) — the matching comparison mode.
- **Methods:**
  - `GetRelativePath(relativeTo, path)` — thin wrapper over `System.IO.Path.GetRelativePath`.

## Cross-references
- **Uses:** `System.Runtime.InteropServices.RuntimeInformation`/`OSPlatform`; `System.StringComparer`/`StringComparison`; `System.IO.Path`.
- **Used by:** [AtomicFile.cs](AtomicFile.cs.md), [InstanceLocator.cs](InstanceLocator.cs.md), [LastSessionFile.cs](../Model/LastSessionFile.cs.md), [PluginSourcesDocument.cs](../Model/PluginSourcesDocument.cs.md), [MagnetarProcess.cs](../Process/MagnetarProcess.cs.md), [PidFileReader.cs](../Process/PidFileReader.cs.md), [Program.cs](../Program.cs.md), [AppShell.cs](../Ui/AppShell.cs.md)
