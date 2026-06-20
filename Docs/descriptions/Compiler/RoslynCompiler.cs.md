# Compiler/RoslynCompiler.cs

**Project:** Compiler · **Namespace:** `Pulsar.Compiler` · **Kind:** class (+ 2 interfaces, 1 nested class) · **Lines:** 171

## Summary
The core in-process C# compiler used to build local/Workshop plugins from source at server startup. It accumulates source streams, then emits a DLL (and optionally a portable PDB with embedded source) via Roslyn `CSharpCompilation`. It wires in the full SE DS reference set from `RoslynReferences`, applies assembly publicizing for sources that request it, and supports extra `.dll` dependencies. `RoslynCompiler` is a `MarshalByRefObject` so it can be driven across an AppDomain boundary (the loader compiles plugins in an isolated domain). The `ICompilerFactory`/`ICompiler` interfaces decouple the loader from this concrete implementation.

## Types
### ICompilerFactory — interface, public : `IDisposable`
Factory abstraction for producing compilers (allows the host to set up the compile environment once and create per-plugin compilers).
- **Methods:** `Init()` — one-time setup of the compile environment; `Create(bool debugBuild = false)` — returns a fresh `ICompiler`, optionally configured for debug builds.

### ICompiler — interface, public
Contract for a single plugin compilation unit.
- **Methods:** `Load(Stream s, string name, string embedFile = null)` — add a source file; `Compile(string assemblyName, out byte[] symbols)` — emit IL bytes (and PDB bytes via `symbols`); `TryAddDependency(string dll)` — register an extra reference assembly.

### RoslynCompiler — class, public : `MarshalByRefObject`, `ICompiler`
Concrete Roslyn-based compiler. Collects sources, inspects each for publicizing requirements, and emits a dynamically-linked library with unsafe code allowed.
- **Fields:** `DebugBuild` — public flag selecting Debug vs Release optimization and PDB emission; `Flags` — public `string[]` of preprocessor symbols passed to the parser; `source` — `List<Source>` of accumulated parsed sources; `publicizedAssemblies` — a `PublicizedAssemblies` collecting `IgnoresAccessChecksTo` declarations across all loaded sources; `customReferences` — `List<MetadataReference>` of extra DLLs added via `TryAddDependency`.
- **Methods:**
  - `Load(Stream s, string name, string embedFile = null)` — builds `CSharpParseOptions` for `LanguageVersion.CSharp14` with `Flags` as preprocessor symbols, copies the input stream into a `MemoryStream`, adds a `Source`, and calls `publicizedAssemblies.InspectSource` to harvest publicizing requests.
  - `Compile(string assemblyName, out byte[] symbols)` — builds the reference list by mapping every `RoslynReferences.Instance.AllReferences` entry through `PublicizeReferenceIfRequired` and appending `customReferences`; creates a `CSharpCompilation` (`OutputKind.DynamicallyLinkedLibrary`, Debug/Release optimization, `allowUnsafe: true`) over all source syntax trees; emits to a `MemoryStream`. For debug builds, emits a `PortablePdb` with embedded source texts and returns the PDB via `symbols`. On failure, aggregates error/warning-as-error `Diagnostic`s — each mapped back to its source file name and line/column — into an `AggregateException("Compilation failed!", ...)`. On success returns the DLL byte array.
  - `TryAddDependency(string dll)` — if `dll` has a `.dll` extension and exists on disk, creates a `MetadataReference.CreateFromFile`, logs it, and adds to `customReferences`; swallows exceptions.

### Source — class, private (nested in `RoslynCompiler`)
Holds one parsed source file and, when embedding is requested, its `EmbeddedText` for source-included debugging.
- **Properties:** `Name` — the logical source name used in diagnostics; `Tree` — the parsed `SyntaxTree`; `Text` — the `EmbeddedText` (null unless `embedFile` was supplied).
- **Methods:** `Source(Stream s, string name, CSharpParseOptions options, string embedFile = null)` — reads `SourceText` (with `canBeEmbedded` when `embedFile` is set), and either parses with the embed path and stores `EmbeddedText.FromSource`, or parses plainly.

## Cross-references
- **Uses:** `Compiler/RoslynReferences.cs` (`Instance.AllReferences`); `Compiler/PublicizedAssemblies.cs`; `Compiler/LogFile.cs`; Roslyn (`Microsoft.CodeAnalysis`, `.CSharp`, `.Emit`, `.Text` — `CSharpCompilation`, `EmitOptions`, `EmbeddedText`, `Diagnostic`).
- **Used by:** [Interim.cs](../Legacy/Compiler/Interim.cs.md), [Legacy.cs](../Legacy/Compiler/Legacy.cs.md), [GitHubPlugin.cs](../Shared/Data/GitHubPlugin.cs.md), [LocalFolderPlugin.cs](../Shared/Data/LocalFolderPlugin.cs.md), [ObsoletePlugin.cs](../Shared/Data/ObsoletePlugin.cs.md), [PluginData.cs](../Shared/Data/PluginData.cs.md), [PluginList.cs](../Shared/PluginList.cs.md), [Tools.cs](../Shared/Tools.cs.md)
