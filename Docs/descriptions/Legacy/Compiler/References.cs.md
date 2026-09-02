# Legacy/Compiler/References.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Compiler` · **Kind:** static class (internal) · **Lines:** 36

## Summary
Provides the list of assembly references that the Roslyn compiler must know about when compiling SE scripts and plugins. It combines glob-matched Space Engineers game assemblies (discovered from the game's executable directory) with a fixed set of standard framework/library names. Both `CompilerFactory` implementations (Legacy and Interim) call `GetReferences` during their `Init` phase to seed `RoslynReferences.GenerateAssemblyList`.

## Types

### `References` — static class, internal
Enumerates all assemblies the Roslyn compiler should reference. The SE-specific assemblies are discovered by glob patterns applied to the game directory via `Tools.GetFiles`; the result excludes `VRage.Native.dll` which cannot be safely referenced. A hard-coded `baseEnvironment` list adds compiler-agnostic framework and library assemblies that all compiled scripts/plugins will need (`Microsoft.CSharp`, `0Harmony`, `Newtonsoft.Json`, `Mono.Cecil`, `NLog`, `PluginSdk`).

- **Fields:** `baseEnvironment — string[] of unconditional assembly names (no path, resolved by RoslynReferences later), including Magnetar.Protocol so source plugins can implement Quasar companion contracts`; `includeGlobs — string[] of filename globs covering SpaceEngineers*, VRage*, Sandbox*, ProtoBuf* DLLs`; `excludeGlobs — string[] containing VRage.Native.dll to prevent it from being referenced`
- **Methods:** `GetReferences(exeLocation) — yields glob-matched SE assembly paths from the game directory, then yields each name in baseEnvironment; returns IEnumerable<string>`

## Cross-references
- **Uses:** `Shared/Tools.cs` (`Tools.GetFiles` for glob-based file discovery); `Legacy/Compiler/Interim.cs` and `Legacy/Compiler/Legacy.cs` (callers)
- **Used by:** [Interim.cs](Interim.cs.md), [Legacy.cs](Legacy.cs.md)
