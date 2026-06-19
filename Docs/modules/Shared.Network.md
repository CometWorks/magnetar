# Module: Shared.Network

**Project:** `Shared` · **Files:** 7 · **Source lines:** 864

## Purpose

Provides all outbound network I/O for Magnetar: GitHub REST/CDN access for plugin downloads and update checks, full NuGet v3 client integration (dependency resolution, download, extraction) for plugin assembly dependencies, and a lightweight synchronous REST façade used by stats and other callers. The module abstracts the two target runtimes (net48 Legacy and net10.0 Interim) behind a single compile-time framework selection so the rest of the codebase stays platform-agnostic.

## Role in Magnetar

Consumed by the plugin loader pipeline (Shared.Data plugin types trigger GitHub downloads; plugin manifests declare NuGet dependencies that NuGetClient fulfils) and by Shared.Votes (SimpleHttpClient). It sits below Shared.Data and Shared.Config, which supply plugin metadata and runtime configuration respectively, and above no other Magnetar module.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `GitHub` | static class | [`Shared/Network/GitHub.cs`](../descriptions/Shared/Network/GitHub.cs.md) | Wraps GitHub REST API and raw CDN for repo archive downloads, single-file fetches, commit hash lookups, and release version queries. |
| `NuGetClient` | class | [`Shared/Network/NuGetClient.cs`](../descriptions/Shared/Network/NuGetClient.cs.md) | Full NuGet v3 client: resolves transitive dependencies, downloads packages from nuget.org, and extracts them into the local Magnetar package cache. |
| `NuGetLogger` | class | [`Shared/Network/NuGetLogger.cs`](../descriptions/Shared/Network/NuGetLogger.cs.md) | Adapts NuGet.Common.ILogger to Magnetar's LogFile/NLog pipeline with a [NuGet] prefix and mapped log levels. |
| `NuGetPackage` | class | [`Shared/Network/NuGetPackage.cs`](../descriptions/Shared/Network/NuGetPackage.cs.md) | Represents an extracted on-disk NuGet package; resolves the best-matching lib and content files for the active target framework. |
| `NuGetPackage.Item` | class | [`Shared/Network/NuGetPackage.cs`](../descriptions/Shared/Network/NuGetPackage.cs.md) | Nested DTO holding FilePath, Folder, and FullPath for one file within an extracted NuGet package. |
| `NuGetPackageId` | class | [`Shared/Network/NuGetPackageId.cs`](../descriptions/Shared/Network/NuGetPackageId.cs.md) | Serialisable DTO (ProtoBuf + XML) identifying a NuGet package by name and version; converts to NuGet SDK PackageIdentity via TryGetIdentity. |
| `NuGetPackageList` | class | [`Shared/Network/NuGetPackageList.cs`](../descriptions/Shared/Network/NuGetPackageList.cs.md) | Container for a plugin's NuGet dependency declaration: optional packages.config path and/or inline NuGetPackageId array. |
| `SimpleHttpClient` | static class | [`Shared/Network/SimpleHttpClient.cs`](../descriptions/Shared/Network/SimpleHttpClient.cs.md) | Synchronous REST façade with generic JSON GET/POST helpers and a 3-second timeout, used by stats reporting and similar short-lived API calls. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Shared/Network/GitHub.cs`](../descriptions/Shared/Network/GitHub.cs.md) | 140 | `GitHub` is a thin static HTTP façade over the GitHub REST API and raw-content CDN. |
| [`Shared/Network/NuGetClient.cs`](../descriptions/Shared/Network/NuGetClient.cs.md) | 248 | `NuGetClient` wraps the NuGet v3 client SDK to download and extract packages from `api.nuget.org` into a local cache inside Magnetar's data directory. |
| [`Shared/Network/NuGetLogger.cs`](../descriptions/Shared/Network/NuGetLogger.cs.md) | 87 | `NuGetLogger` adapts the NuGet SDK's `ILogger` interface to Magnetar's `LogFile` / NLog pipeline. |
| [`Shared/Network/NuGetPackage.cs`](../descriptions/Shared/Network/NuGetPackage.cs.md) | 124 | `NuGetPackage` represents a single NuGet package that has already been extracted to disk. |
| [`Shared/Network/NuGetPackageId.cs`](../descriptions/Shared/Network/NuGetPackageId.cs.md) | 47 | `NuGetPackageId` is a serialisable DTO that identifies a single NuGet package by name and version string. |
| [`Shared/Network/NuGetPackageList.cs`](../descriptions/Shared/Network/NuGetPackageList.cs.md) | 20 | `NuGetPackageList` is a compact container that carries a plugin's NuGet dependency declaration in two optional forms: a path to a `packages.config` file (`Config`) and/or an inline array of `NuGetPackageId` records (`PackageIds`). |
| [`Shared/Network/SimpleHttpClient.cs`](../descriptions/Shared/Network/SimpleHttpClient.cs.md) | 198 | `SimpleHttpClient` is a thin, synchronous REST façade built on `HttpWebRequest`. |

## Public API surface

- `GitHub.Init() — must be called at startup to enable TLS 1.2`
- `GitHub.GetStream(Uri) — core authenticated HTTP fetch returning a buffered MemoryStream`
- `GitHub.GetRepoArchive(string repo, string reference) — download a repo ZIP`
- `GitHub.GetRepoFile(string repo, string reference, string file) — download a single raw file`
- `GitHub.GetRepoHash(string repo, string reference, out string hash) — resolve a git ref to its commit SHA`
- `GitHub.GetReleaseVersion(string repo, out Version version, bool beta) — get latest release version`
- `GitHub.GetReleaseJson(string repo, string tag) — get full release JSON by tag`
- `NuGetClient.DownloadFromConfig(Stream) — download packages declared in a packages.config stream`
- `NuGetClient.DownloadPackages(IEnumerable<NuGetPackageId>, bool) — download explicit package list with optional dependency resolution`
- `NuGetClient.DownloadPackage(SourceCacheContext, PackageIdentity, NuGetFramework) — download and extract one package`
- `NuGetPackageId.TryGetIdentity(out PackageIdentity) — convert DTO to NuGet SDK identity`
- `NuGetPackageList.HasPackages — guard property before invoking NuGetClient`
- `NuGetPackageList.PackagesConfigNormalized — cross-platform config path`
- `SimpleHttpClient.Get<TV>(string) — typed JSON GET`
- `SimpleHttpClient.Post<TV,TR>(string, TR) — typed JSON POST with response`
- `SimpleHttpClient.Post<TR>(string, TR) — fire-and-forget JSON POST returning bool`

## Dependencies

**Uses modules:** [Shared.Config](Shared.Config.md)  
**Used by modules:** [Shared.Core](Shared.Core.md), [Shared.Data](Shared.Data.md), [Shared.Votes](Shared.Votes.md)  
**External systems:** GitHub; NuGet

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
