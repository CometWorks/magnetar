# Legacy/Launcher/ServerControl.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Launcher` · **Kind:** static class · **Lines:** 525

## Summary
Single source of truth for the dedicated server's lifecycle operations — save world, reload dedicated config, quit, and restart — with and without saving. It backs both the POSIX signal handlers (SIGTERM/SIGINT → save+quit, SIGHUP → reload) on the .NET (Core)/Interim build and the plugin-facing `PluginSdk.ServerControl` facade. All world access is marshalled onto SE's update thread and every operation null-guards `MySandboxGame.Static`/`MySession.Static` so plugins may call from any thread; save completion is detected from Keen's `MySession.IsSaveInProgress` and `MyAsyncSaving.InProgress` flags, with update-thread waits pumping `ParallelTasks` callbacks so the async save can clear its own state. Restart faithfully reproduces the original launch — via `execve` on Linux (preserving PID/stdio/tty under systemd/tmux) and `Process.Start` on Windows.

## Types
### ServerControl — static class, internal
Lifecycle coordinator. Captures launch state at the top of `Main`, installs signal handlers and the SDK facade, performs thread-safe saves with timeouts, and terminates/restarts the process deterministically. A single-shot `terminating` latch guards against re-entrant shutdown from concurrent signals and commands.

- **Fields:**
  - `SaveTimeout` (5 min), `DisposeTimeout` (30 s) — bounds for save completion and plugin disposal.
  - `originalArgv`, `originalCwd`, `originalEnv` — snapshot of the launch (argv[0] is the executable), captured before the launcher mutates cwd/env, used to reproduce the process on restart.
  - `sigTerm`, `sigInt`, `sigHup` (`#if NETCOREAPP`) — rooted `PosixSignalRegistration`s; kept as static fields so the GC does not finalize them (a finalized registration silently stops delivering).
  - `terminating` (volatile bool), `terminateLock` — single-shot shutdown latch.
- **P/Invoke:**
  - `LibcExit(int)` — `libc _exit`, guarantees the exact exit code without running finalizers.
  - `execve(path, argv, envp)` — `libc execve` for in-place image replacement on restart.
- **Methods:**
  - `CaptureLaunchState(argv, cwd, env)` — stores launch argv/cwd/env (env flattened to `KEY=VALUE`). Call once at the top of `Main` before any cwd/env mutation.
  - `InstallSignalHandlers()` — binds the SDK `PluginSdk.ServerControl` facade to the six lifecycle delegates; on .NET (Core) registers SIGTERM/SIGINT → `OnTerminate` and (Linux only) SIGHUP → `OnReload`. Safe before a session exists.
  - `OnTerminate(ctx)` / `OnReload(ctx)` (`#if NETCOREAPP`) — set `ctx.Cancel = true` to suppress default termination, log, and hand off to `Task.Run(SaveAndQuit)` / `Task.Run(ReloadConfig)` to keep the signal-dispatch thread fast.
  - `SaveWorld(timeout = null)` — saves and blocks until the on-disk write finishes (or times out); returns false with no session or if starting the save throws. Runs the snapshot inline if already on the update thread (marshalling to it from itself would deadlock), otherwise via `Game.RunOnGameThread`. Skips starting a second save if either `MySession.IsSaveInProgress` or `MyAsyncSaving.InProgress` is already true, then waits for both flags to clear instead of trusting `MyAsyncSaving.Start`'s early callback. Uses a `ManualResetEventSlim` started gate and a `Stopwatch` budget.
  - `WaitForSaveIdle(stopwatch, limit)` — polls the save-in-progress flags until the remaining timeout expires; when called on the update thread, runs `ParallelTasks.Parallel.RunCallbacks()` each pass so Keen's async snapshot-save completion callback can execute and clear `MyAsyncSaving.InProgress`.
  - `IsSaveInProgress()` — true when the session reports snapshot saving or the async snapshot writer reports in-progress.
  - `ReloadConfig()` — saves, then reloads `MySandboxGame.ConfigDedicated` and pushes the browser server name via `MyGameService.GameServer.SetServerName` (MOTD and admin/ban lists are read live, so `Load()` suffices for them). Does not quit. Runs on the update thread (inline or marshalled). Returns false with no game.
  - `SaveAndQuit()` — single-shot; deliberately does NOT raise `Terminating` (backs SIGTERM/SIGINT and SE's internal exit, not an admin stop intent); saves, disposes plugins, flushes, exits 0.
  - `QuitWithoutSaving()` — single-shot; raises `Terminating(Shutdown)`, disposes, flushes, exits 0. (Backs the in-game `!quit`/`!stop` command.)
  - `SaveAndRestart()` — single-shot; raises `Terminating(Restart)`, saves, disposes, flushes, restarts.
  - `RestartWithoutSaving()` — single-shot; raises `Terminating(Restart)`, disposes, flushes, restarts (no save).
  - `BeginTerminate()` — locked test-and-set of `terminating`; returns false if a termination is already underway, making all the lifecycle terminators idempotent.
  - `DisposePlugins()` — disposes `PluginLoader.Instance` on a background task bounded by `DisposeTimeout` so a hung plugin can't block shutdown.
  - `FlushAll()` — first removes the pid file (`PidFile.Delete()`, so a reader polling during shutdown sees it disappear promptly; a crash skips this path and leaves a stale file the reader detects by probing the now-dead pid), then best-effort flushes `Console.Out`/`Console.Error`, `MyLog.Default`, and `LogFile`. Every step is wrapped in its own try/catch.
  - `ExitProcess(code)` — on Linux calls `LibcExit` first (exact code, no finalizer hang), then `Environment.Exit`.
  - `RestartProcess()` — reproduces the launch. On Linux restores `originalCwd` and `execve`s the original image with null-terminated argv/envp (logs `GetLastWin32Error` only if `execve` returns). On Windows builds a `ProcessStartInfo` from the captured argv/cwd/env (using `ArgumentList` on Core, the legacy `Arguments` string with `PasteArguments` quoting on Framework), starts it, and exits 0. Errors if launch state wasn't captured.
  - `OnUpdateThread()` — true if the current managed thread id equals `MyPrecalcComponent.UpdateThreadManagedId` (and that id is initialized).
  - `IsLinux` — `OperatingSystem.IsLinux()` on Core; constant `false` on the Framework/Legacy build.
  - `PasteArguments(argv)` / `AppendArgument(sb, arg)` (`#if !NETCOREAPP`) — Windows command-line quoting reproduction for .NET Framework (which lacks `ProcessStartInfo.ArgumentList`), skipping argv[0]; mirrors CoreFX's backslash/quote escaping rules.

## Cross-references
- **Uses:** SE DS — `MySandboxGame`/`MySession`/`MyAsyncSaving`/`MyGameService`/`MyPrecalcComponent` (`Sandbox`, `Sandbox.Engine.Networking`, `Sandbox.Game.World`), `ParallelTasks.Parallel`, `VRage.Utils.MyLog`; `Pulsar.Legacy.Loader.PluginLoader` (`Legacy/Loader/`); `Pulsar.Shared` (`LogFile`); `Pulsar.Legacy.Launcher.Game` (`Game.RunOnGameThread`, this folder); `PidFile` (this folder); `PluginSdk.ServerControl` / `ServerTerminationKind` (`PluginSdk/`); `libc` via P/Invoke; `System.Runtime.InteropServices.PosixSignalRegistration`.
- **Used by:** _none within the repository_
