# Legacy/Compiler/Interim.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Compiler` · **Kind:** classes (file-local + internal) · **Lines:** 147

## Summary
Active only under `#if NETCOREAPP` (the Interim/.NET 10 build). Provides the `CompilerFactory` that instantiates Roslyn-based script compilers for the Interim runtime. Because the Compiler assembly (`Pulsar.Compiler`) is itself a .NET Core library and SE mods may pull in conflicting versions of framework assemblies, the factory loads the compiler into an isolated, collectible `AssemblyLoadContext` (`CompilerLoadContext`). All cross-context calls are then forwarded through `CompilerWrapper`, a reflection-based adapter that implements `ICompiler` without requiring a shared type identity between load contexts.

## Types

### `CompilerWrapper` — class, file-local : `ICompiler`
Bridges the type-identity gap between the host's `AssemblyLoadContext` and the isolated compiler context. Holds a reference to a `RoslynCompiler` instance that was created inside the isolated context and delegates every `ICompiler` call to it via `MethodInfo.Invoke`/field-set. `TargetInvocationException` wrappers are stripped so callers see the original exception.

- **Fields:** `instance — the RoslynCompiler object living inside the compiler load context`; `access — BindingFlags constant for public instance reflection`
- **Methods:** `CompilerWrapper(compiler, debugBuild, flags) — constructs the wrapper, sets DebugBuild and Flags fields on the remote instance via SetField`; `Compile(assemblyName, out symbols) — invokes RoslynCompiler.Compile via reflection using an object[] args array so the out-parameter byte[] is captured as args[1]`; `Load(s, name, embedFile) — forwards to RoslynCompiler.Load`; `TryAddDependency(dll) — forwards to RoslynCompiler.TryAddDependency`; `SetField(name, value) — reflects on the remote instance type to set a public instance field`; `RunMethod(name, args) — finds and invokes a public instance method, unwrapping TargetInvocationException`

### `CompilerLoadContext` — sealed class, file-local : `AssemblyLoadContext`
A collectible `AssemblyLoadContext` named `"Magnetar"` that resolves assemblies from `<applicationBase>/Libraries/MagnetarInterim/Compiler/`. Its `Load` override looks up `assemblyName.Name + ".dll"` in that directory and loads it by path; if not found, returns `null` to fall back to the default context. This keeps the Roslyn and compiler-support DLLs out of the main load context.

- **Fields:** `binPath — absolute path to the compiler's private binary directory`
- **Methods:** `CompilerLoadContext() — sets binPath to <exe dir>/Libraries/MagnetarInterim/Compiler`; `Load(assemblyName) — probes binPath for the named dll and loads it, or returns null`

### `CompilerFactory` — class, internal : `ICompilerFactory`
The public factory consumed by the Legacy launcher. Manages the lifecycle of the isolated `CompilerLoadContext` and the `compilerAsm` loaded into it. `Init()` creates the context, loads the compiler assembly, configures `LogFile` and `RoslynReferences` inside the isolated context, and registers probe directories and game-assembly references. `Create()` instantiates a new `RoslynCompiler` inside the context and wraps it in a `CompilerWrapper`, injecting the appropriate preprocessor flags (`NETCOREAPP`, `PLATFORM_WINDOWS` or `PLATFORM_LINUX`, `TRACE`, optionally `DEBUG`). `Dispose()` discards the compiler assembly reference and unloads the `AssemblyLoadContext`.

- **Fields:** `compilerAsm — the Pulsar.Compiler assembly loaded into the isolated context`; `loadContext — the CompilerLoadContext instance`
- **Methods:** `Init() — creates the load context and calls SetupLoadContext with the game assembly list`; `Create(debugBuild) — lazily calls Init if needed; determines platform flag; reflects into the compiler context to create a RoslynCompiler, returns a CompilerWrapper`; `SetupLoadContext(assemblies) — via reflection: calls LogFile.Init in the isolated context, adds the .NET runtime dir and probeDirs to the RoslynReferences resolver, then calls GenerateAssemblyList`; `CreateLoadContext() — constructs CompilerLoadContext and loads Pulsar.Compiler.dll into it from its original on-disk location`; `Dispose() — nulls compilerAsm and calls AssemblyLoadContext.Unload`

## Cross-references
- **Uses:** `Compiler/RoslynCompiler.cs` (target type, instantiated reflectively); `Legacy/Compiler/References.cs` (game assembly enumeration); `Compiler/LogFile.cs` (Compiler-side log initialisation, called reflectively); `Compiler/RoslynReferences.cs` (assembly reference setup, called reflectively); `PluginSdk.Paths` — no; `Pulsar.Compiler` — `ICompiler`, `ICompilerFactory` interfaces
- **Used by:** [Legacy.cs](Legacy.cs.md), [Program.cs](../Program.cs.md)
