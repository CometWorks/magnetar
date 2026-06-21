# Legacy/Loader/PluginLoader.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Loader` · **Kind:** class · **Lines:** 217

## Summary
The top-level plugin host: a singleton `IHandleInputPlugin` that SE itself drives (`Init`/`Update`/`HandleInput`/`Dispose`). It instantiates every compiled plugin obtained from the shared loader, applies late Harmony patches, stands up the chat-command pipeline (`CommandService`) with Magnetar's built-in `!save`/`!restart`/`!quit`/`!stop` commands, binds the SDK path resolver, initializes each plugin, and finally downloads the Steam workshop mods referenced by the active profile. It also subscribes to `AppDomain.FirstChanceException` to attribute member-access errors back to the offending plugin so that one bad plugin can be quarantined rather than crashing the server. Failing plugins are pruned from the list at each lifecycle stage.

## Types

### PluginLoader — class, public : IHandleInputPlugin
Server-side singleton plugin manager. Exists as the single SE-visible plugin that fans the SE lifecycle out to all loaded Magnetar plugins and owns shared host services (commands, exception attribution).

- **Fields:**
  - `Instance` (static `PluginLoader`) — the singleton, set in the ctor and cleared in `Dispose`.
  - `init` (`bool`) — gate ensuring lifecycle calls are ignored until `Init` completes.
  - `plugins` (`List<PluginInstance>`, readonly) — the live plugins.
- **Properties:**
  - `Plugins` (`List<PluginInstance>`) — exposes the plugin list.
  - `Commands` (`CommandService`, private set) — chat-command pipeline, built in `Init` and installed as `ServerCommands.Registrar`; null until plugins are initialized.
- **Methods:**
  - `PluginLoader()` — Sets `Instance` and subscribes `OnException` to `AppDomain.CurrentDomain.FirstChanceException`.
  - `TryGetPluginInstance(string id, out PluginInstance)` — Linear lookup by plugin `Id`; returns false before init.
  - `RegisterSessionComponents()` — Calls each plugin's `RegisterSessionComponents(MySession.Static)`.
  - `RegisterEntityComponents()` — Calls each plugin's `RegisterEntityComponents(MyScriptManager.Static)`.
  - `Init(object gameInstance)` — Applies the `"Late"` Harmony patch category for the executing assembly. In `SafeMode` clears all plugins. Otherwise: instantiates plugins, creates `Commands` and assigns `ServerCommands.Registrar`, registers built-in `SaveCommand`/`RestartCommand`/`QuitCommand`/`StopCommand` (last-registration-wins so plugins can override), binds `PathResolverBinder` (reflection-only, triggers no LinuxCompat cctor), then iterates plugins in reverse calling `Init`, removing (fast) any that fail. With `Flags.CheckAllPlugins` it accumulates a failure report and reopens the log. Sets `init = true`. Finally collects the active profile's mod-plugin workshop IDs and calls `SteamMods.Update`.
  - `Update()` — Reverse-iterates plugins calling `Update`, fast-removing failures; no-op before init.
  - `HandleInput()` — Reverse-iterates plugins calling `HandleInput`, fast-removing failures; no-op before init.
  - `Dispose()` — Disposes and clears all plugins, clears `ServerCommands.Registrar`/`Commands`, disposes `LogFile`, nulls `Instance`.
  - `OnException(object, FirstChanceExceptionEventArgs)` (private) — Extracts a `MemberAccessException` (direct or inner) and asks each plugin whether the exception site belongs to it (`ContainsExceptionSite`). Wrapped in a swallow-all try/catch — must never throw inside a first-chance handler.
  - `InstantiatePlugins()` (private) — For each `(data, assembly)` from `Pulsar.Shared.Loader.Instance.Plugins`, builds a `PluginInstance` via `PluginInstance.TryGet`, then reverse-iterates calling `Instantiate`, fast-removing failures.

## Cross-references
- **Uses:** `Pulsar.Shared` (`LogFile`, `Loader`, `PluginList`), `Pulsar.Shared.Config` (`ConfigManager`, `Profile`, `Flags`, `SafeMode`), `Pulsar.Shared.Data`; `PluginSdk.Commands` (`CommandService`, `ServerCommands`); `Pulsar.Legacy.Commands` (`SaveCommand`, `RestartCommand`, `QuitCommand`, `StopCommand`); `Pulsar.Legacy.Paths` (`PathResolverBinder`); `Pulsar.Legacy.Loader` (`PluginInstance`, `SteamMods`); SE DS `Sandbox.Game.World` (`MySession`, `MyScriptManager`), `VRage.Plugins` (`IHandleInputPlugin`); `HarmonyLib` (`Harmony`, `PatchCategory`).
- **Used by:** [ServerControl.cs](../Launcher/ServerControl.cs.md), [Patch_ComponentRegistered.cs](../Patch/Patch_ComponentRegistered.cs.md), [Patch_LoadScripts.cs](../Patch/Patch_LoadScripts.cs.md), [Patch_ServerChat.cs](../Patch/Patch_ServerChat.cs.md), [Program.cs](../Program.cs.md)
