# Module: Legacy.Launcher

**Project:** `Legacy` · **Files:** 5 · **Source lines:** 1455

## Purpose

The launcher bootstrap for Magnetar's Space Engineers Dedicated Server loader. It is the process entry point: it locates and validates the DS install, sets up config/logging, wires Steam and assembly resolvers, loads and preload-patches plugins, owns the server lifecycle (save/reload/quit/restart with POSIX signals), and finally hands off to the real SE dedicated server.

## Role in Magnetar

This is the outermost layer of the Legacy launcher — it runs before any SE game code and is conditionally compiled for the Windows/.NET 4.8 "Legacy" build and the Linux/.NET 10 "Interim" build. It bridges Magnetar's own subsystems (Shared core/config, Compiler, Loader, Patch, PluginSdk) into the SE engine via reflection and Harmony, then invokes SpaceEngineersDedicated.exe's hidden Main. It is the home of the cross-environment startup sequencing and the single source of truth for server lifecycle/shutdown.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `Folder` | static class | [`Legacy/Launcher/Folder.cs`](../descriptions/Legacy/Launcher/Folder.cs.md) | Resolves and validates the SE DedicatedServer64 directory from -ds64 override, Steam launch args, Steam library VDF, or the Windows registry. |
| `Game` | static class | [`Legacy/Launcher/Game.cs`](../descriptions/Legacy/Launcher/Game.cs.md) | Reflection/Mono.Cecil bridge into SE engine internals: registers the plugin, sets MyFileSystem paths, reads the game version from IL, and starts the DS. |
| `GameLog` | class | [`Legacy/Launcher/Game.cs`](../descriptions/Legacy/Launcher/Game.cs.md) | Adapts SE's MyLog.Default to Magnetar's IGameLog (exists/open/write). |
| `ServerControl` | static class | [`Legacy/Launcher/ServerControl.cs`](../descriptions/Legacy/Launcher/ServerControl.cs.md) | Single source of truth for server lifecycle (save/reload/quit/restart), backing POSIX signal handlers and the PluginSdk.ServerControl facade; waits for Keen save-idle state and is idempotent. |
| `Program` | static class | [`Legacy/Program.cs`](../descriptions/Legacy/Program.cs.md) | Process entry point orchestrating the full startup pipeline (incl. -help and telemetry-consent resolution) for both the Legacy (net48) and Interim (net10) builds. |
| `ExternalTools` | class | [`Legacy/Program.cs`](../descriptions/Legacy/Program.cs.md) | IExternalTools adapter that marshals shared-code actions onto the SE game thread via Game.RunOnGameThread. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Legacy/Launcher/Daemon.cs`](../descriptions/Legacy/Launcher/Daemon.cs.md) | 164 | Detaches the running process from its parent (typically Quasar) when the `-daemon` flag is set, so the parent terminating does not take the dedicated server down with it. |
| [`Legacy/Launcher/Folder.cs`](../descriptions/Legacy/Launcher/Folder.cs.md) | 161 | Locates the Space Engineers Dedicated Server `DedicatedServer64` installation directory so the launcher knows which game binaries to load and patch. |
| [`Legacy/Launcher/Game.cs`](../descriptions/Legacy/Launcher/Game.cs.md) | 141 | Thin bridge between Magnetar's launcher and the Space Engineers DS engine internals (`Sandbox`, `VRage`). |
| [`Legacy/Launcher/ServerControl.cs`](../descriptions/Legacy/Launcher/ServerControl.cs.md) | 525 | Single source of truth for the dedicated server's lifecycle operations — save world, reload dedicated config, quit, and restart — with and without saving. |
| [`Legacy/Program.cs`](../descriptions/Legacy/Program.cs.md) | 464 | Entry point for the Magnetar launcher. |

## Public API surface

- `Program.Main(string[] args) — process entry point (handles -help and -consent/-noconsent/-withdraw-consent before server start)`
- `Folder.GetDS64() — resolve the DS64 install directory`
- `Game.RegisterPlugin / SetMainAssembly / GetGameVersion / StartDedicatedServer / RunOnGameThread — SE engine bridge`
- `ServerControl.CaptureLaunchState / InstallSignalHandlers / SaveWorld / ReloadConfig / SaveAndQuit / QuitWithoutSaving / SaveAndRestart / RestartWithoutSaving — lifecycle control`

## Dependencies

**Uses modules:** [Legacy.Integration](Legacy.Integration.md), [Legacy.Loader](Legacy.Loader.md), [Legacy.Patch](Legacy.Patch.md), [Shared.Config](Shared.Config.md), [Shared.Core](Shared.Core.md), [Shared.Votes](Shared.Votes.md)  
**Used by modules:** [Legacy.Commands](Legacy.Commands.md), [Legacy.Integration](Legacy.Integration.md)
**External systems:** GitHub; Harmony; NuGet; SE DS assemblies; Steam

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
