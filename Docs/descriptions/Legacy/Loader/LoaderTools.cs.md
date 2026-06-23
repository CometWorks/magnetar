# Legacy/Loader/LoaderTools.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Loader` · **Kind:** static class · **Lines:** 137

## Summary
Process-level utilities for the loader: restarting the dedicated server process with adjusted command-line arguments, and force-precompiling (JIT-preparing) plugin assemblies so member-access errors surface immediately instead of mid-game. The restart logic is cross-platform — on Linux/.NET it uses the `libc` `execv` syscall to replace the process image (preserving PID, stdio and tty for systemd/tmux supervision), and on Windows it spawns a fresh `Process`. The precompile logic walks every method of every type via reflection and calls `RuntimeHelpers.PrepareMethod`, while rejecting Harmony `[HarmonyReversePatch]` methods that are incompatible with Pulsar.

## Types

### LoaderTools — static class, public
Stateless helper collection for restarting the server and eagerly JIT-compiling assemblies. Exists so the loader can recover/reload by relaunching itself and can fail fast on broken plugin code.

- **Fields:**
  - `ContinueArg` (const `"-continue"`) — CLI flag added on auto-rejoin restart so the relaunched server resumes/reconnects.
  - `DebugArg` (const `"-debug"`) — CLI flag added when a debugger should be (re)attached.
  - `execv` (`extern int(string path, string[] argv)`, `[DllImport("libc", SetLastError = true)]`, `NETCOREAPP` only) — POSIX `execv` to replace the current process image.
- **Methods:**
  - `Restart(bool autoRejoin = false, bool? debugger = null)` — Starts a replacement process via `Start`, defaulting `debugger` to `Debugger.IsAttached`, then `Kill`s the current process. On Linux the `Kill` is only reached if `execv` failed (otherwise the image is already replaced).
  - `Start(bool autoRejoin, bool debugger)` (private) — Rebuilds the argument list from `Environment.GetCommandLineArgs()` (skipping argv[0]), toggling `ContinueArg`/`DebugArg` per parameters. On Linux builds a null-terminated `argv` (argv[0] = program name by convention) and calls `execv`; logs `Marshal.GetLastWin32Error()` on failure. Otherwise builds a quoted argument string and `Process.Start`s `MainModule.FileName`.
  - `Precompile(Assembly a)` — Enumerates `a.GetTypes()` (logging `ReflectionTypeLoadException.LoaderExceptions` and rethrowing), skips types with a static constructor (avoids triggering early type-initializer side effects), throws if any method carries `[HarmonyReversePatch]`, and calls `Precompile(MethodInfo)` on every declared method (all visibilities, instance + static).
  - `Precompile(MethodInfo m)` (private) — Calls `RuntimeHelpers.PrepareMethod(m.MethodHandle)` unless the method is abstract or has open generic parameters.
  - `HasStaticConstructor(Type t)` (private) — Returns whether any constructor on the type is static.

## Cross-references
- **Uses:** `Pulsar.Shared` (`LogFile`); `HarmonyLib` (`HasAttribute`, `HarmonyReversePatch`); BCL `System.Diagnostics.Process`, `System.Reflection`, `System.Runtime.CompilerServices.RuntimeHelpers`, `System.Runtime.InteropServices` (`libc`/`execv`).
- **Used by:** _none within the repository_
