using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pulsar.Legacy.Launcher;
using Pulsar.Legacy.Loader;
using Pulsar.Shared;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using VRage.Utils;
using VRage.Voxels;

namespace Magnetar.Legacy.Launcher;

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
    private static readonly TimeSpan PreviousProcessTimeout = TimeSpan.FromSeconds(60);

    // Set by a Windows restart for the new process: the PID of the old one.
    private const string RestartWaitPidVariable = "MAGNETAR_RESTART_WAIT_PID";

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

    // Rooted so the GC does not collect the delegate native code calls.
    private static ConsoleCtrlHandler consoleCtrlHandler;

    private static volatile bool terminating;
    private static readonly object terminateLock = new object();

    [DllImport("libc", EntryPoint = "_exit")]
    private static extern void LibcExit(int status);

    [DllImport("libc", SetLastError = true)]
    private static extern int execve(string path, string[] argv, string[] envp);

    private const int Synchronize = 0x100000;

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(int access, bool inheritHandle, int processId);

    [DllImport("kernel32.dll")]
    private static extern int WaitForSingleObject(IntPtr handle, int milliseconds);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    private const int CtrlCloseEvent = 2;
    private const int CtrlShutdownEvent = 6;

    private delegate bool ConsoleCtrlHandler(int ctrlType);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler handler, bool add);

    /// <summary>
    /// Records the original command line, working directory and environment so
    /// a restart can reproduce the launch exactly. Call once at the top of
    /// <c>Main</c>, before any cwd/env mutation.
    /// </summary>
    public static void CaptureLaunchState(string[] argv, string cwd, IDictionary env)
    {
        originalArgv = HostArgv(argv ?? Array.Empty<string>());
        originalCwd = cwd;

        var list = new List<string>();
        if (env != null)
            foreach (DictionaryEntry e in env)
                if ((string)e.Key != RestartWaitPidVariable)
                    list.Add($"{e.Key}={e.Value}");
        originalEnv = list.ToArray();
    }

    /// <summary>
    /// After a Windows restart, waits for the old process to be gone. It may
    /// still be exiting and holding the patched game assemblies, which the
    /// preloader rewrites on startup. Call at the top of <c>Main</c>.
    /// </summary>
    public static void WaitForPreviousProcess()
    {
        string pid = Environment.GetEnvironmentVariable(RestartWaitPidVariable);
        if (pid == null)
            return;

        Environment.SetEnvironmentVariable(RestartWaitPidVariable, null);

        // Not Process.GetProcessById: it treats a process as gone once its exit
        // code is set, while the DS's native teardown still runs.
        IntPtr previous = OpenProcess(Synchronize, false, int.Parse(pid));
        if (previous == IntPtr.Zero)
            return;
        WaitForSingleObject(previous, (int)PreviousProcessTimeout.TotalMilliseconds);
        CloseHandle(previous);
    }

    /// <summary>
    /// Binds the plugin SDK facade, installs the POSIX signal handlers on .NET
    /// (Core) and the console close and shutdown handler on Windows. Safe to
    /// call before a session exists — the handlers tolerate a null
    /// <see cref="MySession.Static"/>. The .NET Framework (Legacy) build has no
    /// POSIX signals.
    /// </summary>
    public static void InstallSignalHandlers()
    {
        PluginSdk.ServerControl.Bind(
            () => SaveWorld(),
            ReloadConfig,
            SaveAndQuit,
            SaveAndRestart,
            QuitWithoutSaving,
            RestartWithoutSaving
        );

#if NETCOREAPP
        sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, OnTerminate);
        sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, OnTerminate);

        // SIGHUP is Linux-only; on Windows Create() throws for it.
        if (OperatingSystem.IsLinux())
            sigHup = PosixSignalRegistration.Create(PosixSignal.SIGHUP, OnReload);
#endif

        // Registered after SIGTERM, which .NET maps to the shutdown event on
        // Windows, because Windows calls the last registered handler first.
        if (!IsLinux)
        {
            consoleCtrlHandler = OnConsoleCtrl;
            SetConsoleCtrlHandler(consoleCtrlHandler, true);
        }
    }

    // Windows ends the process when the handler returns after the console
    // window was closed or Windows is shutting down, or when its time runs
    // out (5 s for a closed window). So save and quit, and hold the handler
    // until that exits the process. The logoff event is left alone: a server
    // running as a service gets it whenever any user logs off.
    private static bool OnConsoleCtrl(int ctrlType)
    {
        // Ctrl+C and Ctrl+Break go on to the dedicated server's own handler.
        if (ctrlType != CtrlCloseEvent && ctrlType != CtrlShutdownEvent)
            return false;

        LogFile.WriteLine(
            ctrlType == CtrlCloseEvent
                ? "Console window closed: saving world and quitting"
                : "Windows is shutting down: saving world and quitting"
        );
        // Closing the window asks for a stop; a shutdown is the host going
        // down, which, like a signal, a supervisor should recover from.
        Task.Run(() => SaveAndQuit(requested: ctrlType == CtrlCloseEvent));
        Thread.Sleep(Timeout.Infinite);
        return true;
    }

#if NETCOREAPP
    private static void OnTerminate(PosixSignalContext context)
    {
        // Cancel the default disposition (process termination) so the save can
        // finish, and keep the signal-dispatch thread fast by handing off.
        context.Cancel = true;
        QuitOnSignal(context.Signal.ToString());
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

        LogFile.Warn(
            startedOwnSave
                ? "World save did not finish within the timeout"
                : "In-progress world save did not finish within the timeout"
        );
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
                    MyGameService.GameServer?.SetServerName(
                        MySandboxGame.ConfigDedicated.ServerName
                    );
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

    /// <summary>Saves the world, then quits with exit code 0, for a signal:
    /// on a worker thread and without Terminating. On Linux one Ctrl+C reaches
    /// two handlers, so only the call that begins the shutdown logs.</summary>
    public static void QuitOnSignal(string signal) =>
        Task.Run(() =>
            SaveAndQuit(requested: false, $"Received {signal}: saving world and quitting")
        );

    /// <summary>Saves the world, then quits with exit code 0.</summary>
    public static void SaveAndQuit() => SaveAndQuit(requested: true);

    // A signal comes from outside the server, under a supervisor mostly
    // because the host shuts down, so it raises no Terminating: a supervisor
    // that keeps a server off after a requested stop must start it again.
    private static void SaveAndQuit(bool requested, string reason = null)
    {
        if (!BeginTerminate())
            return;

        if (reason != null)
            LogFile.WriteLine(reason);
        if (requested)
            PluginSdk.ServerControl.RaiseTerminating(PluginSdk.ServerTerminationKind.Shutdown);
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
        LogFile.WriteLine($"Restarting: {originalArgv.FirstOrDefault()}");
        DisposePlugins();
        RestartProcess();
    }

    /// <summary>Restarts immediately with the original launch state, no save.</summary>
    public static void RestartWithoutSaving()
    {
        if (!BeginTerminate())
            return;

        PluginSdk.ServerControl.RaiseTerminating(PluginSdk.ServerTerminationKind.Restart);
        LogFile.WriteLine($"Restarting without saving: {originalArgv.FirstOrDefault()}");
        DisposePlugins();
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
        PluginLoader loader = PluginLoader.Instance;
        if (loader == null)
            return;

        try
        {
            // The loader's Update and HandleInput drop disposed plugins from the
            // list Dispose walks, so Dispose must not run while the game thread
            // updates plugins: run it there, or on a worker while the game
            // thread waits here. Without a session the game loop isn't running
            // yet: the world loads at the end of the loader's Init. Bound the
            // wait so a misbehaving plugin cannot hang shutdown.
            Task dispose = new Task(loader.Dispose);
            if (OnUpdateThread() || MySandboxGame.Static == null || MySession.Static == null)
                dispose.Start();
            else
                Game.RunOnGameThread(dispose.RunSynchronously);
            if (!dispose.Wait(DisposeTimeout))
                LogFile.Warn("Plugin disposal timed out");
        }
        catch (Exception e)
        {
            LogFile.Error($"Error disposing plugins: {e}");
        }
    }

    /// <summary>
    /// Runs the shutdown cleanup on a fatal-exit path — an unhandled managed
    /// exception or a native crash — where <see cref="Environment.Exit(int)"/> is
    /// called directly and the normal quit sequence never runs. Only the pid
    /// file removal and the log flush apply here: the world is deliberately not
    /// saved and plugins are not disposed, because the process state is already
    /// unsound. Never throws, so it cannot mask the failure being reported.
    /// </summary>
    public static void FlushOnFatalExit()
    {
        try
        {
            FlushAll();
        }
        catch { }
    }

    private static void FlushAll()
    {
        // Remove the pid file before flushing logs so a reader polling during
        // shutdown sees it disappear promptly. The fatal-exit paths reach this
        // through FlushOnFatalExit; anything that kills the process outright
        // (SIGKILL, a native abort) still leaves a stale file behind, which the
        // reader detects by probing the (now dead) pid.
        try
        {
            PidFile.Delete();
        }
        catch { }
        try
        {
            Console.Out.Flush();
        }
        catch { }
        try
        {
            Console.Error.Flush();
        }
        catch { }
        try
        {
            MyLog.Default?.Flush();
        }
        catch { }
        try
        {
            LogFile.Dispose();
        }
        catch { }
    }

    private static void ExitProcess(int code)
    {
        // libc _exit guarantees the exact exit code and cannot hang in a
        // finalizer or static destructor, unlike Environment.Exit.
        if (IsLinux)
        {
            try
            {
                LibcExit(code);
            }
            catch { }
        }

        Environment.Exit(code);
    }

    // On .NET, element 0 of Environment.GetCommandLineArgs() is the managed
    // MagnetarInterim.dll, not the apphost that was launched, and neither execve
    // nor CreateProcess can start a .dll. Restart through the host binary, and
    // keep the .dll as its first argument when the dotnet muxer ran it.
    private static string[] HostArgv(string[] argv)
    {
#if NETCOREAPP
        string host = Environment.ProcessPath;
        if (argv.Length > 0 && !string.IsNullOrEmpty(host))
        {
            bool muxer = Path.GetFileNameWithoutExtension(host) == "dotnet";
            var hostArgv = new List<string> { host };
            hostArgv.AddRange(muxer ? argv : argv.Skip(1));
            return hostArgv.ToArray();
        }
#endif
        return argv;
    }

    // Disposing the plugins has closed the log file by now, so failures go to
    // stderr.
    private static void RestartProcess()
    {
        FlushAll();

        if (originalArgv.Length == 0)
        {
            Console.Error.WriteLine("Cannot restart: original launch state was not captured");
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
                try
                {
                    Environment.CurrentDirectory = originalCwd;
                }
                catch { }
            }

            string[] argv = new string[originalArgv.Length + 1];
            Array.Copy(originalArgv, argv, originalArgv.Length);
            argv[originalArgv.Length] = null;

            string[] envp = new string[originalEnv.Length + 1];
            Array.Copy(originalEnv, envp, originalEnv.Length);
            envp[originalEnv.Length] = null;

            execve(path, argv, envp);

            // Only reached if execve failed.
            Console.Error.WriteLine(
                $"Restart failed: execve {path}: errno {Marshal.GetLastWin32Error()}"
            );
            ExitProcess(1);
            return;
        }

        // The new process starts while this one is still exiting.
        originalEnv = originalEnv
            .Append($"{RestartWaitPidVariable}={Process.GetCurrentProcess().Id}")
            .ToArray();

        try
        {
#if NETCOREAPP
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
#else
            StartWithoutInheritedHandles(path);
#endif
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Restart failed: {path}: {e}");
            Environment.Exit(1);
            return;
        }

        Environment.Exit(0);
    }

    private static bool OnUpdateThread() =>
        MyPrecalcComponent.UpdateThreadManagedId != 0
        && Environment.CurrentManagedThreadId == MyPrecalcComponent.UpdateThreadManagedId;

#if NETCOREAPP
    private static bool IsLinux => OperatingSystem.IsLinux();
#else
    // The .NET Framework (Legacy) build only ever runs on Windows.
    private static bool IsLinux => false;

    private static readonly char[] ArgumentDelimiters = { ' ', '\t', '\n', '\v', '"' };

    private const int CreateUnicodeEnvironment = 0x400;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int cb;
        public string lpReserved,
            lpDesktop,
            lpTitle;
        public int dwX,
            dwY,
            dwXSize,
            dwYSize,
            dwXCountChars,
            dwYCountChars,
            dwFillAttribute,
            dwFlags;
        public short wShowWindow,
            cbReserved2;
        public IntPtr lpReserved2,
            hStdInput,
            hStdOutput,
            hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr hProcess,
            hThread;
        public int dwProcessId,
            dwThreadId;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(
        string applicationName,
        StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        bool inheritHandles,
        int creationFlags,
        string environment,
        string currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation
    );

    // .NET Framework creates sockets inheritable, and Process.Start passes every
    // inheritable handle to the child. The new server then holds the old one's
    // game port and fails to bind it ("Error binding server endpoint", 0x2740).
    // CreateProcess without handle inheritance avoids that; the child still
    // attaches to the same console.
    private static void StartWithoutInheritedHandles(string path)
    {
        var commandLine = new StringBuilder();
        foreach (string argument in originalArgv)
            AppendArgument(commandLine, argument);

        string environment = string.Join("\0", originalEnv) + "\0\0";
        var startupInfo = new StartupInfo { cb = Marshal.SizeOf(typeof(StartupInfo)) };
        if (
            !CreateProcess(
                path,
                commandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                CreateUnicodeEnvironment,
                environment,
                originalCwd,
                ref startupInfo,
                out ProcessInformation process
            )
        )
            throw new System.ComponentModel.Win32Exception();

        CloseHandle(process.hThread);
        CloseHandle(process.hProcess);
    }

    // The Windows command-line quoting that ProcessStartInfo's ArgumentList
    // applies on .NET (Core).
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
