# Module: Legacy.Loader

**Project:** `Legacy` · **Files:** 6 · **Source lines:** 1078

## Purpose

Runtime plugin host and native bootstrap for the SE1 dedicated server. It instantiates compiled plugins, drives their SE lifecycle (Init/Update/HandleInput/Dispose), registers their SE session and entity components, applies late Harmony patches, wires up the chat-command pipeline and mission-screen popup senders, prefetches Steam Workshop mod content (including the implicit MagnetarMod client companion), and on Linux preloads native libraries and aliases Windows DLL names to their .so equivalents.

## Role in Magnetar

This is the heart of Magnetar's launcher on the Legacy (.NET Framework 4.8 / Windows) and Interim (.NET 10 / Linux) targets: the single SE-visible IHandleInputPlugin (PluginLoader) that fans the engine lifecycle out to every loaded Magnetar plugin, quarantines misbehaving plugins via first-chance exception attribution, manages the bundled MagnetarMod client world mod that backs plugin mission-screen popups, and supplies the cross-platform plumbing (process restart, JIT precompile, native library resolution) needed to run the headless DS on both platforms.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `PluginLoader` | class | [`Legacy/Loader/PluginLoader.cs`](../descriptions/Legacy/Loader/PluginLoader.cs.md) | Singleton IHandleInputPlugin host that instantiates, initializes and drives all loaded plugins and owns the command pipeline. |
| `PluginInstance` | class | [`Legacy/Loader/PluginInstance.cs`](../descriptions/Legacy/Loader/PluginInstance.cs.md) | Wrapper around one plugin's IPlugin object: lifecycle, DI, SE component registration and error isolation. |
| `SteamMods` | static class | [`Legacy/Loader/SteamMods.cs`](../descriptions/Legacy/Loader/SteamMods.cs.md) | Reflection-bridged wrapper over SE's internal MyWorkshop downloader to prefetch mod-plugin workshop content; also expands legacy `*_legacy.bin` archives after download and tests whether a mod is untrusted. |
| `MagnetarClientMod` | static class | [`Legacy/Loader/MagnetarClientMod.cs`](../descriptions/Legacy/Loader/MagnetarClientMod.cs.md) | Policy + mechanics for the implicit MagnetarMod client companion world mod (workshop id 3750200326): injects it into workshop-id sets and checkpoint mod lists, removing/skipping it under -noimplicitmod or any crossplay configuration. |
| `NativeLibraryPreloader` | static class | [`Legacy/Loader/NativeLibraryPreloader.cs`](../descriptions/Legacy/Loader/NativeLibraryPreloader.cs.md) | Linux-only native bootstrap: dlopens bundled .so files and aliases Windows DLL names across all AssemblyLoadContexts. |
| `LoaderTools` | static class | [`Legacy/Loader/LoaderTools.cs`](../descriptions/Legacy/Loader/LoaderTools.cs.md) | Process restart (execv on Linux, Process.Start on Windows) and assembly JIT precompilation utilities. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Legacy/Loader/LoaderTools.cs`](../descriptions/Legacy/Loader/LoaderTools.cs.md) | 137 | Process-level utilities for the loader: restarting the dedicated server process with adjusted command-line arguments, and force-precompiling (JIT-preparing) plugin assemblies so member-access errors surface immediately instead of mid-game. |
| [`Legacy/Loader/MagnetarClientMod.cs`](../descriptions/Legacy/Loader/MagnetarClientMod.cs.md) | 102 | Manages the bundled **MagnetarMod** client companion world mod (Steam workshop id `3750200326`), the script-side counterpart clients must load so that plugin-driven mission-screen popups have receiving code. |
| [`Legacy/Loader/NativeLibraryPreloader.cs`](../descriptions/Legacy/Loader/NativeLibraryPreloader.cs.md) | 154 | Linux-only native-library bootstrap that runs once at the very top of `Main()`. |
| [`Legacy/Loader/PluginInstance.cs`](../descriptions/Legacy/Loader/PluginInstance.cs.md) | 336 | Runtime wrapper around a single loaded plugin: it locates the plugin's `IPlugin` implementation type in the compiled assembly, instantiates it, performs reflection-based dependency injection of loader services into well-known static fields/methods, and drives the SE plugin lifecycle (`Init` / `Update` / `HandleInput` / `Dispose`). |
| [`Legacy/Loader/PluginLoader.cs`](../descriptions/Legacy/Loader/PluginLoader.cs.md) | 229 | The top-level plugin host: a singleton `IHandleInputPlugin` that SE itself drives (`Init`/`Update`/`HandleInput`/`Dispose`). |
| [`Legacy/Loader/SteamMods.cs`](../descriptions/Legacy/Loader/SteamMods.cs.md) | 120 | Downloads/updates Steam Workshop items (mod-plugins referenced by the active profile) by reproducing SE's own blocking workshop-download path. |

## Public API surface

- `PluginLoader.Instance / Init(object) / Update() / HandleInput() / Dispose()`
- `PluginLoader.TryGetPluginInstance(string, out PluginInstance)`
- `PluginLoader.RegisterSessionComponents() / RegisterEntityComponents()`
- `PluginInstance.TryGet(PluginData, Assembly, out PluginInstance)`
- `SteamMods.Update(IEnumerable<ulong>) / IsModUntrusted(ModItem)`
- `MagnetarClientMod.WorkshopId / GetWorkshopIdsForUpdate(IEnumerable<ulong>) / ApplyToCheckpoint(MyObjectBuilder_Checkpoint) / ApplyToModList(...)`
- `NativeLibraryPreloader.Initialize(string baseDir)`
- `LoaderTools.Restart(bool, bool?) / Precompile(Assembly)`

## Dependencies

**Uses modules:** [Legacy.Commands](Legacy.Commands.md), [Legacy.Integration](Legacy.Integration.md), [PluginSdk.Commands](PluginSdk.Commands.md), [PluginSdk.Logging](PluginSdk.Logging.md), [PluginSdk.Runtime](PluginSdk.Runtime.md), [Shared.Config](Shared.Config.md), [Shared.Core](Shared.Core.md), [Shared.Data](Shared.Data.md)  
**Used by modules:** [Legacy.Integration](Legacy.Integration.md), [Legacy.Launcher](Legacy.Launcher.md), [Legacy.Patch](Legacy.Patch.md)  
**External systems:** Harmony; NuGet; PluginSdk; SE DS assemblies; Steam

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
