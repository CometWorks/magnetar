using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pulsar.Legacy.Loader;
using Pulsar.Shared;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using VRage.Utils;
using VRage.Voxels;

namespace Pulsar.Legacy.Launcher;

/// <summary>
/// Single source of truth for the dedicated server's lifecycle operations —
/// saving the world, reloading the dedicated config, quitting and restarting —
/// backing both the POSIX signal handlers (SIGTERM/SIGINT → save+quit,
/// SIGHUP → reload) and the plugin-facing <see cref="PluginSdk.ServerControl"/>
/// facade.
///
/// <para>
/// World access is marshalled to the game's update thread (saving touches live
/// entities) and every operation null-guards <see cref="MySandboxGame.Static"/>
/// / <see cref="MySession.Static"/>, so plugins may call from any thread. The
/// disk write itself runs on a background task. Completion is detected from
/// Keen's save-in-progress flags rather than the early async-save callback; if
/// the caller is already on the update thread, the wait loop pumps
/// <c>ParallelTasks</c> callbacks so Keen's async-save completion can clear.
/// </para>
/// </summary>
internal static class ServerControl
{
    private static readonly TimeSpan SaveTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DisposeTimeout = TimeSpan.FromSeconds(30);

    // Faithful reproduction of the original launch, captured at the very top of
    // Main before the launcher mutates the working directory and environment.
    private static string[] originalArgv = Array.Empty<string>();
    private static string originalCwd;
    private static string[] originalEnv = Array.Empty<string>();

#if NETCOREAPP
    // Rooted so the CLR does not finalize the registrations (a finalized
    // registration silently stops delivering the signal).
    private static PosixSignalRegistration sigTerm;
    private static PosixSignalRegistration sigInt;
    private static PosixSignalRegistration sigHup;
#endif

    private static volatile bool terminating;
    private static readonly object terminateLock = new object();

    [DllImport("libc", EntryPoint = "_exit")]
    private static extern void LibcExit(int status);

    [DllImport("libc", SetLastError = true)]
    private static extern int execve(string path, string[] argv, string[] envp);

    /// <summary>
    /// Records the original command line, working directory and environment so
    /// a restart can reproduce the launch exactly. Call once at the top of
    /// <c>Main</c>, before any cwd/env mutation.
    /// </summary>
    public static void CaptureLaunchState(string[] argv, string cwd, IDictionary env)
    {
        originalArgv = argv ?? Array.Empty<string>();
        originalCwd = cwd;

        var list = new List<string>();
        if (env != null)
            foreach (DictionaryEntry e in env)
                list.Add($"{e.Key}={e.Value}");
        originalEnv = list.ToArray();
    }

    /// <summary>
    /// Binds the plugin SDK facade and, on .NET (Core), installs the POSIX
    /// signal handlers. Safe to call before a session exists — the handlers
    /// tolerate a null <see cref="MySession.Static"/>. The .NET Framework
    /// (Legacy) build has no POSIX signals, so it only binds the facade.
    /// </summary>
    public static void InstallSignalHandlers()
    {
        PluginSdk.ServerControl.Bind(
            () => SaveWorld(),
            ReloadConfig,
            SaveAndQuit,
            SaveAndRestart,
            QuitWithoutSaving,
            RestartWithoutSaving);

#if NETCOREAPP
        sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnTerminate);
        sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnTerminate);

        // SIGHUP is Linux-only; on Windows Create() throws for it.
        if (OperatingSystem.IsLinux())
            sigHup = PosixSignalRegistration.Create(PosixSignal.SIGHUP, OnReload);
#endif
    }

#if NETCOREAPP
    private static void OnTerminate(PosixSignalContext context)
    {
        // Cancel the default disposition (process termination) so the save can
        // finish, and keep the signal-dispatch thread fast by handing off.
        context.Cancel = true;
        LogFile.WriteLine($"Received {context.Signal}: saving world and quitting");
        Task.Run(SaveAndQuit);
    }

    private static void OnReload(PosixSignalContext context)
    {
        context.Cancel = true;
        LogFile.WriteLine("Received SIGHUP: saving world and reloading config");
        Task.Run(() => ReloadConfig());
    }
#endif

    /// <summary>
    /// Saves the world and blocks the calling thread until the on-disk write
    /// completes (or <see cref="SaveTimeout"/> elapses). Returns <c>false</c>
    /// when no session is loaded.
    /// </summary>
    public static bool SaveWorld(TimeSpan? timeout = null)
    {
        if (MySandboxGame.Static == null || MySession.Static == null)
            return false;

        TimeSpan limit = timeout ?? SaveTimeout;
        var started = new ManualResetEventSlim(false);
        bool startedOwnSave = false;
        bool startFailed = false;

        void StartSave()
        {
            try
            {
                // A save is already running (e.g. an autosave): don't start a
                // second one, just wait for the server to become save-idle.
                if (IsSaveInProgress())
                    return;

                startedOwnSave = true;
                MyAsyncSaving.Start();
            }
            catch (Exception e)
            {
                startFailed = true;
                LogFile.Error($"Failed to start world save: {e}");
            }
            finally
            {
                started.Set();
            }
        }

        // Marshalling to the update thread from the update thread would
        // deadlock (the queued action never runs). Detect that and run the
        // snapshot inline instead; the disk write is still a background task.
        if (OnUpdateThread())
            StartSave();
        else
            Game.RunOnGameThread(StartSave);

        var stopwatch = Stopwatch.StartNew();
        if (!started.Wait(limit))
        {
            LogFile.Warn("World save did not start within the timeout");
            return false;
        }

        if (startFailed)
            return false;

        if (WaitForSaveIdle(stopwatch, limit))
            return true;

        LogFile.Warn(startedOwnSave
            ? "World save did not finish within the timeout"
            : "In-progress world save did not finish within the timeout");
        return false;
    }

    private static bool WaitForSaveIdle(Stopwatch stopwatch, TimeSpan limit)
    {
        bool pumpCallbacks = OnUpdateThread();

        while (stopwatch.Elapsed < limit)
        {
            if (pumpCallbacks)
                ParallelTasks.Parallel.RunCallbacks();

            if (!IsSaveInProgress())
                return true;
            Thread.Sleep(50);
        }

        if (pumpCallbacks)
            ParallelTasks.Parallel.RunCallbacks();

        return !IsSaveInProgress();
    }

    private static bool IsSaveInProgress()
    {
        MySession session = MySession.Static;
        return (session != null && session.IsSaveInProgress) || MyAsyncSaving.InProgress;
    }

    /// <summary>
    /// Saves the world, then re-reads the dedicated config and applies the
    /// runtime-safe settings (MOTD, admin/ban lists, browser server name). Does
    /// not quit. Returns <c>false</c> when there is no game to apply it to.
    /// </summary>
    public static bool ReloadConfig()
    {
        if (MySandboxGame.Static == null)
            return false;

        SaveWorld();

        bool ok = false;

        void DoReload()
        {
            try
            {
                MySandboxGame.ConfigDedicated.Load();

                // The MOTD is recomputed per join, and admin/ban lists are read
                // live for new connections, so Load() is enough. The browser
                // server name is the only value that needs an explicit push.
                try
                {
                    MyGameService.GameServer?.SetServerName(MySandboxGame.ConfigDedicated.ServerName);
                }
                catch (Exception e)
                {
                    LogFile.Warn($"Could not update live server name: {e.Message}");
                }

                ok = true;
                LogFile.WriteLine("Dedicated config reloaded");
            }
            catch (Exception e)
            {
                LogFile.Error($"Failed to reload dedicated config: {e}");
            }
        }

        if (OnUpdateThread())
        {
            DoReload();
        }
        else
        {
            var done = new ManualResetEventSlim(false);
            Game.RunOnGameThread(() =>
            {
                DoReload();
                done.Set();
            });
            done.Wait(SaveTimeout);
        }

        return ok;
    }

    /// <summary>Saves the world, then quits with exit code 0.</summary>
    public static void SaveAndQuit()
    {
        if (!BeginTerminate())
            return;

        // Deliberately does NOT raise Terminating: this backs SIGTERM/SIGINT and
        // SE's internal ExitThreadSafe path, neither of which is an admin stop
        // intent. Only the in-game !quit/!stop command (QuitWithoutSaving) does.
        LogFile.WriteLine("Saving world before shutdown");
        SaveWorld();
        DisposePlugins();
        FlushAll();
        ExitProcess(0);
    }

    /// <summary>Quits immediately with exit code 0, without saving.</summary>
    public static void QuitWithoutSaving()
    {
        if (!BeginTerminate())
            return;

        PluginSdk.ServerControl.RaiseTerminating(PluginSdk.ServerTerminationKind.Shutdown);
        LogFile.WriteLine("Quitting without saving");
        DisposePlugins();
        FlushAll();
        ExitProcess(0);
    }

    /// <summary>Saves the world, then restarts with the original launch state.</summary>
    public static void SaveAndRestart()
    {
        if (!BeginTerminate())
            return;

        PluginSdk.ServerControl.RaiseTerminating(PluginSdk.ServerTerminationKind.Restart);
        LogFile.WriteLine("Saving world before restart");
        SaveWorld();
        DisposePlugins();
        FlushAll();
        RestartProcess();
    }

    /// <summary>Restarts immediately with the original launch state, no save.</summary>
    public static void RestartWithoutSaving()
    {
        if (!BeginTerminate())
            return;

        PluginSdk.ServerControl.RaiseTerminating(PluginSdk.ServerTerminationKind.Restart);
        LogFile.WriteLine("Restarting without saving");
        DisposePlugins();
        FlushAll();
        RestartProcess();
    }

    private static bool BeginTerminate()
    {
        lock (terminateLock)
        {
            if (terminating)
                return false;
            terminating = true;
            return true;
        }
    }

    private static void DisposePlugins()
    {
        try
        {
            // Bound the wait so a misbehaving plugin cannot hang shutdown.
            Task dispose = Task.Run(() => PluginLoader.Instance?.Dispose());
            if (!dispose.Wait(DisposeTimeout))
                LogFile.Warn("Plugin disposal timed out");
        }
        catch (Exception e)
        {
            LogFile.Error($"Error disposing plugins: {e}");
        }
    }

    private static void FlushAll()
    {
        try { Console.Out.Flush(); } catch { }
        try { Console.Error.Flush(); } catch { }
        try { MyLog.Default?.Flush(); } catch { }
        try { LogFile.Dispose(); } catch { }
    }

    private static void ExitProcess(int code)
    {
        // libc _exit guarantees the exact exit code and cannot hang in a
        // finalizer or static destructor, unlike Environment.Exit.
        if (IsLinux)
        {
            try { LibcExit(code); } catch { }
        }

        Environment.Exit(code);
    }

    private static void RestartProcess()
    {
        if (originalArgv.Length == 0)
        {
            LogFile.Error("Cannot restart: original launch state was not captured");
            ExitProcess(1);
            return;
        }

        string path = originalArgv[0];

        if (IsLinux)
        {
            // Restore the working directory the process was launched from, then
            // replace the image. execve preserves the PID, stdio and tty, which
            // matters under systemd / tmux supervision. argv and envp must be
            // null-terminated for libc.
            if (!string.IsNullOrEmpty(originalCwd))
            {
                try { Environment.CurrentDirectory = originalCwd; } catch { }
            }

            string[] argv = new string[originalArgv.Length + 1];
            Array.Copy(originalArgv, argv, originalArgv.Length);
            argv[originalArgv.Length] = null;

            string[] envp = new string[originalEnv.Length + 1];
            Array.Copy(originalEnv, envp, originalEnv.Length);
            envp[originalEnv.Length] = null;

            execve(path, argv, envp);

            // Only reached if execve failed.
            LogFile.Error($"execve failed: {Marshal.GetLastWin32Error()}");
            ExitProcess(1);
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = originalCwd,
                UseShellExecute = false,
            };

#if NETCOREAPP
            for (int i = 1; i < originalArgv.Length; i++)
                startInfo.ArgumentList.Add(originalArgv[i]);
#else
            // .NET Framework's ProcessStartInfo has no ArgumentList, so paste
            // the args into the legacy Arguments string with the same quoting.
            startInfo.Arguments = PasteArguments(originalArgv);
#endif

            startInfo.EnvironmentVariables.Clear();
            foreach (string entry in originalEnv)
            {
                int eq = entry.IndexOf('=');
                if (eq <= 0)
                    continue;
                startInfo.EnvironmentVariables[entry.Substring(0, eq)] = entry.Substring(eq + 1);
            }

            Process.Start(startInfo);
        }
        catch (Exception e)
        {
            LogFile.Error($"Failed to restart process: {e}");
            Environment.Exit(1);
            return;
        }

        Environment.Exit(0);
    }

    private static bool OnUpdateThread()
        => MyPrecalcComponent.UpdateThreadManagedId != 0
           && Environment.CurrentManagedThreadId == MyPrecalcComponent.UpdateThreadManagedId;

#if NETCOREAPP
    private static bool IsLinux => OperatingSystem.IsLinux();
#else
    // The .NET Framework (Legacy) build only ever runs on Windows.
    private static bool IsLinux => false;

    private static readonly char[] ArgumentDelimiters = { ' ', '\t', '\n', '\v', '"' };

    // Reproduces the Windows command-line quoting that ProcessStartInfo's
    // ArgumentList applies on .NET (Core), skipping argv[0] (the executable
    // path, passed separately via FileName).
    private static string PasteArguments(string[] argv)
    {
        var sb = new StringBuilder();
        for (int i = 1; i < argv.Length; i++)
            AppendArgument(sb, argv[i]);
        return sb.ToString();
    }

    private static void AppendArgument(StringBuilder sb, string argument)
    {
        if (sb.Length != 0)
            sb.Append(' ');

        if (argument.Length != 0 && argument.IndexOfAny(ArgumentDelimiters) < 0)
        {
            sb.Append(argument);
            return;
        }

        sb.Append('"');
        int idx = 0;
        while (idx < argument.Length)
        {
            char c = argument[idx++];
            if (c == '\\')
            {
                int slashes = 1;
                while (idx < argument.Length && argument[idx] == '\\')
                {
                    idx++;
                    slashes++;
                }

                if (idx == argument.Length)
                    sb.Append('\\', slashes * 2);
                else if (argument[idx] == '"')
                {
                    sb.Append('\\', slashes * 2 + 1);
                    sb.Append('"');
                    idx++;
                }
                else
                    sb.Append('\\', slashes);
            }
            else if (c == '"')
            {
                sb.Append('\\');
                sb.Append('"');
            }
            else
                sb.Append(c);
        }
        sb.Append('"');
    }
#endif
}
