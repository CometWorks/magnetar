# ConfigTerminalTests/WorkshopResolverTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 178

## Summary
xUnit tests for `WorkshopResolver`, which turns Steam Workshop ids/URLs into mod names via the Steam Web API. Using a canned `IHttpFetcher` fake (no network), it verifies id extraction from bare numbers and URLs, dedup/ordering, the published-file-details JSON parse, single-mod name resolution, collection expansion into members in sort order, skipping non-mods with a warning, and keeping unavailable ids by id with a warning. One live test hits the real Steam API and is gated behind `MAGNETAR_LIVE=1`.

## Types
### WorkshopResolverTests — class, public
Fact/Theory suite driving the resolver against a fake transport that returns pre-baked JSON.

- **Methods:**
  - `ExtractIds_pulls_the_id_from_urls_and_bare_numbers()` (`[Theory]`) — extracts the id from bare numbers, whitespace-padded numbers, and `sharedfiles`/`workshop` filedetails URLs (with extra query params).
  - `ExtractIds_dedupes_and_keeps_order_for_multiple_ids()` — dedups repeated ids while preserving first-seen order.
  - `ExtractIds_ignores_short_numbers_and_empty_input()` — ignores <6-digit numbers, empty string, and null.
  - `ParsePublishedFileDetails_reads_id_title_type_and_tags()` — decodes id/title/type/tags into a `WorkshopItem` with `Ok` true and `IsCollection` false.
  - `Resolve_returns_the_friendly_name_for_a_single_mod()` — resolves a single mod's friendly name with no warnings.
  - `Resolve_expands_a_collection_into_its_members_in_sort_order()` — a collection (`file_type` 2) is expanded via `GetCollectionDetails` and members returned ordered by `sortorder`.
  - `Resolve_skips_non_mods_with_a_warning()` — a blueprint-tagged item is dropped from `Mods` with a single warning.
  - `Resolve_adds_unavailable_ids_without_a_name_and_warns()` — a `result != 1` id is kept (added by id, empty name) with a warning.
  - `Resolve_live_reads_the_example_mod_name()` — gated on `MAGNETAR_LIVE=1`; resolves id 657749341 against the real Steam Workshop API to "Automatic Ore Pickup".
- **Nested:** `FakeFetcher` — private sealed `IHttpFetcher` returning canned `Get`/`Post` bodies and recording endpoint `Calls` for assertions.

## Cross-references
- **Uses:** `WorkshopResolver`, `WorkshopItem`, `IHttpFetcher` (`ConfigTerminal/Model/`); Steam Web API (`GetPublishedFileDetails`/`GetCollectionDetails`, live test only); xUnit (`Theory`/`InlineData`/`Fact`); `System.Linq`.
- **Used by:** _none within the repository_
