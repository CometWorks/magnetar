using System;
using System.IO;
using Magnetar.ConfigTerminal.Io;
using SysProcess = System.Diagnostics.Process;

namespace Magnetar.ConfigTerminal.Process;

/// <summary>
/// Reads and verifies <c>magnetar.pid</c> (written by the launcher, §2.8). The
/// file's presence alone is never trusted: the pid must be live AND its identity
/// must match this instance. A crash leaves a stale file, reported as such and
/// never signalled.
/// </summary>
internal sealed class PidFileReader
{
    private readonly string pidFilePath;
    private readonly string expectedDataDir;

    public PidFileReader(string pidFilePath, string expectedDataDir)
    {
        this.pidFilePath = pidFilePath;
        this.expectedDataDir = expectedDataDir;
    }

    public ServerStatus Query()
    {
        var status = new ServerStatus { State = ServerState.NotRunning };

        if (!File.Exists(pidFilePath))
            return status;

        int pid;
        string dataDirLine;
        try
        {
            string[] lines = File.ReadAllLines(pidFilePath);
            if (lines.Length == 0 || !int.TryParse(lines[0].Trim(), out pid))
                return status; // unreadable → treat as not running
            dataDirLine = lines.Length > 1 ? lines[1].Trim() : string.Empty;
        }
        catch
        {
            return status;
        }

        SysProcess proc = TryGetProcess(pid);
        if (proc == null)
        {
            status.State = ServerState.StalePidFile;
            status.Pid = pid;
            status.Detail = "pid file present but the process is gone (stale)";
            return status;
        }

        status.Pid = pid;
        try { status.StartedAt = proc.StartTime; } catch { }

        if (IdentityMatches(pid, dataDirLine, proc))
        {
            status.State = ServerState.Running;
        }
        else
        {
            status.State = ServerState.Foreign;
            status.Detail = "a different process holds this pid (identity mismatch)";
        }
        return status;
    }

    private bool IdentityMatches(int pid, string dataDirLine, SysProcess proc)
    {
        // Strongest signal: the pid file's DS-data-dir line matches what we bound to.
        if (!string.IsNullOrEmpty(expectedDataDir) && !string.IsNullOrEmpty(dataDirLine))
        {
            if (PathsEqual(dataDirLine, expectedDataDir))
                return true;
        }

        // Linux: confirm the cmdline still references Magnetar / the DS data dir,
        // guarding against a recycled pid.
        if (PlatformPaths.IsLinux)
        {
            string cmdline = ReadProcCmdline(pid);
            if (cmdline != null)
            {
                if (cmdline.IndexOf("Magnetar", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                if (!string.IsNullOrEmpty(expectedDataDir)
                    && cmdline.IndexOf(expectedDataDir, PlatformPaths.PathComparison) >= 0)
                    return true;
            }
            return false;
        }

        // Windows fallback: match on the process name.
        try
        {
            string name = proc.ProcessName ?? string.Empty;
            return name.IndexOf("Magnetar", StringComparison.OrdinalIgnoreCase) >= 0
                   || name.IndexOf("SpaceEngineers", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool PathsEqual(string a, string b)
    {
        try
        {
            string na = Path.GetFullPath(a).TrimEnd('/', '\\');
            string nb = Path.GetFullPath(b).TrimEnd('/', '\\');
            return PlatformPaths.PathComparer.Equals(na, nb);
        }
        catch
        {
            return PlatformPaths.PathComparer.Equals(a, b);
        }
    }

    private static string ReadProcCmdline(int pid)
    {
        try
        {
            string path = $"/proc/{pid}/cmdline";
            if (!File.Exists(path))
                return null;
            // cmdline args are NUL-separated.
            return File.ReadAllText(path).Replace('\0', ' ');
        }
        catch
        {
            return null;
        }
    }

    private static SysProcess TryGetProcess(int pid)
    {
        try
        {
            SysProcess p = SysProcess.GetProcessById(pid);
            if (p.HasExited)
                return null;
            return p;
        }
        catch
        {
            return null;
        }
    }
}
