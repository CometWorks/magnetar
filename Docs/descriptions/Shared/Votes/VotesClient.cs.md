# Shared/Votes/VotesClient.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Votes` · **Kind:** static class · **Lines:** 94

## Summary
The single outbound client for Magnetar's statistics back-end, providing four REST operations: consent management, votes download, session tracking, and voting. It holds module-level state for the anonymized player hash (derived once from `CoreConfig.InstallId`) and the most recently received voting token. All HTTP I/O is delegated to `SimpleHttpClient`; JSON serialization is handled transparently within that layer.

## Types
### `VotesClient` — static class, public
Owns all communication with the remote stats service. The four public methods map one-to-one onto the four server endpoints. Because `DownloadVotes` may be called from a background thread (as `ConfigManager.UpdatePlayerVotes` wraps it in `Task.Run`), the class is written to be safe for concurrent calls that do not interleave (the token assignment is last-write-wins which is sufficient given the single background task pattern).

- **Properties:**
  - `BaseUrl` — settable base URL for the stats API, set at startup by the host layer to point at the live server (or a test instance).

- **Fields (private):**
  - `playerHash` — backing field for the lazily-initialized `PlayerHash` computed property; `null` until first access.
  - `votingToken` — the latest `VotingToken` received from `/Stats`; cleared to `null` when votes are fetched anonymously (no consent); required by `Vote`.

- **Computed properties (private):**
  - `PlayerHash` — lazy property that calls `ConfigManager.Instance.GetOrCreateInstallId()`, hashes it with `Tools.GetStringHash` (SHA-1 hex), and takes the first 20 characters (80 bits). The value is computed once and cached in `playerHash`.
  - `ConsentUri`, `VotesUri`, `TrackUri`, `VoteUri` — string properties that prepend `BaseUrl` to the respective path segments; recomputed on each access so a `BaseUrl` change is immediately reflected.

- **Methods:**
  - `Consent(bool consent) → bool` — builds a `ConsentRequest` with the current `PlayerHash` and POSTs it to `/Consent`; logs the action before sending; returns `true` on HTTP 200.
  - `DownloadVotes() → PluginVotes` — fetches `/Stats`; if `DataHandlingConsent` is `false` the request is made anonymously (no query parameters, `votingToken` set to `null`); if consent is active, `PlayerHash` is passed as a query parameter and the returned `VotingToken` is cached for use by `Vote`. Safe to call from a non-UI thread.
  - `Track(string[] pluginIds) → bool` — POSTs a `TrackRequest` to `/Track` recording which plugins were active at startup; returns `true` on success.
  - `Vote(string pluginId, int vote) → PluginVote` — guards against a missing `votingToken` (logs an error and returns `null` if absent); otherwise POSTs a `VoteRequest` to `/Vote` and deserializes the server's updated `PluginVote` response, giving the caller the fresh record without a separate download.

## Cross-references
- **Uses:**
  - `Shared/Votes/Model/ConsentRequest.cs`
  - `Shared/Votes/Model/PluginVotes.cs`
  - `Shared/Votes/Model/PluginVote.cs`
  - `Shared/Votes/Model/TrackRequest.cs`
  - `Shared/Votes/Model/VoteRequest.cs`
  - `Shared/Network/SimpleHttpClient.cs` — performs all HTTP GET/POST calls
  - `Shared/Config/ConfigManager.cs` — reads `Core.DataHandlingConsent` and `InstallId`
  - `Shared/LogFile.cs` — logging
  - External stats REST API (Magnetar/Pulsar back-end service)
- **Used by:** [ConfigManager.cs](../Config/ConfigManager.cs.md), [Loader.cs](../Loader.cs.md)
