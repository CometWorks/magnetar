# Shared/Flags.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared` · **Kind:** static class (+ enum) · **Lines:** 83

## Summary
Parses Magnetar's own command-line switches once at startup (in a static constructor) and exposes them as read-only boolean/enum flags for the rest of the loader. These are dash-prefixed arguments (e.g. `-noupdate`, `-debug`, `-sources`) layered on top of the SE DS's normal arguments, controlling update behavior, debug tooling, plugin compilation, and mod trust hardening.

## Types
### `UpdateType` — enum, public
Selects the self-update channel: `None` (updates disabled), `Standard` (stable releases), `Tester` (pre-release/early updates).

### `Flags` — static class, public
Reads `Environment.GetCommandLineArgs()` once and snapshots each recognized switch into a static property. Also logs which non-default flags are active.
- **Properties:** `UpdateType` — `None` if `-noupdate`, `Tester` if `-prerelease`, else `Standard`; `ExternalDebug` — `-debug`; `DebugMenu` — `-f12menu`; `CustomSources` — `-sources`; `ContinueGame` — `-continue`; `CheckAllPlugins` — `-debugCompileAll` (compile every listed plugin to surface build failures); `GameIntroVideo` — `-keepintro`; `MakeCheckFile` — `-mkcheck`; `TrustedMods` — `-hardened`; `Daemon` — `-daemon` (detach from the parent process at startup; see [Daemon.cs](../Legacy/Launcher/Daemon.cs.md)). All are `{ get; private set; }`.
- **Methods:** `LogFlags()` — builds the list of enabled non-default flags and writes a single `Enabled flags: ...` line via `LogFile` (nothing if none changed); `HasArg(string)` — case-insensitive check for `-<argument>` in the process command line.

## Cross-references
- **Uses:** `Shared/LogFile.cs` (flag logging); `Environment.GetCommandLineArgs`.
- **Used by:** [PluginData.cs](Data/PluginData.cs.md), [PluginLoader.cs](../Legacy/Loader/PluginLoader.cs.md), [Interim.cs](../Legacy/Compiler/Interim.cs.md), [Program.cs](../Legacy/Program.cs.md), [Patch_MySessionLoader.cs](../Legacy/Patch/Patch_MySessionLoader.cs.md), [Updater.cs](Updater.cs.md), [Loader.cs](Loader.cs.md), [Game.cs](../Legacy/Launcher/Game.cs.md)
