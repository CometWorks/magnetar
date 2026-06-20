# Module: Shared.Votes

**Project:** `Shared` · **Files:** 7 · **Source lines:** 361

## Purpose

Provides the client-side telemetry and community-rating layer for Magnetar. It sends anonymous usage tracking events to a remote stats server, fetches aggregate plugin statistics (active player counts, upvote/downvote totals, half-star ratings), and lets consenting players cast and update votes. Participation is opt-in and gated by a local consent state machine (ConsentManager): nothing is sent unless consent is granted, and all identification uses an anonymous instance identifier (the first 20 hex chars of a locally generated instance.id UUID), never a Steam ID or any personal data.

## Role in Magnetar

Sits between the configuration/launcher layer (ConfigManager, Program startup) and the external Pulsar/Magnetar stats REST service. ConsentManager owns the consent decision (flags, interactive prompt, instance.id lifecycle) and exposes Granted/PlayerHash; VotesClient is the only type that talks to the service; other modules consume the cached PluginVotes through ConfigManager.Instance.Votes rather than calling VotesClient directly.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `ConsentManager` | static class | [`Shared/Votes/ConsentManager.cs`](../descriptions/Shared/Votes/ConsentManager.cs.md) | Telemetry-consent state machine: resolves the decision from flags/instance.id/interactive prompt, derives the anonymous PlayerHash, and handles withdrawal. |
| `VotesClient` | static class | [`Shared/Votes/VotesClient.cs`](../descriptions/Shared/Votes/VotesClient.cs.md) | Four-operation REST client for the stats back-end: Consent, DownloadVotes, Track, Vote. |
| `PluginVotes` | class | [`Shared/Votes/Model/PluginVotes.cs`](../descriptions/Shared/Votes/Model/PluginVotes.cs.md) | Top-level response DTO from /Stats; maps plugin IDs to PluginVote records and carries the server-issued voting token. |
| `PluginVote` | class | [`Shared/Votes/Model/PluginVote.cs`](../descriptions/Shared/Votes/Model/PluginVote.cs.md) | Per-plugin statistics record: 30-day player count, lifetime vote totals, half-star rating, and the requesting player's personal tried/vote state. |
| `ConsentRequest` | class | [`Shared/Votes/Model/ConsentRequest.cs`](../descriptions/Shared/Votes/Model/ConsentRequest.cs.md) | Request body for /Consent: anonymous instance hash and a boolean consent flag. |
| `TrackRequest` | class | [`Shared/Votes/Model/TrackRequest.cs`](../descriptions/Shared/Votes/Model/TrackRequest.cs.md) | Request body for /Track: anonymous instance hash and the list of enabled plugin IDs at game start. |
| `VoteRequest` | class | [`Shared/Votes/Model/VoteRequest.cs`](../descriptions/Shared/Votes/Model/VoteRequest.cs.md) | Request body for /Vote: plugin ID, anonymous instance hash, voting token, and +1/0/-1 vote value. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Shared/Votes/ConsentManager.cs`](../descriptions/Shared/Votes/ConsentManager.cs.md) | 180 | Owns the telemetry-consent state machine: it decides, once per startup, whether anonymous plugin-usage statistics may be sent, and exposes the result to the rest of the loader through static properties. |
| [`Shared/Votes/Model/ConsentRequest.cs`](../descriptions/Shared/Votes/Model/ConsentRequest.cs.md) | 14 | Defines the JSON request body sent to the statistics server's `/Consent` endpoint when a user grants or withdraws data-handling consent. |
| [`Shared/Votes/Model/PluginVote.cs`](../descriptions/Shared/Votes/Model/PluginVote.cs.md) | 24 | Represents the statistics record for a single plugin as returned by the `/Stats` REST endpoint. |
| [`Shared/Votes/Model/PluginVotes.cs`](../descriptions/Shared/Votes/Model/PluginVotes.cs.md) | 24 | Top-level response container returned by the `/Stats` REST endpoint. |
| [`Shared/Votes/Model/TrackRequest.cs`](../descriptions/Shared/Votes/Model/TrackRequest.cs.md) | 11 | Request body POSTed to `/Track` each time the game starts, recording which plugins were active for a given anonymous instance. |
| [`Shared/Votes/Model/VoteRequest.cs`](../descriptions/Shared/Votes/Model/VoteRequest.cs.md) | 20 | Request body POSTed to `/Vote` when a player changes their vote on a plugin. |
| [`Shared/Votes/VotesClient.cs`](../descriptions/Shared/Votes/VotesClient.cs.md) | 88 | The single outbound client for Magnetar's statistics back-end, providing four REST operations: consent management, votes download, session tracking, and voting. |

## Public API surface

- `ConsentManager.Granted / PendingServerConsent / PlayerHash`
- `ConsentManager.Resolve()`
- `ConsentManager.Withdraw(string votesServer)`
- `VotesClient.BaseUrl (set by host at startup)`
- `VotesClient.Consent(bool, string playerHash = null) → bool`
- `VotesClient.DownloadVotes() → PluginVotes`
- `VotesClient.Track(string[]) → bool`
- `VotesClient.Vote(string, int) → PluginVote`
- `PluginVotes.GetVotesForPlugin(PluginData) → PluginVote`

## Dependencies

**Uses modules:** [Shared.Config](Shared.Config.md), [Shared.Core](Shared.Core.md), [Shared.Data](Shared.Data.md), [Shared.Network](Shared.Network.md)  
**Used by modules:** [Legacy.Launcher](Legacy.Launcher.md), [Shared.Config](Shared.Config.md), [Shared.Core](Shared.Core.md)  
**External systems:** External stats REST API (Pulsar/Magnetar back-end service)

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
