# Shared/Flags.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared` · **Kind:** static class (+ enums) · **Lines:** 154

## Summary
Parses Magnetar's own command-line switches once at startup (in a static constructor) and exposes them as read-only boolean/enum flags for the rest of the loader. These are dash-prefixed arguments (e.g. `-noupdate`, `-debug`, `-sources`) layered on top of the SE DS's normal arguments, controlling update behavior, debug tooling, plugin compilation, mod-trust hardening, implicit companion-mod loading, daemon detach, telemetry consent, and the `-help` screen. It also renders the human-readable usage text printed by `-help`.

## Types
### `UpdateType` — enum, public
Selects the self-update channel: `None` (updates disabled), `Standard` (stable releases), `Tester` (pre-release/early updates).

### `ConsentChoice` — enum, public
The telemetry-consent intent expressed on the command line: `Unset` (no consent flag given), `Accept` (`-consent`), `Deny` (`-noconsent`, this run only), `Withdraw` (`-withdraw-consent`, erase server data and exit). Consumed by [`ConsentManager`](Votes/ConsentManager.cs.md).

### `Flags` — static class, public
Reads `Environment.GetCommandLineArgs()` once and snapshots each recognized switch into a static property. Also logs which non-default flags are active and prints the usage screen.
- **Properties:** `UpdateType` — `None` if `-noupdate`, `Tester` if `-prerelease`, else `Standard`; `ExternalDebug` — `-debug`; `DebugMenu` — `-f12menu`; `CustomSources` — `-sources`; `ContinueGame` — `-continue`; `CheckAllPlugins` — `-debugCompileAll` (compile every listed plugin to surface build failures); `GameIntroVideo` — `-keepintro`; `MakeCheckFile` — `-mkcheck`; `TrustedMods` — `-hardened`; `Daemon` — `-daemon` (detach from the parent process at startup; see [Daemon.cs](../Legacy/Launcher/Daemon.cs.md)); `NoImplicitMod` — `-noimplicitmod` (suppress auto-loading the MagnetarMod client companion mod); `Consent` — a `ConsentChoice` resolved from `-withdraw-consent` / `-consent` / `-noconsent` (in that precedence), default `Unset`; `Help` — `true` for `-h`, `-help`, or `--help`. All are `{ get; private set; }`.
- **Methods:**
  - `LogFlags()` — builds the list of enabled non-default flags (including `NoImplicitMod` and the `Consent` choice when not `Unset`) and writes a single `Enabled flags: ...` line via `LogFile` (nothing if none changed).
  - `PrintHelp()` — writes the full usage screen to `Console`: a version line (from the entry assembly), Magnetar's own options, the telemetry-consent options (`-consent` / `-noconsent` / `-withdraw-consent`), the pass-through dedicated-server options, and the help switches. Invoked by [`Program`](../Legacy/Program.cs.md) when `Help` is set, after which the launcher exits without starting the server.
  - `HasArg(string)` — case-insensitive check for `-<argument>` in the process command line. Note `--help` is matched by passing the literal `-help` argument (the second leading dash is part of the matched token).

## Cross-references
- **Uses:** `Shared/LogFile.cs` (flag logging); `Environment.GetCommandLineArgs`; `System.Reflection` (entry-assembly version for the help screen); `System.Console`.
- **Used by:** [Interim.cs](../Legacy/Compiler/Interim.cs.md), [Game.cs](../Legacy/Launcher/Game.cs.md), [MagnetarClientMod.cs](../Legacy/Loader/MagnetarClientMod.cs.md), [PluginLoader.cs](../Legacy/Loader/PluginLoader.cs.md), [Patch_MySessionLoader.cs](../Legacy/Patch/Patch_MySessionLoader.cs.md), [Program.cs](../Legacy/Program.cs.md), [PluginData.cs](Data/PluginData.cs.md), [Loader.cs](Loader.cs.md), [Tools.cs](Tools.cs.md), [Updater.cs](Updater.cs.md), [ConsentManager.cs](Votes/ConsentManager.cs.md)
