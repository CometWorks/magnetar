# Shared/Votes/Model/VoteRequest.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Votes.Model` · **Kind:** class · **Lines:** 19

## Summary
Request body POSTed to `/Vote` when a player changes their vote on a plugin. The `VotingToken` field, received with the previous `/Stats` response and cached in `VotesClient`, links this request back to an authenticated stats session, making it harder to programmatically fabricate votes without first fetching real stats. The vote value uses the signed-integer convention (`+1`/`0`/`-1`) shared with `PluginVote.Vote`.

## Types
### `VoteRequest` — class, public
DTO serialized to JSON by `VotesClient.Vote`. All four fields must be populated; `VotesClient` guards against a null `votingToken` before constructing this object.

- **Properties:**
  - `PluginId` — identifier of the plugin being voted on.
  - `PlayerHash` — same anonymous instance identifier used in `TrackRequest` (first 20 hex chars of the `instance.id` UUID); ties the vote to the anonymous instance identity without exposing any account or Steam ID.
  - `VotingToken` — opaque token obtained from the most recent `PluginVotes` response; the server validates it to reject forged vote requests.
  - `Vote` — integer vote value: `+1` for upvote, `0` to clear a prior vote, `-1` for downvote.

## Cross-references
- **Uses:** nothing (pure DTO)
- **Used by:** [VotesClient.cs](../VotesClient.cs.md)
