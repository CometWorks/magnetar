using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using SysProcess = System.Diagnostics.Process;

namespace Magnetar.ConfigTerminal.Process;

/// <summary>Outcome of a process operation.</summary>
internal sealed class OpResult
{
    public bool Ok { get; private set; }
    public string Message { get; private set; } = string.Empty;

    public static OpResult Success(string m = "") => new() { Ok = true, Message = m };
    public static OpResult Fail(string m) => new() { Ok = false, Message = m };
}

/// <summary>
/// Controls the one Magnetar instance: start (daemonized), stop (SIGTERM →
/// save+quit), reload (SIGHUP), force-kill, and status query via the pid file.
/// On Windows, graceful stop/reload are deferred (no signal path to a detached
/// process); Start and status work everywhere.
/// </summary>
internal sealed class MagnetarProcess
{
    private const int SIGHUP = 1;
    private const int SIGKILL = 9;
    private const int SIGTERM = 15;

    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int sig);

    private readonly InstanceBinding binding;
    private readonly PidFileReader pidReader;

    public MagnetarProcess(InstanceBinding binding)
    {
        this.binding = binding;
        pidReader = new PidFileReader(binding.PidFilePath, binding.DataDir);
    }

    public ServerStatus Query() => pidReader.Query();

    /// <summary>
    /// Spawns Magnetar daemonized, then waits (bounded) for the pid file to
    /// appear and verify. stdout/stderr are discarded so the child cannot corrupt
    /// the TUI or block on a full pipe.
    /// </summary>
    public OpResult Start(LaunchSpec spec, TimeSpan? readyTimeout = null)
    {
        ServerStatus current = Query();
        if (current.State == ServerState.Running || current.State == ServerState.Starting)
            return OpResult.Fail("Server is already running.");
        if (current.State == ServerState.Foreign)
            return OpResult.Fail("A foreign process holds this pid; refusing to start.");

        string reason = spec.RejectionReason();
        if (reason != null)
            return OpResult.Fail(reason);

        string exe = binding.MagnetarExePath;
        if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
            return OpResult.Fail($"Magnetar launcher not found: {exe}");

        // Remove any stale pid file so the appearance wait is unambiguous.
        TryDeleteStalePid(current);

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(exe),
        };
#if NETFRAMEWORK
        // net48 has no ArgumentList; join into the escaped Arguments string.
        psi.Arguments = ArgumentEscaping.Join(spec.BuildArgs());
#else
        foreach (string arg in spec.BuildArgs())
            psi.ArgumentList.Add(arg);
#endif

        SysProcess proc;
        try
        {
            proc = SysProcess.Start(psi);
        }
        catch (Exception e)
        {
            return OpResult.Fail($"Failed to launch Magnetar: {e.Message}");
        }

        if (proc == null)
            return OpResult.Fail("Failed to launch Magnetar (no process handle).");

        // Discard the child's console output so it never reaches our terminal.
        DrainAsync(proc);

        TimeSpan timeout = readyTimeout ?? TimeSpan.FromSeconds(60);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (proc.HasExited)
                return OpResult.Fail($"Magnetar exited early (code {proc.ExitCode}) before the pid file appeared. Check the logs.");

            ServerStatus s = Query();
            if (s.State == ServerState.Running)
                return OpResult.Success($"Started (pid {s.Pid}).");

            Thread.Sleep(250);
        }

        return OpResult.Fail("Timed out waiting for the server to report its pid. It may still be starting; check the logs.");
    }

    /// <summary>Graceful stop: SIGTERM → Magnetar saves the world and quits. Waits up to the grace period.</summary>
    public OpResult Stop(TimeSpan gracePeriod)
    {
        ServerStatus s = Query();
        if (s.State != ServerState.Running || s.Pid == null)
            return OpResult.Fail("Server is not running.");

        if (!PlatformPaths.IsLinux)
            return OpResult.Fail("Graceful stop is not available on Windows in this build; use force-kill (may lose progress).");

        if (kill(s.Pid.Value, SIGTERM) != 0)
            return OpResult.Fail($"Failed to send SIGTERM to pid {s.Pid} (errno {Marshal.GetLastWin32Error()}).");

        return WaitForExit(gracePeriod)
            ? OpResult.Success("Server stopped.")
            : OpResult.Fail("Server did not stop within the grace period.");
    }

    /// <summary>SIGHUP: Magnetar saves and reloads live-reloadable config. Linux, running only.</summary>
    public OpResult Reload()
    {
        ServerStatus s = Query();
        if (s.State != ServerState.Running || s.Pid == null)
            return OpResult.Fail("Server is not running.");
        if (!PlatformPaths.IsLinux)
            return OpResult.Fail("Live reload is only available on Linux in this build.");

        if (kill(s.Pid.Value, SIGHUP) != 0)
            return OpResult.Fail($"Failed to send SIGHUP to pid {s.Pid} (errno {Marshal.GetLastWin32Error()}).");
        return OpResult.Success("Reload signal sent.");
    }

    /// <summary>Force-kill (SIGKILL / TerminateProcess). Data since the last save is lost.</summary>
    public OpResult ForceKill(TimeSpan gracePeriod)
    {
        ServerStatus s = Query();
        if (s.Pid == null)
            return OpResult.Fail("No process to kill.");

        try
        {
            if (PlatformPaths.IsLinux)
                kill(s.Pid.Value, SIGKILL);
            else
                SysProcess.GetProcessById(s.Pid.Value).Kill();
        }
        catch (Exception e)
        {
            return OpResult.Fail($"Force-kill failed: {e.Message}");
        }

        return WaitForExit(gracePeriod)
            ? OpResult.Success("Server killed.")
            : OpResult.Fail("Process still present after force-kill.");
    }

    private bool WaitForExit(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            ServerStatus s = Query();
            if (s.State == ServerState.NotRunning || s.State == ServerState.StalePidFile)
                return true;
            Thread.Sleep(250);
        }
        ServerStatus final = Query();
        return final.State == ServerState.NotRunning || final.State == ServerState.StalePidFile;
    }

    private void TryDeleteStalePid(ServerStatus current)
    {
        if (current.State != ServerState.StalePidFile)
            return;
        try
        {
            if (File.Exists(binding.PidFilePath))
                File.Delete(binding.PidFilePath);
        }
        catch
        {
        }
    }

    private static void DrainAsync(SysProcess proc)
    {
        try
        {
            proc.OutputDataReceived += static (_, _) => { };
            proc.ErrorDataReceived += static (_, _) => { };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
        }
        catch
        {
            // Draining is best effort; if it fails the worst case is stray output.
        }
    }
}
