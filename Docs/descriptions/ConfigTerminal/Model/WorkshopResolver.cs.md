# ConfigTerminal/Model/WorkshopResolver.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class, interface · **Lines:** 259

## Summary
Looks up Steam Workshop mod metadata (friendly names, collection members) so the per-world mod-list editor can accept a Workshop URL or id and fill the name in automatically. Modelled on Quasar's `QuasarWorkshopModResolver` but trimmed to the keyless `ISteamRemoteStorage` endpoints (no Steam Web API key). All wire parsing lives in static methods so it is testable against captured fixtures with no live calls.

## Types
### WorkshopItem — sealed class, internal
Basic metadata for one published file: `Id`, `Result` (1 == OK), `Title`, `FileType` (2 == collection), `Tags`, `ChildIds`; derived `Ok`, `IsCollection`, `IsClearlyNonMod` (world/blueprint/ingameScript tag), `DisplayName`.
### IHttpFetcher — interface, internal
Pluggable HTTP transport (`Get(url)`, `Post(url, form)`) so parsing can be unit-tested without network.
### WorkshopResolver.ResolveResult — sealed class, internal (nested)
Outcome of a resolve: `Mods` (`List<(long Id, string Name)>` in stable order) and `Warnings`.
### WorkshopResolver — sealed class, internal
- **Consts/fields:** `SpaceEngineersAppId` (244850), `IdPattern` (compiled regex), `http`.
- **Constructor:** takes an optional `IHttpFetcher` (defaults to `DefaultHttpFetcher`).
- **Methods:**
  - `ExtractIds(string text)` (static) — pulls Workshop ids from free text/URLs, deduped, in order.
  - `GetDetails(ids)` — fetches basic details in 100-id batches via `GetPublishedFileDetails`.
  - `GetCollectionChildren(collectionIds)` — expands collection ids into ordered member ids via `GetCollectionDetails`.
  - `Resolve(ids)` — resolves names, expands collections one level into ordered members, drops non-mods, and returns addable mods plus warnings; throws on transport failure.
  - `ParsePublishedFileDetails(json)` / `ParseCollectionDetails(json)` (static) — parse the Steam envelopes via `MiniJson`; empty result on malformed JSON.

## Cross-references
- **Uses:** `MiniJson`/`JsonValue` (`Model/Json/`), `DefaultHttpFetcher` (this module); `System.Text.RegularExpressions`, `System.Linq`; Steam `ISteamRemoteStorage` Web API.
- **Used by:** [DefaultHttpFetcher.cs](DefaultHttpFetcher.cs.md), [ModListView.cs](../Ui/ModListView.cs.md), [WorkshopResolverTests.cs](../../ConfigTerminalTests/WorkshopResolverTests.cs.md)
