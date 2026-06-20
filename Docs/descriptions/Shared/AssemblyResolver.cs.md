# Shared/AssemblyResolver.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared` · **Kind:** class · **Lines:** 107

## Summary
Provides a scoped `AppDomain.AssemblyResolve` handler that satisfies managed assembly load requests from one or more "source" folders, but only when the *requesting* assembly is on an allow-list. This lets Magnetar redirect plugin/SDK dependency resolution to bundled DLLs without globally hijacking the CLR's resolution for unrelated assemblies, which is important because the SE DS process already has its own assembly probing logic. The allow-list (by assembly name, by full file path, or by membership in a registered source folder) keeps the resolver from serving arbitrary callers.

## Types
### `AssemblyResolver` — class, public
Lazily hooks `AppDomain.CurrentDomain.AssemblyResolve` the first time a non-empty source folder is registered, then resolves requests by mapping the requested simple assembly name to a `.dll` file discovered in those folders. Resolution is gated by `IsAllowedRequest` so only trusted requesting assemblies are served.
- **Fields:** `allowedAssemblyNames` — set of simple names whose load requests are honored; `allowedAssemblyFiles` — set of full file paths of trusted requesting assemblies; `sourceFolders` — full paths of registered folders (any assembly located under one is trusted); `assemblies` — map of simple name -> full `.dll` path discovered in source folders; `enabled` — whether the `AssemblyResolve` event has been hooked yet.
- **Events:** `AssemblyResolved` — `Action<string>` raised with the resolved DLL path each time a request is satisfied (consumers use it to track which dependency files were actually loaded).
- **Methods:** `AddAllowedAssemblyName(string)` — adds a simple name to the trusted-requester set; `AddAllowedAssemblyFile(string)` — adds a full file path (normalized via `Path.GetFullPath`) to the trusted-requester set; `AddSourceFolder(string folder, SearchOption=TopDirectoryOnly)` — enumerates `*.dll` under the folder, records the first occurrence of each simple name, and lazily subscribes the resolver on first discovery (no-op if the folder does not exist); `Resolve(object, ResolveEventArgs)` — the event handler: returns `null` if the requester is not allowed, otherwise `Assembly.LoadFile`s the mapped path, fires `AssemblyResolved`, logs, and returns the assembly; `IsAllowedRequest(Assembly)` — returns true when the requesting assembly has no location and its name is allow-listed, or its location matches an allowed file/source folder, or its simple name is allow-listed.

## Cross-references
- **Uses:** `Shared/LogFile.cs` (resolution logging); .NET `System.Reflection` / `AppDomain` assembly-resolution APIs.
- **Used by:** [Program.cs](../Legacy/Program.cs.md), [GitHubPlugin.cs](Data/GitHubPlugin.cs.md), [LocalFolderPlugin.cs](Data/LocalFolderPlugin.cs.md), [LocalPlugin.cs](Data/LocalPlugin.cs.md), [Preloader.cs](Preloader.cs.md)
