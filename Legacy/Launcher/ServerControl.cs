using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
/// disk write itself runs on a background task, so blocking a worker — or even
/// the update thread, on the inline fast path — for its completion never
/// deadlocks.
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

    // Rooted so the CLR does not finalize the registrations (a finalized
    // registration silently stops delivering the signal).
    private static PosixSignalRegistration sigTerm;
    private static PosixSignalRegistration sigInt;
    private static PosixSignalRegistration sigHup;

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
    /// Installs the POSIX signal handlers and binds the plugin SDK facade.
    /// Safe to call before a session exists — the handlers tolerate a null
    /// <see cref="MySession.Static"/>.
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

        sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnTerminate);
        sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnTerminate);

        // SIGHUP is Linux-only; on Windows Create() throws for it.
        if (OperatingSystem.IsLinux())
            sigHup = PosixSignalRegistration.Create(PosixSignal.SIGHUP, OnReload);
    }

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
        var finished = new ManualResetEventSlim(false);
        bool startedOwnSave = false;

        void StartSave()
        {
            try
            {
                // A save is already running (e.g. an autosave): don't start a
                // second one, just wait for it via the InProgress flag.
                if (MyAsyncSaving.InProgress)
                    return;

                startedOwnSave = true;
                MyAsyncSaving.Start(finished.Set);
            }
            catch (Exception e)
            {
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

        TimeSpan remaining = limit - stopwatch.Elapsed;
        if (remaining < TimeSpan.Zero)
            remaining = TimeSpan.Zero;

        // Our own save signals completion via the MyAsyncSaving callback; a
        // pre-existing save is tracked through the InProgress flag.
        if (startedOwnSave)
        {
            if (!finished.Wait(remaining))
            {
                LogFile.Warn("World save did not finish within the timeout");
                return false;
            }
            return true;
        }

        while (stopwatch.Elapsed < limit)
        {
            if (!MyAsyncSaving.InProgress)
                return true;
            Thread.Sleep(50);
        }

        LogFile.Warn("In-progress world save did not finish within the timeout");
        return false;
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
        if (OperatingSystem.IsLinux())
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

        if (OperatingSystem.IsLinux())
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

            for (int i = 1; i < originalArgv.Length; i++)
                startInfo.ArgumentList.Add(originalArgv[i]);

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
}
