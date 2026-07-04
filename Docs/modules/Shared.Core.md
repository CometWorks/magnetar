# Module: Shared.Core

**Project:** `Shared` · **Files:** 11 · **Source lines:** 2240

## Purpose

The core bootstrap layer of the Magnetar plugin/mod loader. It discovers and caches plugin metadata from GitHub hubs, single repos, Steam Workshop mods and local folders; compiles/loads enabled plugins per the active profile; performs pre-launch sanity checks; patches SE DS assemblies on disk (preloader mechanism); self-updates against GitHub; and provides cross-cutting services (logging, hashing, Steam path/UGC, assembly resolution, console progress).

## Role in Magnetar

This is the environment-agnostic heart of the launcher/SDK. The Legacy (.NET Framework 4.8 / Windows) and Interim (.NET 10 / Linux) front-ends call into these types to load plugins and patch the SE Dedicated Server. It sits above Shared.Config/Data/Network/Votes and the Compiler, and below the environment-specific Launcher/Loader/Integration modules, exposing the Loader, PluginList, Preloader and Updater that those front-ends drive.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `Loader` | class | [`Shared/Loader.cs`](../descriptions/Shared/Loader.cs.md) | Instantiates all enabled plugins, validates Harmony, wires stats, reports usage. |
| `PluginList` | class | [`Shared/PluginList.cs`](../descriptions/Shared/PluginList.cs.md) | Aggregates plugin metadata from remote hubs, repos, mods and local sources into one keyed catalog with caching and dependency resolution. |
| `Preloader` | class | [`Shared/Preloader.cs`](../descriptions/Shared/Preloader.cs.md) | Mono.Cecil on-disk patcher for SE DS DLLs via 'Preloader' plugin types with Patch/Initialize/Finish hooks. |
| `Updater` | class | [`Shared/Updater.cs`](../descriptions/Shared/Updater.cs.md) | Self-updates Magnetar against a GitHub release repo by launching an external Updater.exe. |
| `AssemblyResolver` | class | [`Shared/AssemblyResolver.cs`](../descriptions/Shared/AssemblyResolver.cs.md) | Scoped AssemblyResolve handler serving allow-listed requesters from registered source folders. |
| `Tools` | static class | [`Shared/Tools.cs`](../descriptions/Shared/Tools.cs.md) | Cross-cutting utilities: hashing, message reporting, deep copy, interactive-terminal detection, native crash handler, injected service holders. |
| `IExternalTools` | interface | [`Shared/Tools.cs`](../descriptions/Shared/Tools.cs.md) | Bridge to run an Action on the SE main thread. |
| `Steam` | static class | [`Shared/Steam.cs`](../descriptions/Shared/Steam.cs.md) | Cross-platform Steam path resolution, Steamworks.NET resolver, and game-server UGC install checks. |
| `LogFile` | static class | [`Shared/LogFile.cs`](../descriptions/Shared/LogFile.cs.md) | NLog-backed central logging facade writing per-start info_*.log files, fail-soft. |
| `IGameLog` | interface | [`Shared/LogFile.cs`](../descriptions/Shared/LogFile.cs.md) | Abstraction over the SE DS native log. |
| `Flags` | static class | [`Shared/Flags.cs`](../descriptions/Shared/Flags.cs.md) | Parses Magnetar command-line switches once into read-only flags (incl. -noimplicitmod, consent and -help) and renders the usage screen. |
| `UpdateType` | enum | [`Shared/Flags.cs`](../descriptions/Shared/Flags.cs.md) | Self-update channel: None/Standard/Tester. |
| `ConsentChoice` | enum | [`Shared/Flags.cs`](../descriptions/Shared/Flags.cs.md) | Telemetry-consent intent from the command line: Unset/Accept/Deny/Withdraw. |
| `Launcher` | class | [`Shared/Launcher.cs`](../descriptions/Shared/Launcher.cs.md) | Pre-launch sanity checks (SE running, dropped -plugin switch, config presence). |
| `PluginProgress` | static class | [`Shared/PluginProgress.cs`](../descriptions/Shared/PluginProgress.cs.md) | Plain-text console/log progress for plugin download and compilation. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Shared/AssemblyResolver.cs`](../descriptions/Shared/AssemblyResolver.cs.md) | 107 | Provides a scoped `AppDomain.AssemblyResolve` handler that satisfies managed assembly load requests from one or more "source" folders, but only when the *requesting* assembly is on an allow-list. |
| [`Shared/Flags.cs`](../descriptions/Shared/Flags.cs.md) | 171 | Parses Magnetar's own command-line switches once at startup (in a static constructor) and exposes them as read-only boolean/string/enum flags for the rest of the loader. |
| [`Shared/Launcher.cs`](../descriptions/Shared/Launcher.cs.md) | 52 | Performs pre-launch sanity checks before Magnetar starts the SE Dedicated Server: refuses to start if the SE process is already running, rejects the removed `-plugin` switch, and verifies that an app `.config` exists when the SE folder ships one. |
| [`Shared/Loader.cs`](../descriptions/Shared/Loader.cs.md) | 156 | The orchestrator that instantiates all enabled plugins at startup. |
| [`Shared/LogFile.cs`](../descriptions/Shared/LogFile.cs.md) | 130 | Magnetar's central logging facade writing per-start `info_*.log` files. |
| [`Shared/PluginList.cs`](../descriptions/Shared/PluginList.cs.md) | 868 | The plugin catalog. |
| [`Shared/PluginProgress.cs`](../descriptions/Shared/PluginProgress.cs.md) | 45 | Plain-text console progress reporter for plugin download and compilation, replacing the former WinForms splash screen that does not exist on the headless DS. |
| [`Shared/Preloader.cs`](../descriptions/Shared/Preloader.cs.md) | 225 | Implements Magnetar's "preloader plugin" mechanism: BepInEx/Pulsar-style assembly patching of SE DS DLLs *on disk* before they are loaded into the CLR. |
| [`Shared/Steam.cs`](../descriptions/Shared/Steam.cs.md) | 81 | Thin Steam helper for the Dedicated Server: resolves the Steam install path cross-platform, redirects `Steamworks.NET` assembly resolution to a bundled copy, and checks Workshop item install state through the *game-server* UGC API. |
| [`Shared/Tools.cs`](../descriptions/Shared/Tools.cs.md) | 196 | Grab-bag of cross-cutting utilities used throughout Magnetar: SHA-256 hashing of files/strings/folders (used for cache invalidation), human-friendly "time ago" formatting, console/error message reporting, file globbing, filename sanitizing, JSON-based deep copy, interactive-terminal detection, and a cross-platform native crash handler. |
| [`Shared/Updater.cs`](../descriptions/Shared/Updater.cs.md) | 209 | Handles Magnetar's self-update against a GitHub release repo. |

## Public API surface

- `new Loader(votesServer, forceEnable) -> populated Loader.Plugins list of (PluginData, Assembly)`
- `new PluginList(mainDirectory, sources, profiles); UpdateRemoteList/UpdateLocalList; GetModPlugins; TryGetPlugin; enumeration`
- `new Preloader(assemblies).Patch(gameDir, cacheDir); PreHooks()/PostHooks(); HasPatches`
- `new Updater(repoName).TryUpdate(); Updater.GameUpdatePrompt(...)`
- `AssemblyResolver.AddSourceFolder/AddAllowedAssemblyName/AddAllowedAssemblyFile + AssemblyResolved event`
- `Tools.Init(external, compiler); Tools.GetFolderHash/GetFileHash/ShowMessage/InstallNativeCrashHandler; Tools.IsInteractiveTerminal()`
- `Steam.GetSteamPath(); Steam.IsItemInstalled(id); Steam.SteamworksResolver(baseDir)`
- `LogFile.Init(mainPath)/WriteLine/Error/Warn/Dispose`
- `Flags.* flags (incl. NoImplicitMod/Consent/Help), Flags.LogFlags(), Flags.PrintHelp()`
- `new Launcher(sePath).CanStart()/VerifyConfig()`
- `PluginProgress.ReportDownloading/ReportCompiling/ReportCompiled/ReportSummary`

## Dependencies

**Uses modules:** [Compiler](Compiler.md), [Shared.Config](Shared.Config.md), [Shared.Data](Shared.Data.md), [Shared.Network](Shared.Network.md), [Shared.Votes](Shared.Votes.md)  
**Used by modules:** [Legacy.Integration](Legacy.Integration.md), [Legacy.Launcher](Legacy.Launcher.md), [Legacy.Loader](Legacy.Loader.md), [Legacy.Patch](Legacy.Patch.md), [Shared.Config](Shared.Config.md), [Shared.Data](Shared.Data.md), [Shared.Network](Shared.Network.md), [Shared.Votes](Shared.Votes.md)  
**External systems:** GitHub; Harmony; NuGet; SE DS assemblies; Steam

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
