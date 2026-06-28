# Module: Compiler

**Project:** `Compiler` · **Files:** 5 · **Source lines:** 562

## Purpose

In-process C# compiler that builds Space Engineers plugins from source at server startup using Roslyn. It resolves the SE DS / VRage / framework assembly closure as references, optionally publicizes targeted assemblies (forcing internals public via Mono.Cecil) so plugins can access non-public SE members declared through IgnoresAccessChecksTo, and emits a plugin DLL (with optional embedded-source PDB).

## Role in Magnetar

Provides the compilation backend for Magnetar's plugin loader. The Legacy/Interim loaders feed plugin sources and extra dependencies into RoslynCompiler (driven across an AppDomain via MarshalByRefObject through the ICompilerFactory/ICompiler abstraction), which produces the assembly bytes the loader then loads and instantiates. It is environment-agnostic (netstandard2.0) so the same compiler runs under both the .NET Framework 4.8 Legacy host and the .NET 10 Interim host.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `RoslynCompiler` | class | [`Compiler/RoslynCompiler.cs`](../descriptions/Compiler/RoslynCompiler.cs.md) | MarshalByRefObject ICompiler that accumulates sources and emits a plugin DLL/PDB via Roslyn CSharpCompilation. |
| `ICompiler` | interface | [`Compiler/RoslynCompiler.cs`](../descriptions/Compiler/RoslynCompiler.cs.md) | Contract for a single compilation unit: Load sources, Compile to IL, add dependencies. |
| `ICompilerFactory` | interface | [`Compiler/RoslynCompiler.cs`](../descriptions/Compiler/RoslynCompiler.cs.md) | Factory abstraction (IDisposable) that initializes the compile environment and creates compilers. |
| `RoslynReferences` | class | [`Compiler/RoslynReferences.cs`](../descriptions/Compiler/RoslynReferences.cs.md) | Process-wide singleton that resolves and caches the recursive SE DS assembly reference closure via Mono.Cecil. |
| `PublicizedAssemblies` | class | [`Compiler/PublicizedAssemblies.cs`](../descriptions/Compiler/PublicizedAssemblies.cs.md) | Scans source for IgnoresAccessChecksTo attributes and substitutes publicized MetadataReferences at compile time. |
| `Publicizer` | static class | [`Compiler/Publicizer.cs`](../descriptions/Compiler/Publicizer.cs.md) | Mono.Cecil IL rewriter that forces non-public types/fields/methods/properties public and re-emits an in-memory reference. |
| `LogFile` | static class | [`Compiler/LogFile.cs`](../descriptions/Compiler/LogFile.cs.md) | Self-contained NLog file logger (info.log) used across the Compiler module, swallowing all logging exceptions. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`Compiler/LogFile.cs`](../descriptions/Compiler/LogFile.cs.md) | 122 | Minimal NLog-backed file logger used by the Compiler module to record Roslyn reference loading, publicizing, and compilation diagnostics to the active Magnetar `info_*.log` file. |
| [`Compiler/PublicizedAssemblies.cs`](../descriptions/Compiler/PublicizedAssemblies.cs.md) | 77 | Bridges Roslyn source analysis with assembly publicizing. |
| [`Compiler/Publicizer.cs`](../descriptions/Compiler/Publicizer.cs.md) | 151 | Performs the actual IL-level publicizing of an SE DS assembly using Mono.Cecil: it reads the assembly from disk, forces every non-public type, field, method, and property to public, and re-emits it to an in-memory `MetadataReference` for Roslyn. |
| [`Compiler/RoslynCompiler.cs`](../descriptions/Compiler/RoslynCompiler.cs.md) | 171 | The core in-process C# compiler used to build local/Workshop plugins from source at server startup. |
| [`Compiler/RoslynReferences.cs`](../descriptions/Compiler/RoslynReferences.cs.md) | 84 | Builds and caches the global set of Roslyn `MetadataReference`s that plugins are compiled against — essentially the SE Dedicated Server / VRage / framework assembly closure. |

## Public API surface

- `RoslynCompiler.Load(Stream, string, string)`
- `RoslynCompiler.Compile(string, out byte[])`
- `RoslynCompiler.TryAddDependency(string)`
- `ICompilerFactory.Init() / Create(bool)`
- `RoslynReferences.Instance`
- `RoslynReferences.GenerateAssemblyList(IReadOnlyCollection<string>)`
- `PublicizedAssemblies.InspectSource(SourceText) / PublicizeReferenceIfRequired(...)`
- `Publicizer.PublicizeReference(PortableExecutableReference)`
- `LogFile.Init(string) / WriteLine / Error / Warn / Dispose`

## Dependencies

**Uses modules:** _none_  
**Used by modules:** [Legacy.Integration](Legacy.Integration.md), [Shared.Core](Shared.Core.md), [Shared.Data](Shared.Data.md)  
**External systems:** Harmony; Mono.Cecil; NLog; Roslyn (Microsoft.CodeAnalysis.CSharp); SE DS assemblies

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
