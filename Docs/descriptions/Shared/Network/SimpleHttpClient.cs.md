# Shared/Network/SimpleHttpClient.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Network` · **Kind:** static class · **Lines:** 198

## Summary
`SimpleHttpClient` is a thin, synchronous REST façade built on `HttpWebRequest`. It provides generic typed GET and POST helpers that automatically serialise/deserialise JSON payloads via `Newtonsoft.Json`. It is used for short-lived REST API calls (e.g., stats reporting via `VotesClient`) where the 3-second hard timeout is acceptable and async complexity is unnecessary.

## Types

### `SimpleHttpClient` — static class, public

All public methods are generic with the constraint `where TV : class, new()`. JSON (de)serialisation is handled by `JsonConvert`. Errors are swallowed and logged via `LogFile.Error`; the caller receives `null` (or `false`) on failure.

- **Fields:**
  - `TimeoutMs` — constant `int` (3000); applied to every `HttpWebRequest` as `Timeout`

- **Methods:**
  - `Get<TV>(string url)` — issues a GET request to `url`, deserialises the response body as `TV`; returns `null` on `WebException`
  - `Get<TV>(string url, Dictionary<string, string> parameters)` — appends URL-encoded query parameters via `AppendQueryParameters` then delegates to the same GET logic; returns `null` on `WebException`
  - `Post<TV>(string url)` — issues a no-body POST (content-length 0); returns the deserialised `TV` response or `null`
  - `Post<TV>(string url, Dictionary<string, string> parameters)` — appends query parameters and issues a POST with `application/x-www-form-urlencoded` content type and zero content body; returns `TV` or `null`
  - `Post<TV, TR>(string url, TR body)` — serialises `body` to JSON, sets `Content-Type: application/json`, writes the bytes to the request stream, and deserialises the response as `TV`; returns `null` on `WebException`
  - `Post<TR>(string url, TR body)` — fire-and-forget variant; serialises `body` to JSON, POSTs it, and returns `true` only when the response status is `HttpStatusCode.OK`
  - `PostRequest<TV>(HttpWebRequest, byte[])` — private; optionally writes `body` bytes to the request stream, reads and deserialises the response
  - `PostRequest(HttpWebRequest, byte[])` — private; optionally writes body bytes and returns `response.StatusCode == HttpStatusCode.OK`
  - `CreateRequest(HttpMethod, string)` — private; creates `HttpWebRequest` with `Timeout = TimeoutMs` and the given HTTP method
  - `AppendQueryParameters(StringBuilder, Dictionary<string, string>)` — private; appends `?key=val&...` to the builder using `Uri.EscapeDataString` for both keys and values

## Cross-references
- **Uses:**
  - `Shared/LogFile.cs` — `LogFile.Error` for network failure messages
  - External: `Newtonsoft.Json.JsonConvert` — request/response serialisation
  - External: `System.Net.HttpWebRequest` / `HttpWebResponse`
  - External: `System.Net.Http.HttpMethod`
- **Used by:** [VotesClient.cs](../Votes/VotesClient.cs.md)
