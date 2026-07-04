# Shared/Network/GitHub.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Network` · **Kind:** static class · **Lines:** 149

## Summary
`GitHub` is a thin static HTTP façade over the GitHub REST API and raw-content CDN. It provides download streams for repository ZIP archives and individual files (used when installing GitHub-hosted plugins), commit SHA lookups (used to detect upstream changes), and release version queries (used for update checks). It also performs one-time TLS 1.2 enablement, which is required on Windows 7/Server 2008 targets where .NET Framework 4.8 does not enable TLS 1.2 by default.

## Types

### `GitHub` — static class, public

Single responsibility: wraps four GitHub URL templates and exposes typed helpers on top of them. All HTTP work goes through `GetStream`, which creates a `HttpWebRequest` with the `Pulsar` user-agent string, enables gzip/deflate decompression, applies the configured `NetworkTimeout`, adds normalized authorization from `Flags.GitHubToken` for known GitHub hosts when present, and optionally blocks IPv6 binding via a `BindIPEndPointDelegate`. The full response is buffered into a rewound `MemoryStream` before returning so callers do not hold the connection open.

- **Fields:**
  - `CommitInfo` — URL template: `https://api.github.com/repos/{0}/commits/{1}`
  - `ReleaseInfo` — URL template: `https://api.github.com/repos/{0}/releases`
  - `FetchRepo` — URL template: `https://api.github.com/repos/{0}/zipball/{1}`
  - `FetchFile` — URL template: `https://raw.githubusercontent.com/{0}/{1}/`

- **Methods:**
  - `Init()` — sets `ServicePointManager.SecurityProtocol |= Tls12`; logs a non-fatal error if the platform does not support TLS 1.2 and continues
  - `GetStream(Uri)` — core HTTP fetch: creates `HttpWebRequest`, applies `CoreConfig` timeout, adds a normalized `Authorization` header when `Flags.GitHubToken` is set and the host is GitHub-owned, applies optional IPv6 block, copies the response body into a `MemoryStream`, rewinds it, and returns it to the caller
  - `IsGitHubHost(string host)` — private guard that limits authorization-header attachment to `github.com`, `api.github.com`, `raw.githubusercontent.com`, and `codeload.github.com`
  - `GetAuthorizationHeader()` — private helper that trims the configured token, accepts an already-prefixed `Authorization:`, `Bearer`, or `token` value, and otherwise emits a `token <value>` header compatible with classic GitHub personal access tokens
  - `BlockIPv6(ServicePoint, IPEndPoint, int)` — `BindIPEndPointDelegate` callback; allows IPv4 endpoints through (`IPAddress.Any`) and throws for IPv6, effectively forcing IPv4-only connections
  - `GetRepoArchive(string repo, string reference)` — builds a ZIP download URI from `FetchRepo` and delegates to `GetStream`; logs the URL before fetching
  - `GetRepoFile(string repo, string reference, string file)` — builds a raw file URI from `FetchFile` and delegates to `GetStream`; strips a leading slash from `file` before appending
  - `GetRepoHash(string repo, string reference, out string hash)` — calls the commits API, parses the JSON object and extracts the `sha` field; returns `false` and logs on any exception
  - `GetReleaseVersion(string repo, out Version version, bool beta)` — calls the releases API, iterates releases, skips pre-releases unless `beta` is `true`, parses the first matching `tag_name` as a `System.Version` (after stripping a leading `v`); returns `false` on failure
  - `GetReleaseJson(string repo, string tag)` — calls `releases/tags/{tag}` and returns the full `JObject`; used when the caller needs raw release metadata
  - `GetText(string url)` — private helper: calls `GetStream` and reads the response to a string using a `StreamReader`

## Cross-references
- **Uses:**
  - `Shared/Config/ConfigManager.cs` — reads `ConfigManager.Instance.Core` for `NetworkTimeout` and `AllowIPv6`
  - `Shared/Config/CoreConfig.cs` — `CoreConfig.NetworkTimeout`, `CoreConfig.AllowIPv6`
  - `Shared/LogFile.cs` — `LogFile.WriteLine`, `LogFile.Error`
  - External: GitHub REST API (`api.github.com`), GitHub raw content CDN (`raw.githubusercontent.com`)
  - External: `Newtonsoft.Json` (`JObject`, `JArray`, `JToken`)
- **Used by:** [GitHubPlugin.CacheManifest.cs](../Data/GitHubPlugin.CacheManifest.cs.md), [GitHubPlugin.cs](../Data/GitHubPlugin.cs.md), [Profile.cs](../Data/Profile.cs.md), [Flags.cs](../Flags.cs.md), [Loader.cs](../Loader.cs.md), [PluginList.cs](../PluginList.cs.md), [Updater.cs](../Updater.cs.md)
