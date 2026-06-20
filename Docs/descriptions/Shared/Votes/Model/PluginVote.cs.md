# Shared/Votes/Model/PluginVote.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Votes.Model` · **Kind:** class · **Lines:** 24

## Summary
Represents the statistics record for a single plugin as returned by the `/Stats` REST endpoint. It bundles aggregate community metrics (active player count, lifetime vote totals, star rating) with per-requesting-player context (whether the player has tried the plugin and their current vote). This dual nature allows the UI to render both public reputation and personal history from a single server round-trip.

## Types
### `PluginVote` — class, public
A plain DTO deserialized from the JSON body of a `GET /Stats` response. Consumed by `PluginVotes.GetVotesForPlugin` and surfaced through `ConfigManager.Votes` to the UI layer.

- **Properties:**
  - `Players` — count of distinct players (by hashed ID) who ran SE with this plugin active during the past 30 days; used to indicate current adoption.
  - `Upvotes` — lifetime total upvotes cast on this plugin (votes do not expire).
  - `Downvotes` — lifetime total downvotes cast on this plugin.
  - `Tried` — `true` when the requesting player has been tracked as having run this plugin at least once; gates the voting UI.
  - `Vote` — the requesting player's current vote: `+1` (upvote), `0` (none or cleared), or `-1` (downvote).
  - `Rating` — server-computed quality indicator expressed in half-stars (range 1–10, i.e. ½ to 5 stars); set to `0` when the plugin has not yet accumulated enough votes for a statistically meaningful score.

## Cross-references
- **Uses:** nothing (pure DTO)
- **Used by:** [PluginVotes.cs](PluginVotes.cs.md), [VotesClient.cs](../VotesClient.cs.md)
