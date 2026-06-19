# Shared/Votes/Model/PluginVotes.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Votes.Model` · **Kind:** class · **Lines:** 24

## Summary
Top-level response container returned by the `/Stats` REST endpoint. It wraps the per-plugin statistics map and carries a server-issued voting token that the client must present on subsequent vote requests, acting as a lightweight anti-spoofing mechanism. `ConfigManager` caches the deserialized instance so that the UI layer can read it without additional network calls.

## Types
### `PluginVotes` — class, public
Aggregates all per-plugin statistics into a keyed dictionary and provides a convenience accessor. Deserialized by `VotesClient.DownloadVotes` from the JSON response body and stored in `ConfigManager.Instance.Votes`.

- **Properties:**
  - `Votes` — `Dictionary<string, PluginVote>` mapping plugin ID strings to their individual `PluginVote` records; initialized to an empty dictionary so callers never encounter `null`. Carries `[JsonProperty("Stats")]` so the wire key stays `Stats`, preserving compatibility with the statistics and voting backend server.
  - `VotingToken` — opaque server-generated token required by `VotesClient.Vote`; only populated when the request included a `playerHash` query parameter (i.e. when `DataHandlingConsent` is enabled); cached locally in `VotesClient.votingToken`.

- **Methods:**
  - `GetVotesForPlugin(PluginData data) → PluginVote` — looks up statistics by `data.Id`; returns a default-constructed (all-zero) `PluginVote` if the plugin is absent from the dictionary, ensuring callers always get a safe value without null-checks.

## Cross-references
- **Uses:** `Shared/Data/PluginData.cs` (parameter type of `GetVotesForPlugin`)
- **Used by:** [VotesClient.cs](../VotesClient.cs.md), [ConfigManager.cs](../../Config/ConfigManager.cs.md)
