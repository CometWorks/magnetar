# Module: Legacy.Integration

**Project:** `Legacy` · **Files:** 7 · **Source lines:** 613

## Purpose

Provides the glue between the Legacy launcher and two cross-cutting concerns: (1) Roslyn script compilation, isolated in a separate AppDomain (.NET Framework) or a collectible AssemblyLoadContext (.NET Core), with compiler assembly references seeded from SE DS game DLLs; (2) case-insensitive path resolution on Linux, wired from the PluginSdk PathResolver facade to the LinuxCompat plugin's path cache via reflection. It also bridges Magnetar's ModPlugin data type to the SE DS mod-registration API (MyObjectBuilder_Checkpoint.ModItem, MyModContext), and hosts the server-side sender that transports plugin-declared mission-screen popups to clients through the bundled MagnetarMod world mod via SE multiplayer messaging.

## Role in Magnetar

Acts as the integration layer within the Legacy project, connecting Magnetar's abstractions and the PluginSdk facades to SE DS internals and optional OS-compatibility plugins. It is not a user-facing subsystem; it is called at launcher startup (compiler pipeline, path facade wiring) and, for mission screens, on demand whenever a plugin requests a popup.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `CompilerFactory (Interim)` | class | [`Legacy/Compiler/Interim.cs`](../descriptions/Legacy/Compiler/Interim.cs.md) | NETCOREAPP-only factory that loads Pulsar.Compiler into a collectible AssemblyLoadContext and creates RoslynCompiler instances via reflection-wrapped CompilerWrapper. |
| `CompilerWrapper` | class | [`Legacy/Compiler/Interim.cs`](../descriptions/Legacy/Compiler/Interim.cs.md) | File-local reflection bridge implementing ICompiler over a RoslynCompiler instance living in an isolated AssemblyLoadContext. |
| `CompilerLoadContext` | class | [`Legacy/Compiler/Interim.cs`](../descriptions/Legacy/Compiler/Interim.cs.md) | Collectible AssemblyLoadContext that resolves compiler-private assemblies from Libraries/MagnetarInterim/Compiler/. |
| `CompilerFactory (Legacy)` | class | [`Legacy/Compiler/Legacy.cs`](../descriptions/Legacy/Compiler/Legacy.cs.md) | NETFRAMEWORK-only factory that hosts Pulsar.Compiler in a separate AppDomain and returns RoslynCompiler as a transparent MarshalByRefObject proxy. |
| `References` | static class | [`Legacy/Compiler/References.cs`](../descriptions/Legacy/Compiler/References.cs.md) | Enumerates Roslyn compiler references: glob-matched SE game DLLs plus a fixed set of framework/library assembly names. |
| `ModPluginExtensions` | static class | [`Legacy/Extensions/ModPlugin.cs`](../descriptions/Legacy/Extensions/ModPlugin.cs.md) | Extension methods on ModPlugin that produce SE DS mod-registration objects (MyObjectBuilder_Checkpoint.ModItem and MyModContext). |
| `PathResolverBinder` | static class | [`Legacy/Paths/PathResolverBinder.cs`](../descriptions/Legacy/Paths/PathResolverBinder.cs.md) | One-shot startup helper that discovers LinuxCompat PathHelpers/PathCache types and installs a ReflectionPathResolver into the PluginSdk PathResolver facade. |
| `ReflectionPathResolver` | class | [`Legacy/Paths/ReflectionPathResolver.cs`](../descriptions/Legacy/Paths/ReflectionPathResolver.cs.md) | IPathResolver backend bound to LinuxCompat static methods via pre-created delegates; zero reflection overhead on the hot path. |
| `MissionScreenSender` | static class | [`Legacy/Integration/MissionScreenSender.cs`](../descriptions/Legacy/Integration/MissionScreenSender.cs.md) | Host-side implementation of the PluginSdk MissionScreens facade; serializes a MissionScreenContent into a versioned ProtoBuf packet and sends it to a Steam id, an identity, or all online players via MyAPIGateway.Multiplayer.SendMessageTo on the game thread, gated on the bundled MagnetarMod client receiver being enabled as a world mod. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Legacy/Compiler/Interim.cs`](../descriptions/Legacy/Compiler/Interim.cs.md) | 147 | Active only under `#if NETCOREAPP` (the Interim/.NET 10 build). |
| [`Legacy/Compiler/Legacy.cs`](../descriptions/Legacy/Compiler/Legacy.cs.md) | 86 | Active only under `#if NETFRAMEWORK` (the .NET Framework 4.8 / Windows build). |
| [`Legacy/Compiler/References.cs`](../descriptions/Legacy/Compiler/References.cs.md) | 36 | Provides the list of assembly references that the Roslyn compiler must know about when compiling SE scripts and plugins. |
| [`Legacy/Extensions/ModPlugin.cs`](../descriptions/Legacy/Extensions/ModPlugin.cs.md) | 31 | Extends `ModPlugin` (the Magnetar data type representing a Steam Workshop mod) with the SE DS API objects needed to register a mod with the game engine at runtime. |
| [`Legacy/Integration/MissionScreenSender.cs`](../descriptions/Legacy/Integration/MissionScreenSender.cs.md) | 142 | Host-side sender that delivers plugin-declared mission-screen popups to clients over Space Engineers' multiplayer messaging API. |
| [`Legacy/Paths/PathResolverBinder.cs`](../descriptions/Legacy/Paths/PathResolverBinder.cs.md) | 77 | Wires the `PluginSdk.Paths.PathResolver` facade to the LinuxCompat plugin's case-insensitive path cache at startup. |
| [`Legacy/Paths/ReflectionPathResolver.cs`](../descriptions/Legacy/Paths/ReflectionPathResolver.cs.md) | 94 | An `IPathResolver` backend that forwards path operations to the LinuxCompat plugin's `PathHelpers` and `PathCache` static methods via pre-bound delegates. |

## Public API surface

- `PathResolverBinder.Bind() — called once at launcher startup to wire Linux case-insensitive path resolution`
- `ModPluginExtensions.GetModItem(ModPlugin) — converts a Magnetar mod descriptor to MyObjectBuilder_Checkpoint.ModItem for SE DS registration`
- `ModPluginExtensions.GetModContext(ModPlugin) — converts a Magnetar mod descriptor to MyModContext for SE DS registration`
- `References.GetReferences(exeLocation) — enumerates all Roslyn compiler assembly references for a given game directory`
- `CompilerFactory.Create(debugBuild) — creates a ready-to-use ICompiler instance with appropriate preprocessor flags`
- `MissionScreenSender.ShowToPlayer/ShowToSteam/ShowToAll(MissionScreenContent) — internal host backends bound to the PluginSdk MissionScreens facade that send mission-screen popups to clients over the SE multiplayer channel`

## Dependencies

**Uses modules:** [Compiler](Compiler.md), [Legacy.Launcher](Legacy.Launcher.md), [Legacy.Loader](Legacy.Loader.md), [PluginSdk.Runtime](PluginSdk.Runtime.md), [Shared.Core](Shared.Core.md), [Shared.Data](Shared.Data.md)  
**Used by modules:** [Legacy.Launcher](Legacy.Launcher.md), [Legacy.Loader](Legacy.Loader.md)  
**External systems:** LinuxCompat plugin (runtime optional); SE DS assemblies

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
