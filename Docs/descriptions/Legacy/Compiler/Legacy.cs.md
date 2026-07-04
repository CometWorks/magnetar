# Legacy/Compiler/Legacy.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Compiler` · **Kind:** class (internal) · **Lines:** 86

## Summary
Active only under `#if NETFRAMEWORK` (the .NET Framework 4.8 / Windows build). Provides the `CompilerFactory` that instantiates Roslyn-based script compilers for the Legacy runtime. Because .NET Framework's `AssemblyLoadContext` does not exist, isolation is achieved via a separate `AppDomain` named `"Pulsar.Compiler"`. The app domain's `PrivateBinPath` is `Libraries\MagnetarLegacy\Compiler` (Roslyn-private dependencies, relative to the `ApplicationBase` executable directory) and its `ConfigurationFile` is `Libraries\MagnetarLegacy\Magnetar.Compiler.dll.config`; it then uses `MarshalByRefObject` cross-domain remoting so `RoslynCompiler` is returned directly without a wrapper proxy.

## Types

### `CompilerFactory` — class, internal : `ICompilerFactory`
The factory consumed by the Legacy launcher on .NET Framework. Manages the lifecycle of a dedicated `AppDomain` that hosts the Roslyn compiler. `Init()` creates the domain with a private bin path of `Libraries\MagnetarLegacy\Compiler`, passes probe dirs, log dir, and game-assembly references through `AppDomain.SetData`, then calls `SetupAppDomain` as a cross-domain callback to initialise `LogFile` and `RoslynReferences` inside the new domain. `Create()` uses `AppDomain.CreateInstanceAndUnwrap` to obtain a transparent proxy to a `RoslynCompiler` (valid because `RoslynCompiler : MarshalByRefObject`), sets `DebugBuild` and `Flags` (`NETFRAMEWORK`, `PLATFORM_WINDOWS`, `TRACE`, optionally `DEBUG`) on the proxy, and returns it as `ICompiler`. `Dispose()` calls `AppDomain.Unload` to reclaim the domain and its assemblies.

- **Fields:** `appDomain — the isolated AppDomain that hosts the compiler`
- **Methods:** `Init() — calls References.GetReferences to gather game DLLs, then calls CreateAppDomain`; `Create(debugBuild) — lazily calls Init if needed; creates a RoslynCompiler transparent proxy via CreateInstanceAndUnwrap, sets Flags and DebugBuild, returns the proxy as ICompiler`; `SetupAppDomain() — static, runs inside the child AppDomain: reads data items ("assemblies", "probeDirs", "logDir") from AppDomain.CurrentDomain, calls LogFile.Init, configures RoslynReferences probe dirs, calls GenerateAssemblyList`; `CreateAppDomain(assemblies, probeDirs, logDir) — creates AppDomainSetup (ApplicationBase = exe dir, PrivateBinPath = Libraries\MagnetarLegacy\Compiler, ConfigurationFile = Libraries\MagnetarLegacy\Magnetar.Compiler.dll.config), creates the domain, transfers data via SetData, executes SetupAppDomain callback, returns the domain`; `Dispose() — calls AppDomain.Unload on the compiler domain`

## Cross-references
- **Uses:** `Compiler/RoslynCompiler.cs` (target type, instantiated as transparent proxy); `Legacy/Compiler/References.cs` (game assembly enumeration); `Compiler/LogFile.cs` (LogFile.Init called in child AppDomain); `Compiler/RoslynReferences.cs` (RoslynReferences.Instance setup in child AppDomain); `Pulsar.Compiler` — `ICompiler`, `ICompilerFactory` interfaces
- **Used by:** [Interim.cs](Interim.cs.md), [Program.cs](../Program.cs.md)
