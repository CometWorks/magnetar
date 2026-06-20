# Compiler/RoslynReferences.cs

**Project:** Compiler · **Namespace:** `Pulsar.Compiler` · **Kind:** class · **Lines:** 84

## Summary
Builds and caches the global set of Roslyn `MetadataReference`s that plugins are compiled against — essentially the SE Dedicated Server / VRage / framework assembly closure. Starting from a seed list of assembly names, it uses Mono.Cecil's `DefaultAssemblyResolver` to locate each assembly on disk and recursively walks its `AssemblyReferences` to pull in transitive dependencies. It is a process-wide singleton (`Instance`) whose `Resolver` is also reused by `Publicizer` so publicizing reads from the same assembly search paths. `RoslynCompiler.Compile` consumes `AllReferences` directly.

## Types
### RoslynReferences — class, public
Singleton holding the resolved reference dictionary and the shared Cecil resolver. Resolution is one-shot: once `AllReferences` is populated it is not regenerated.
- **Fields:** `Instance` — static singleton; `Resolver` — public `DefaultAssemblyResolver` (Mono.Cecil) used both here and by `Publicizer` to read assemblies; `AllReferences` — internal `Dictionary<string, MetadataReference>` keyed by assembly name, the complete reference set exposed to the compiler.
- **Methods:**
  - `GenerateAssemblyList(IReadOnlyCollection<string> assemblies)` — public seed entry; no-ops if `AllReferences` is already populated, otherwise logs the reference list and calls `LoadAssemblies`.
  - `LoadAssemblies(IEnumerable<string> names, bool recuse = true)` — private; depth-first walk using a `Stack<string>` and a `missing` set; for each name not already loaded, tries to resolve it, stores the resulting `MetadataReference`, and (when recursing) pushes its dependency names; logs any skipped/unresolved names at the end.
  - `TryLoadAssembly(string name, out MetadataReference reference, out IEnumerable<string> dependencies)` — private; resolves `name` via `Resolver.Resolve(new AssemblyNameReference(name, null))`, returning `false` on `IOException` (assembly not found); on success creates `MetadataReference.CreateFromFile` from the module's on-disk path and yields the dependency assembly names from `MainModule.AssemblyReferences`.

## Cross-references
- **Uses:** Mono.Cecil (`DefaultAssemblyResolver`, `AssemblyDefinition`, `AssemblyNameReference`); Roslyn (`MetadataReference`); `Compiler/LogFile.cs`; SE DS / VRage / .NET framework assemblies (the resolution targets).
- **Used by:** [Publicizer.cs](Publicizer.cs.md), [RoslynCompiler.cs](RoslynCompiler.cs.md), [Interim.cs](../Legacy/Compiler/Interim.cs.md), [Legacy.cs](../Legacy/Compiler/Legacy.cs.md)
