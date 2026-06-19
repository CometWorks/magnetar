# Shared/Tools.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared` · **Kind:** static class (+ interface) · **Lines:** 179

## Summary
Grab-bag of cross-cutting utilities used throughout Magnetar: SHA-256 hashing of files/strings/folders (used for cache invalidation), human-friendly "time ago" formatting, console/error message reporting, file globbing, filename sanitizing, JSON-based deep copy, and a cross-platform native crash handler. It also holds the two injected service references (`IExternalTools` for marshalling onto the SE main thread, `ICompilerFactory` for the Roslyn compiler) wired up at startup via `Init`.

## Types
### `IExternalTools` — interface, public
Environment bridge so shared code can request main-thread execution without referencing SE's update loop directly.
- **Methods:** `OnMainThread(Action action)` — schedule `action` to run on the SE game/update thread.

### `Tools` — static class, public
Static utility surface plus injected service holders.
- **Properties:** `External` — the registered `IExternalTools` (`{ get; private set; }`); `Compiler` — the registered `ICompilerFactory` (`{ get; private set; }`).
- **Methods:** `Init(IExternalTools, ICompilerFactory)` — stores the injected services; `GetFileHash` / `GetStringHash` — SHA-256 hex digest of a file / UTF-8 string; `GetHash(Stream, HashAlgorithm)` — hex-encodes a computed hash; `GetFolderHash(string, glob="*")` — concatenates per-file hashes (sorted by file name) then hashes the result (throws if folder missing); `DateToString(DateTime?)` — "Never"/"Just Now"/"N minutes/hours/days ago" relative formatting from UTC; `ShowMessage(string)` — writes a `[Pulsar] ` line to `Console.Error` (normalizing newlines) and logs it as an error; `GetFiles(path, includeGlobs, excludeGlobs)` — file names (without extension) matching includes minus excludes (case-insensitive); `CleanFileName(string)` — replaces invalid filename chars with `-`; `DeepCopy<T>(T)` — round-trips through Newtonsoft JSON; `RemoveAll(string, IEnumerable<string>)` — strips all given substrings; `IsNative()` — true unless `STEAM_COMPAT_PROTON` is set (i.e. not running under Proton); `InstallNativeCrashHandler(string label)` — on Windows installs a SEH `SetUnhandledExceptionFilter` (P/Invoke `kernel32`) that logs and exits on native faults; no-op on Linux where CoreCLR already converts signals to managed exceptions.
- **Fields:** `nativeFilterDelegate` (private) — keeps the crash-filter delegate alive against GC.

## Cross-references
- **Uses:** `Shared/LogFile.cs`; Compiler (`ICompilerFactory`); Newtonsoft.Json; `System.Security.Cryptography`, `System.Runtime.InteropServices` (P/Invoke); external system Steam/Proton (env var).
- **Used by:** [Profile.cs](Data/Profile.cs.md), [Launcher.cs](Launcher.cs.md), [Folder.cs](../Legacy/Launcher/Folder.cs.md), [GitHubPlugin.cs](Data/GitHubPlugin.cs.md), [Patch_MyDefinitionErrors.cs](../Legacy/Patch/Patch_MyDefinitionErrors.cs.md), [References.cs](../Legacy/Compiler/References.cs.md), [LocalFolderPlugin.cs](Data/LocalFolderPlugin.cs.md), [GitHubPlugin.AssetFile.cs](Data/GitHubPlugin.AssetFile.cs.md), [PluginData.cs](Data/PluginData.cs.md), [ProfilesConfig.cs](Config/ProfilesConfig.cs.md), [PluginList.cs](PluginList.cs.md), [Preloader.cs](Preloader.cs.md), [VotesClient.cs](Votes/VotesClient.cs.md), [Program.cs](../Legacy/Program.cs.md), [Updater.cs](Updater.cs.md), [PluginInstance.cs](../Legacy/Loader/PluginInstance.cs.md), [Loader.cs](Loader.cs.md)
