# Module: Shared.Votes

**Project:** `Shared` · **Files:** 6 · **Source lines:** 190

## Purpose

Provides the client-side telemetry and community-rating layer for Magnetar. It sends anonymized usage tracking events to a remote stats server, fetches aggregate plugin statistics (active player counts, upvote/downvote totals, half-star ratings), and lets consenting players cast and update votes. All player identification uses a truncated SHA-1 hash of a locally-generated install ID, never raw Steam IDs, to satisfy data-protection requirements.

## Role in Magnetar

Sits between the configuration/UI layer (ConfigManager, the launcher UI) and the external Pulsar/Magnetar stats REST service. It is the only module that communicates with that service; other modules consume the cached PluginVotes result through ConfigManager.Instance.Votes rather than calling VotesClient directly.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `VotesClient` | static class | [`Shared/Votes/VotesClient.cs`](../descriptions/Shared/Votes/VotesClient.cs.md) | Four-operation REST client for the stats back-end: Consent, DownloadVotes, Track, Vote. |
| `PluginVotes` | class | [`Shared/Votes/Model/PluginVotes.cs`](../descriptions/Shared/Votes/Model/PluginVotes.cs.md) | Top-level response DTO from /Stats; maps plugin IDs to PluginVote records and carries the server-issued voting token. |
| `PluginVote` | class | [`Shared/Votes/Model/PluginVote.cs`](../descriptions/Shared/Votes/Model/PluginVote.cs.md) | Per-plugin statistics record: 30-day player count, lifetime vote totals, half-star rating, and the requesting player's personal tried/vote state. |
| `ConsentRequest` | class | [`Shared/Votes/Model/ConsentRequest.cs`](../descriptions/Shared/Votes/Model/ConsentRequest.cs.md) | Request body for /Consent: hashed player ID and a boolean consent flag. |
| `TrackRequest` | class | [`Shared/Votes/Model/TrackRequest.cs`](../descriptions/Shared/Votes/Model/TrackRequest.cs.md) | Request body for /Track: hashed player ID and the list of enabled plugin IDs at game start. |
| `VoteRequest` | class | [`Shared/Votes/Model/VoteRequest.cs`](../descriptions/Shared/Votes/Model/VoteRequest.cs.md) | Request body for /Vote: plugin ID, hashed player ID, voting token, and +1/0/-1 vote value. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Shared/Votes/Model/ConsentRequest.cs`](../descriptions/Shared/Votes/Model/ConsentRequest.cs.md) | 14 | Defines the JSON request body sent to the statistics server's `/Consent` endpoint when a user grants or withdraws data-handling consent. |
| [`Shared/Votes/Model/PluginVote.cs`](../descriptions/Shared/Votes/Model/PluginVote.cs.md) | 24 | Represents the statistics record for a single plugin as returned by the `/Stats` REST endpoint. |
| [`Shared/Votes/Model/PluginVotes.cs`](../descriptions/Shared/Votes/Model/PluginVotes.cs.md) | 24 | Top-level response container returned by the `/Stats` REST endpoint. |
| [`Shared/Votes/Model/TrackRequest.cs`](../descriptions/Shared/Votes/Model/TrackRequest.cs.md) | 17 | Request body POSTed to `/Track` each time the game starts, recording which plugins were active for a given (anonymized) player. |
| [`Shared/Votes/Model/VoteRequest.cs`](../descriptions/Shared/Votes/Model/VoteRequest.cs.md) | 20 | Request body POSTed to `/Vote` when a player changes their vote on a plugin. |
| [`Shared/Votes/VotesClient.cs`](../descriptions/Shared/Votes/VotesClient.cs.md) | 94 | The single outbound client for Magnetar's statistics back-end, providing four REST operations: consent management, votes download, session tracking, and voting. |

## Public API surface

- `VotesClient.BaseUrl (set by host at startup)`
- `VotesClient.Consent(bool) → bool`
- `VotesClient.DownloadVotes() → PluginVotes`
- `VotesClient.Track(string[]) → bool`
- `VotesClient.Vote(string, int) → PluginVote`
- `PluginVotes.GetVotesForPlugin(PluginData) → PluginVote`

## Dependencies

**Uses modules:** [Shared.Config](Shared.Config.md), [Shared.Core](Shared.Core.md), [Shared.Data](Shared.Data.md), [Shared.Network](Shared.Network.md)  
**Used by modules:** [Shared.Config](Shared.Config.md), [Shared.Core](Shared.Core.md)  
**External systems:** External stats REST API (Pulsar/Magnetar back-end service)

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
