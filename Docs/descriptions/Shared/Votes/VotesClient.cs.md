# Shared/Votes/VotesClient.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Votes` · **Kind:** static class · **Lines:** 88

## Summary
The single outbound client for Magnetar's statistics back-end, providing four REST operations: consent management, votes download, session tracking, and voting. The anonymous player identity is no longer derived here — every operation reads `ConsentManager.PlayerHash` (the first 20 hex chars of the local `instance.id` UUID), and consent gating is read from `ConsentManager.Granted`. The class only retains the most recently received voting token. All HTTP I/O is delegated to `SimpleHttpClient`; JSON serialization is handled transparently within that layer.

## Types
### `VotesClient` — static class, public
Owns all communication with the remote stats service. The four public methods map one-to-one onto the four server endpoints. Because `DownloadVotes` may be called from a background thread (as `ConfigManager.UpdatePlayerVotes` wraps it in `Task.Run`), the class is written to be safe for concurrent calls that do not interleave (the token assignment is last-write-wins which is sufficient given the single background task pattern).

- **Properties:**
  - `BaseUrl` — settable base URL for the stats API, set at startup by the host layer (or by `ConsentManager.Withdraw`) to point at the live server (or a test instance).

- **Fields (private):**
  - `votingToken` — the latest `VotingToken` received from `/Stats`; cleared to `null` when votes are fetched anonymously (no consent); required by `Vote`.

- **Computed properties (private):**
  - `ConsentUri`, `VotesUri`, `TrackUri`, `VoteUri` — string properties that prepend `BaseUrl` to the respective path segments; recomputed on each access so a `BaseUrl` change is immediately reflected.

- **Methods:**
  - `Consent(bool consent, string playerHash = null) → bool` — builds a `ConsentRequest` and POSTs it to `/Consent`; when `playerHash` is not supplied it falls back to `ConsentManager.PlayerHash` (the explicit argument lets `ConsentManager.Withdraw` pass a hash derived from the `instance.id` it is about to delete); logs the action before sending; returns `true` on HTTP 200.
  - `DownloadVotes() → PluginVotes` — fetches `/Stats`; if `ConsentManager.Granted` is `false` the request is made anonymously (no query parameters, `votingToken` set to `null`); if consent is active, `ConsentManager.PlayerHash` is passed as a query parameter and the returned `VotingToken` is cached for use by `Vote`. Safe to call from a non-UI thread.
  - `Track(string[] pluginIds) → bool` — POSTs a `TrackRequest` (with `ConsentManager.PlayerHash`) to `/Track` recording which plugins were active at startup; returns `true` on success.
  - `Vote(string pluginId, int vote) → PluginVote` — guards against a missing `votingToken` (logs an error and returns `null` if absent); otherwise POSTs a `VoteRequest` (with `ConsentManager.PlayerHash`) to `/Vote` and deserializes the server's updated `PluginVote` response, giving the caller the fresh record without a separate download.

## Cross-references
- **Uses:**
  - `Shared/Votes/ConsentManager.cs` — supplies `PlayerHash` and the `Granted` consent gate
  - `Shared/Votes/Model/ConsentRequest.cs`
  - `Shared/Votes/Model/PluginVotes.cs`
  - `Shared/Votes/Model/PluginVote.cs`
  - `Shared/Votes/Model/TrackRequest.cs`
  - `Shared/Votes/Model/VoteRequest.cs`
  - `Shared/Network/SimpleHttpClient.cs` — performs all HTTP GET/POST calls
  - `Shared/LogFile.cs` — logging
  - External stats REST API (Magnetar/Pulsar back-end service)
- **Used by:** [ConfigManager.cs](../Config/ConfigManager.cs.md), [Loader.cs](../Loader.cs.md), [ConsentManager.cs](ConsentManager.cs.md)
