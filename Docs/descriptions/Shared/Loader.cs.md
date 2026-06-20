# Shared/Loader.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared` · **Kind:** class · **Lines:** 156

## Summary
The orchestrator that instantiates all enabled plugins at startup. It pulls the active profile from `ConfigManager`, force-loads core/dependency plugins, compiles/loads every enabled plugin's assembly via `PluginData.TryLoadAssembly`, validates the running Harmony version against the expected one, wires up the stats client, and reports plugin usage to the statistics server (subject to consent). The resulting `(PluginData, Assembly)` pairs in `Plugins` are what the environment-specific loader then instantiates.

## Types
### `Loader` — class, public
Built once during bootstrap; its constructor performs the entire load pass and stores results. Exposes a static `Instance` and the loaded plugin list.
- **Fields:** `Instance` — static singleton reference (assigned by callers); `Plugins` — readonly list of `(PluginData, Assembly)` for every successfully loaded plugin; `config` — `CoreConfig` from `ConfigManager`; `profiles` — `ProfilesConfig` from `ConfigManager`; `corePluginIds` — the `forceEnable` ids passed to the constructor (the built-in compatibility plugins), retained so usage reporting can include them.
- **Methods:** `Loader(string votesServer, string[] forceEnable=null)` — constructor doing the work: stores `forceEnable` as `corePluginIds`, initializes `GitHub`, logs enabled plugins, sets `VotesClient.BaseUrl` (config override or passed default), updates player stats, warns on Harmony version mismatch (`ConfigManager.HarmonyVersion` vs the loaded `HarmonyLib` assembly), force-loads each `forceEnable` id (exiting with code 1 and a message if a core plugin fails), then loads every enabled plugin — tracking `IsLocal`, accumulating a debug report of compile failures when `Flags.CheckAllPlugins`, skipping `ModPlugin`s there — and finally reports a summary via `PluginProgress` and kicks off `ReportEnabledPlugins` on a background task. `ReportEnabledPlugins()` — returns immediately unless `ConsentManager.Granted`; when `ConsentManager.PendingServerConsent` is set it first (re-)registers the grant via `VotesClient.Consent(true)` (idempotent), then sends the enabled plugin ids — the profile's non-local ids *unioned* with the core/compatibility plugin ids (`corePluginIds`) — to `VotesClient.Track`, logging success/failure. `GetEnabledPlugins()` — yields plugins in the current profile, plus (when `CheckAllPlugins`) all non-local compiled plugins. `LogEnabledPlugins()` — logs the comma-separated list of enabled plugin ids (or "None").

## Cross-references
- **Uses:** `Shared/PluginProgress.cs`, `Shared/Flags.cs`, `Shared/LogFile.cs`, `Shared/Tools.cs`; Shared.Config (`ConfigManager`, `CoreConfig`, `ProfilesConfig`), Shared.Data (`PluginData`, `ModPlugin`), Shared.Network (`GitHub`), Shared.Votes (`VotesClient`, `ConsentManager`); Harmony (version check).
- **Used by:** _none within the repository_
