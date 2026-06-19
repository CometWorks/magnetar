# Shared/Votes/Model/TrackRequest.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Votes.Model` · **Kind:** class · **Lines:** 17

## Summary
Request body POSTed to `/Track` each time the game starts, recording which plugins were active for a given (anonymized) player. This event is the source of `PluginVote.Players` counts on the server. The player is identified only by a truncated SHA-1 hash of the install ID — the comment documents the exact hash format (first 80 bits, hex-encoded) to make the privacy model auditable.

## Types
### `TrackRequest` — class, public
DTO serialized to JSON by `VotesClient.Track`. Both fields are always populated before the request is sent.

- **Properties:**
  - `PlayerHash` — first 20 hex characters of `SHA1(installId)` (80 bits); computed client-side and never reversed by the server; provides near-perfect deduplication while preventing Steam ID extraction from the server database.
  - `EnabledPluginIds` — string array of plugin IDs that were enabled at game start; one tracking record per plugin is created or updated server-side for the 30-day rolling window.

## Cross-references
- **Uses:** nothing (pure DTO)
- **Used by:** [VotesClient.cs](../VotesClient.cs.md)
