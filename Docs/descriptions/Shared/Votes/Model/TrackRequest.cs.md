# Shared/Votes/Model/TrackRequest.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Votes.Model` · **Kind:** class · **Lines:** 11

## Summary
Request body POSTed to `/Track` each time the game starts, recording which plugins were active for a given anonymous instance. This event is the source of `PluginVote.Players` counts on the server. The instance is identified only by the anonymous `instance.id`-derived hash, never a Steam ID.

## Types
### `TrackRequest` — class, public
DTO serialized to JSON by `VotesClient.Track`. Both fields are always populated before the request is sent.

- **Properties:**
  - `PlayerHash` — anonymous instance identifier: the first 20 hex characters of the local `instance.id` UUID (see [`ConsentManager`](../ConsentManager.cs.md)); provides deduplication across restarts without exposing any account or Steam ID.
  - `EnabledPluginIds` — string array of plugin IDs that were enabled at game start (including built-in compatibility plugins); one tracking record per plugin is created or updated server-side for the 30-day rolling window.

## Cross-references
- **Uses:** nothing (pure DTO)
- **Used by:** [VotesClient.cs](../VotesClient.cs.md)
