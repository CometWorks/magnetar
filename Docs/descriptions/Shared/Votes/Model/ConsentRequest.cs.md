# Shared/Votes/Model/ConsentRequest.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Votes.Model` · **Kind:** class · **Lines:** 14

## Summary
Defines the JSON request body sent to the statistics server's `/Consent` endpoint when a user grants or withdraws data-handling consent. This DTO exists to satisfy GDPR-style data-protection requirements: the server only receives a hashed, non-reversible player identifier and a boolean flag, never a raw Steam ID. The request is intentionally omitted when the user declines consent in the first place — it is only transmitted on a change.

## Types
### `ConsentRequest` — class, public
A minimal DTO carrying the two fields the `/Consent` REST endpoint requires. Serialized to JSON by `VotesClient.Consent` via `SimpleHttpClient.Post`.

- **Properties:**
  - `PlayerHash` — truncated SHA-1 hex of the install-specific identifier (see `CoreConfig.InstallId`), used server-side to locate or remove the user's record without exposing the Steam ID.
  - `Consent` — `true` when consent is being granted, `false` when it is being withdrawn.

## Cross-references
- **Uses:** `Shared/Votes/VotesClient.cs` (constructs and posts this type)
- **Used by:** [VotesClient.cs](../VotesClient.cs.md)
