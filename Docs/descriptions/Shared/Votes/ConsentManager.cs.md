# Shared/Votes/ConsentManager.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Votes` · **Kind:** static class · **Lines:** 180

## Summary
Owns the telemetry-consent state machine: it decides, once per startup, whether anonymous plugin-usage statistics may be sent, and exposes the result to the rest of the loader through static properties. Consent is anchored to a locally-generated `instance.id` file (a random UUID) rather than to a Steam ID — that file's presence *is* the grant, and the first 20 hex characters of the UUID become the anonymous `PlayerHash` sent to the stats server. `Resolve()` reconciles the stored state with the `-consent` / `-noconsent` command-line flags and, when nothing has been decided and a real terminal is attached, interactively prompts the operator. `Withdraw()` implements the one-shot `-withdraw-consent` maintenance action. The decision is mirrored into `config.xml` (`DataHandlingConsent`) so it is human-visible and survives an interrupted startup.

## Types
### `ConsentManager` — static class, public
The single authority on whether telemetry is permitted. Other modules read its static state (`Granted`, `PlayerHash`) rather than re-deriving consent themselves; [`Loader`](../Loader.cs.md) and [`VotesClient`](VotesClient.cs.md) gate all outbound stats traffic on it.

- **Properties (read-only):**
  - `Granted` — `true` once consent is confirmed active for this run; gates all tracking/voting.
  - `PendingServerConsent` — `true` when the server should be (re-)notified of the grant on this run; set both on a fresh accept and on every start where an existing `instance.id` is present (idempotent re-register).
  - `PlayerHash` — the anonymous identifier derived from `instance.id`, consumed by `VotesClient` for `/Track`, `/Vote` and authenticated `/Stats` calls. `null` when consent is not granted.

- **Methods:**
  - `Withdraw(string votesServer)` — backs the `-withdraw-consent` flag. If an `instance.id` exists, points `VotesClient.BaseUrl` at the configured (or passed) stats server and calls `VotesClient.Consent(false, hash)` to erase the server-side record, then deletes `instance.id`; if none exists, records the denial only. Always writes `DataHandlingConsent = false` and a fresh `DataHandlingConsentDate` to `config.xml`. Best effort: an unreachable server still leaves telemetry disabled locally. Both branches print a user-facing line to `Console`. `Program.MagnetarMain` calls this and exits without starting the server.
  - `Resolve()` — the normal-startup decision. First *reconciles* stale state: a stored grant (`DataHandlingConsent == true`) with no `instance.id` is treated as undecided and cleared (a denial, `false`, is kept so the user is not re-prompted). Then: `-noconsent` suppresses telemetry for this run (leaving any `instance.id` intact); `-consent` grants via `Accept`. With no flag, an existing `instance.id` means consent is active (`Granted`/`PendingServerConsent` set, `PlayerHash` derived); a stored `false` means previously declined (silent); otherwise the state is undecided — it prompts interactively only when [`Tools.IsInteractiveTerminal()`](../Tools.cs.md) is true, else logs a warning and leaves telemetry off.
  - `Accept(ConfigManager, CoreConfig, string source)` (private) — records a grant: `ConfigManager.CreateInstanceId()` (the UUID is the server identity), derives `PlayerHash`, sets `Granted`/`PendingServerConsent`, writes `DataHandlingConsent = true` + date to `config.xml`. `source` labels the log line (flag vs interactive prompt).
  - `Deny(CoreConfig, string source)` (private) — records a denial in `config.xml` immediately so the operator is not re-prompted; does not create an `instance.id`.
  - `DerivePlayerHash(string instanceId)` (private) — strips dashes, lowercases, and takes the first 20 chars of the UUID, satisfying the server's `^[a-z0-9]{20}$` validation.

### Interactive prompt
When undecided and a TTY is present, `Resolve()` prints what is collected (the list of enabled plugin IDs tied to a random anonymous instance ID — no personal data, account/Steam ID, IP, or world content) and loops on `Console.ReadLine()` until `Y` (→ `Accept`) or `N` (→ `Deny`). An `IOException` or EOF on stdin (e.g. an IDE run console with no keyboard) is treated as "no usable input": it warns, leaves the choice undecided, and disables telemetry for the run rather than spinning.

## Cross-references
- **Uses:** [ConfigManager.cs](../Config/ConfigManager.cs.md) (`HasInstanceId`/`ReadInstanceId`/`CreateInstanceId`/`DeleteInstanceId`), [CoreConfig.cs](../Config/CoreConfig.cs.md) (`DataHandlingConsent`, `DataHandlingConsentDate`, `VotesServerBaseUrl`, `Save`), [VotesClient.cs](VotesClient.cs.md) (`Consent`, `BaseUrl`), [Flags.cs](../Flags.cs.md) (`Consent` / `ConsentChoice`), [Tools.cs](../Tools.cs.md) (`IsInteractiveTerminal`), [LogFile.cs](../LogFile.cs.md); `System.Console`.
- **Used by:** [Program.cs](../../Legacy/Program.cs.md), [Loader.cs](../Loader.cs.md), [VotesClient.cs](VotesClient.cs.md)
