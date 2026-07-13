# ConfigTerminal/Model/DefaultHttpFetcher.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 40

## Summary
The live HTTP transport for `WorkshopResolver`: a plain `HttpClient` with a short (15s) timeout and a friendly user agent. Kept tiny and dependency-free, confining all network use here so the resolver's parsing stays pure and testable.

## Types
### DefaultHttpFetcher — sealed class, internal (implements `IHttpFetcher`)
- **Fields:** `Client` (a shared static `HttpClient`).
- **Methods:**
  - `CreateClient()` (private static) — builds the `HttpClient` with a 15s timeout and `User-Agent: MagnetarConfig/1.0`.
  - `Get(string url)` — synchronous GET (blocking on the async call), throwing on non-success.
  - `Post(string url, form)` — synchronous form-URL-encoded POST, throwing on non-success.

## Cross-references
- **Uses:** `IHttpFetcher` (this module); `System.Net.Http` (`HttpClient`, `FormUrlEncodedContent`).
- **Used by:** [WorkshopResolver.cs](WorkshopResolver.cs.md)
