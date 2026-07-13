# ConfigTerminal/Diagnostics.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal` · **Kind:** static class · **Lines:** 106

## Summary
Produces the headless, read-only `-diag` report of an instance's resolved state without starting Terminal.Gui, exercising the same model/process layers the UI uses. Intended for ops scripts, CI smoke tests, and confirming that an instance resolves and reads correctly. Everything is written to `Console.Out` (plugin/profile reads are wrapped so a read failure prints a note instead of aborting) and the method always returns 0.

## Types
### Diagnostics — static class, internal
Formats and prints an instance snapshot.

- **Methods:**
  - `Run(InstanceBinding binding)` — prints the resolved binding (data dir, config dir, launcher, `Ds64Dir` or `(not found)`), then `DsInstance.Open(binding)` and dumps config presence plus selected options (`ServerName`, `ServerPort`, `NetworkType` via `Val`, and `IgnoreLastSession`/`LoadWorld`/`PremadeCheckpointPath`/`HasPassword`). Lists worlds (active marker, session name, mod count, world-config presence, last save time), the active world, and templates. Inside a try/catch it reads plugins via `MagnetarPlugins` — local DLLs, registered dev-folder plugins (flagging `!missing`), hub catalog plugins, and remote/local source counts — and profiles via `ProfileCatalog` (saved named profiles and which one matches the active set); a failure prints `Plugins: could not read (<message>)`. Finally queries live server status via `MagnetarProcess.Query()` and, if `instance.Problems.Any`, lists the problem messages. Returns 0.
  - `Val(DsInstance instance, string id)` (private) — resolves an `OptionDefinition` by id via `OptionRegistry.ById` and returns `instance.Config.Get(def)`, or `""` when the id is unknown.

## Cross-references
- **Uses:** `InstanceBinding`/`DsInstance`/`WorldInfo`/`WorldTemplate`/`OptionDefinition`/`OptionRegistry`/`MagnetarPlugins`/`LocalDllInfo`/`DevFolderPlugin`/`HubPluginView`/`ProfileCatalog`/`ProfileInfo` (`ConfigTerminal/Model/`), `MagnetarProcess`/`ServerStatus` (`ConfigTerminal/Process/`), `AtomicFile` (`ConfigTerminal/Io/`), `System.Console`, `System.Linq`.
- **Used by:** [Program.cs](Program.cs.md)
