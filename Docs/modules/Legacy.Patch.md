# Module: Legacy.Patch

**Project:** `Legacy` · **Files:** 12 · **Source lines:** 528

## Purpose

Provides all Harmony patches that adapt the Space Engineers Dedicated Server binary to Magnetar's in-process, headless, externally-configured hosting model. The patches cover: stripping the WinForms/Telerik configuration UI and Windows Service branch from DedicatedServer.Run; rerouting crash reporting, process exit, and chat commands; injecting the MagnetarMod client companion and client-mod definitions/scripts into SE's loading and workshop-download pipelines; enforcing a trusted-mods security policy; and capturing Roslyn compilation diagnostics with clean, cross-platform file paths.

## Role in Magnetar

Acts as the glue layer between the unmodified SE DS assemblies and the rest of Magnetar. Every patch in this module is registered via HarmonyLib under one of two categories: "Early" (applied at loader startup before the server initialises) or "Late" (applied from PluginLoader.Init once plugins are ready). Together the patches intercept the exact SE entry points needed by Legacy.Loader, Legacy.Launcher, Legacy.Commands, and the client-mod support in Shared.Data/Shared.Config, without requiring source changes to the SE binaries.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `Patch_Compile` | static class | [`Legacy/Patch/Patch_Compile.cs`](../descriptions/Legacy/Patch/Patch_Compile.cs.md) | Harmony Postfix on MyScriptCompiler.AnalyzeDiagnostics; collects structured Roslyn diagnostics with clean workshop-relative file paths when PulsarLog is active |
| `Patch_ComponentRegistered` | static class | [`Legacy/Patch/Patch_ComponentRegistered.cs`](../descriptions/Legacy/Patch/Patch_ComponentRegistered.cs.md) | Harmony Prefix on MySession.RegisterComponentsFromAssembly; triggers PluginLoader.RegisterSessionComponents when the game assembly is being registered |
| `Patch_DedicatedServerRun` | static class | [`Legacy/Patch/Patch_DedicatedServerRun.cs`](../descriptions/Legacy/Patch/Patch_DedicatedServerRun.cs.md) | Harmony Transpiler on DedicatedServer.Run; replaces the entire method body with a minimal headless startup that skips WinForms UI and Windows Service branches |
| `Patch_ExitThreadSafe` | class | [`Legacy/Patch/Patch_ExitThreadSafe.cs`](../descriptions/Legacy/Patch/Patch_ExitThreadSafe.cs.md) | Harmony Prefix on MySandboxGame.ExitThreadSafe; redirects all in-game exit requests to ServerControl.SaveAndQuit to avoid hangs in in-process hosting |
| `Patch_LoadScripts` | static class | [`Legacy/Patch/Patch_LoadScripts.cs`](../descriptions/Legacy/Patch/Patch_LoadScripts.cs.md) | Harmony Postfix on MyScriptManager.LoadScripts; triggers PluginLoader.RegisterEntityComponents on the base-game script load pass |
| `Patch_MyDefinitionErrors` | static class | [`Legacy/Patch/Patch_MyDefinitionErrors.cs`](../descriptions/Legacy/Patch/Patch_MyDefinitionErrors.cs.md) | Harmony Prefix on MyDefinitionErrors.Add; intercepts compilation-failure messages and re-logs them via Magnetar's logger using diagnostics from Patch_Compile |
| `Patch_MyDefinitionManager` | static class | [`Legacy/Patch/Patch_MyDefinitionManager.cs`](../descriptions/Legacy/Patch/Patch_MyDefinitionManager.cs.md) | Harmony Prefix on MyDefinitionManager.LoadData; injects the MagnetarMod client companion via MagnetarClientMod.ApplyToModList, then appends ModPlugin workshop mod-items from the active Magnetar profile into SE's definition-loading mod list |
| `Patch_MyScriptManager` | static class | [`Legacy/Patch/Patch_MyScriptManager.cs`](../descriptions/Legacy/Patch/Patch_MyScriptManager.cs.md) | Harmony Postfix on MyScriptManager.LoadData; compiles and loads scripts for client ModPlugins with the PULSAR conditional symbol injected into the Roslyn compiler |
| `Patch_MySessionLoader` | class | [`Legacy/Patch/Patch_MySessionLoader.cs`](../descriptions/Legacy/Patch/Patch_MySessionLoader.cs.md) | Two Harmony Prefixes on MySessionLoader multiplayer-load methods; apply the MagnetarMod client companion to the world checkpoint via MagnetarClientMod.ApplyToCheckpoint, then strip untrusted (non-locally-installed Steam) mods when the -hardened flag is set |
| `Patch_MyWorkshop` | static class | [`Legacy/Patch/Patch_MyWorkshop.cs`](../descriptions/Legacy/Patch/Patch_MyWorkshop.cs.md) | Harmony patch (Early) on MyWorkshop.DownloadWorldModsBlocking; injects/strips the MagnetarMod client companion before download and asks SteamMods to expand legacy `*_legacy.bin` archives afterward. |
| `Patch_PrepareCrashReport` | static class | [`Legacy/Patch/Patch_PrepareCrashReport.cs`](../descriptions/Legacy/Patch/Patch_PrepareCrashReport.cs.md) | Harmony Prefix on MyCrashReporting.PrepareCrashAnalyticsReporting; launches the crash reporter against the configured SpaceEngineers.exe path instead of the default |
| `Patch_ServerChat` | static class | [`Legacy/Patch/Patch_ServerChat.cs`](../descriptions/Legacy/Patch/Patch_ServerChat.cs.md) | Harmony Prefix on MyMultiplayerBase.OnChatMessageReceived_Server; routes !-prefixed player-typed chat (Global/Faction/Private) through CommandService and suppresses broadcast when handled, while leaving scripted/system channels untouched |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Legacy/Patch/Patch_Compile.cs`](../descriptions/Legacy/Patch/Patch_Compile.cs.md) | 65 | Postfix-patches `MyScriptCompiler.AnalyzeDiagnostics` to intercept Roslyn compilation failures before they reach SE's own error pipeline. |
| [`Legacy/Patch/Patch_ComponentRegistered.cs`](../descriptions/Legacy/Patch/Patch_ComponentRegistered.cs.md) | 20 | Prefix-patches `MySession.RegisterComponentsFromAssembly` to inject plugin-provided session components at exactly the right moment in the SE session lifecycle. |
| [`Legacy/Patch/Patch_DedicatedServerRun.cs`](../descriptions/Legacy/Patch/Patch_DedicatedServerRun.cs.md) | 78 | Transpiler-patches `VRage.Dedicated.DedicatedServer.Run` to remove the Telerik/WinForms configuration UI and the Windows Service branch, replacing the entire method body with a minimal headless startup sequence. |
| [`Legacy/Patch/Patch_ExitThreadSafe.cs`](../descriptions/Legacy/Patch/Patch_ExitThreadSafe.cs.md) | 20 | Prefix-patches `MySandboxGame.ExitThreadSafe` to redirect in-game and admin-triggered exit requests through Magnetar's graceful shutdown path. |
| [`Legacy/Patch/Patch_LoadScripts.cs`](../descriptions/Legacy/Patch/Patch_LoadScripts.cs.md) | 17 | Postfix-patches `MyScriptManager.LoadScripts` to trigger plugin entity-component registration at the correct point in session startup. |
| [`Legacy/Patch/Patch_MyDefinitionErrors.cs`](../descriptions/Legacy/Patch/Patch_MyDefinitionErrors.cs.md) | 40 | Prefix-patches `MyDefinitionErrors.Add` to intercept Roslyn compilation-failure error messages and redirect them to Magnetar's own log, replacing SE's raw, path-cluttered error string with a cleaner structured output that pairs the mod name with the per-diagnostic messages already collected by `Patch_Compile`. |
| [`Legacy/Patch/Patch_MyDefinitionManager.cs`](../descriptions/Legacy/Patch/Patch_MyDefinitionManager.cs.md) | 45 | Prefix-patches `MyDefinitionManager.LoadData` to augment SE's mod list before definitions are loaded. |
| [`Legacy/Patch/Patch_MyScriptManager.cs`](../descriptions/Legacy/Patch/Patch_MyScriptManager.cs.md) | 78 | Postfix-patches `MyScriptManager.LoadData` to compile and load scripts for client-side `ModPlugin` entries after SE has processed all normal session mods. |
| [`Legacy/Patch/Patch_MySessionLoader.cs`](../descriptions/Legacy/Patch/Patch_MySessionLoader.cs.md) | 38 | Contains two Harmony Prefix patches on `MySessionLoader.LoadMultiplayerScenarioWorld` and `MySessionLoader.LoadMultiplayerSession`. |
| [`Legacy/Patch/Patch_MyWorkshop.cs`](../descriptions/Legacy/Patch/Patch_MyWorkshop.cs.md) | 27 | `Patch_MyWorkshop` intercepts SE's `MyWorkshop.DownloadWorldModsBlocking` path. |
| [`Legacy/Patch/Patch_PrepareCrashReport.cs`](../descriptions/Legacy/Patch/Patch_PrepareCrashReport.cs.md) | 44 | Prefix-patches `VRage.Platform.Windows.MyCrashReporting.PrepareCrashAnalyticsReporting` to redirect the SE crash reporter to run the correct `SpaceEngineers.exe` binary, which in Magnetar's in-process hosting model is not necessarily the process that crashed. |
| [`Legacy/Patch/Patch_ServerChat.cs`](../descriptions/Legacy/Patch/Patch_ServerChat.cs.md) | 56 | Prefix-patches `MyMultiplayerBase.OnChatMessageReceived_Server` to intercept player-typed chat before SE relays it. |

## Public API surface

- `Patch_Compile.PulsarLog — toggled by Patch_MyDefinitionErrors.RedirectModLogging to enable diagnostic capture`
- `Patch_Compile.Diagnostics — HashSet<string> of Roslyn diagnostic strings read by Patch_MyDefinitionErrors`
- `Patch_MyDefinitionErrors.RedirectModLogging(bool) — coordinates logging state across Patch_MyDefinitionErrors and Patch_Compile`
- `Patch_PrepareCrashReport.SpaceEngineersPath — must be set by the launcher before any crash occurs`

## Dependencies

**Uses modules:** [Legacy.Commands](Legacy.Commands.md), [Legacy.Loader](Legacy.Loader.md), [Shared.Config](Shared.Config.md), [Shared.Core](Shared.Core.md), [Shared.Data](Shared.Data.md)  
**Used by modules:** [Legacy.Launcher](Legacy.Launcher.md)  
**External systems:** Harmony; SE DS assemblies

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
