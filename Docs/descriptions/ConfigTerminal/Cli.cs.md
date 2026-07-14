# ConfigTerminal/Cli.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal` · **Kind:** sealed class · **Lines:** 91

## Summary
Parses the MagnetarConfig command line into a strongly-typed options object and converts it into an `InstanceBinding` with defaults filled in. Recognises `-path`, `-config`, `-magnetar`, `-ds64`, `-netdriver`, `-diag`, and `-help`/`-h`/`--help`; any other token stops parsing with an "Unknown argument" error. Also owns the `-help` text (per `Docs/ConfigTerminal.md §10`).

## Types
### Cli — sealed class, internal
Immutable-ish parse result holding the raw option values plus a parse `Error`.

- **Fields:**
  - `DataDir` (`-path`), `ConfigDir` (`-config`), `MagnetarExe` (`-magnetar`), `Ds64Dir` (`-ds64`) — string paths, null when unspecified.
  - `NetDriver` (`-netdriver`), `Diag` (`-diag`, headless read-only report), `Help` (`-help`/`-h`/`--help`) — bool flags.
  - `Error` — non-null message when parsing failed.
- **Properties:**
  - `HasInstance` — true when `DataDir` or `ConfigDir` was given, so the instance picker can be skipped.
- **Methods:**
  - `Parse(string[] args)` (static) — case-insensitive switch over args; value-taking options consume the next token via `Next`; unknown tokens set `Error` and stop. After the loop, validates that any explicitly given `-path`/`-config` directory exists (`Directory.Exists`), setting `Error` if not — never silently falling back past a user-supplied value (matching Magnetar's own refusal).
  - `Next(string[] args, ref int i)` (private static) — returns the following argument (advancing `i`), or null when the option is the last token.
  - `ToBinding()` — builds an `InstanceBinding` from the four path fields and runs it through `InstanceLocator.ResolveDefaults` to fill in defaults.
  - `PrintHelp()` (static) — writes the usage/options block to `Console.Out`, documenting `-path`, `-config`, `-magnetar`, `-ds64`, `-netdriver`, and `-help`/`-h`, and noting that with no `-path`/`-config` the tool opens an interactive instance picker.

## Cross-references
- **Uses:** `InstanceBinding`/`InstanceLocator` (`ConfigTerminal/Model/`), `System.IO.Directory`, `System.Console`.
- **Used by:** [Program.cs](Program.cs.md)
