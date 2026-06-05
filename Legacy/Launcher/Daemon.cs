using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Pulsar.Shared;

namespace Pulsar.Legacy.Launcher;

/// <summary>
/// Detaches the running process from its parent (typically Quasar) when the
/// <c>-daemon</c> flag is set, so the parent terminating does not take the
/// dedicated server down with it.
///
/// <para>
/// On Linux the detach is a <c>setsid()</c>: the process leaves the parent's
/// session and process group, so the group-wide SIGHUP/termination delivered
/// when the parent (or its controlling terminal) goes away no longer reaches it.
/// An explicit <c>kill -HUP &lt;pid&gt;</c> still triggers a config reload. When
/// the process was launched as a child (e.g. Quasar spawning it) this happens
/// <em>in place</em>, preserving the PID and the inherited stdout/stderr so the
/// parent can keep capturing the JSON log stream and tracking it by PID until it
/// exits. When the process is a process-group leader (e.g. a wrapper script
/// <c>exec</c>'d us, making us the leader of the group the shell created),
/// <c>setsid()</c> is forbidden (<c>EPERM</c>); we then re-exec a fresh child —
/// which is not a group leader — and let it detach, while this parent exits.
/// </para>
///
/// <para>
/// On Windows the detach is a <c>FreeConsole()</c> — the process detaches from
/// the inherited console so a parent console-close event can no longer kill it.
/// </para>
/// </summary>
internal static class Daemon
{
    // errno value for "operation not permitted" — what setsid() returns when the
    // caller is already a process-group leader.
    private const int EPERM = 1;

    // Set on the re-exec'd child so a second EPERM (which should never happen)
    // cannot spiral into an endless chain of re-execs.
    private const string ReexecMarker = "MAGNETAR_DAEMON_REEXEC";

    [DllImport("libc")]
    private static extern int getpid();

    [DllImport("libc", SetLastError = true)]
    private static extern int getsid(int pid);

    [DllImport("libc", SetLastError = true)]
    private static extern int setsid();

    [DllImport("libc", EntryPoint = "_exit")]
    private static extern void LibcExit(int status);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();

    /// <summary>
    /// Detaches the process from its parent. Call once at startup, after the log
    /// file is initialized, when <see cref="Flags.Daemon"/> is set. Idempotent:
    /// safe to call again after a daemon-mode restart (the process is already
    /// detached and the call is a no-op).
    /// </summary>
    public static void Detach()
    {
#if NETCOREAPP
        if (OperatingSystem.IsLinux())
        {
            DetachPosix();
            return;
        }
#endif
        DetachWindows();
    }

    private static void DetachPosix()
    {
        // Already a session leader (a prior re-exec already detached us, or an
        // execve restart that kept the session): nothing to do.
        if (getsid(0) == getpid())
        {
            LogFile.WriteLine("Daemon: already detached (session leader)");
            return;
        }

        if (setsid() >= 0)
        {
            LogFile.WriteLine("Daemon: detached into a new session");
            return;
        }

        int err = Marshal.GetLastWin32Error();

        // EPERM: we are a process-group leader (e.g. a wrapper exec'd us into the
        // group the shell created), so setsid() refuses in place. Re-exec a fresh
        // child that is not a leader and let it detach; this parent then exits.
        if (err == EPERM && Environment.GetEnvironmentVariable(ReexecMarker) == null)
        {
            ReexecDetached();
            return; // unreachable on success: ReexecDetached exits the process
        }

        LogFile.Warn($"Daemon: setsid failed (errno {err}); continuing attached to parent session");
    }

    private static void ReexecDetached()
    {
        string exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
        {
            LogFile.Warn("Daemon: cannot re-exec (process path unknown); continuing attached to parent session");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = false, // inherit stdin/stdout/stderr
            };

            // Forward our own arguments (which still include -daemon) verbatim.
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++)
                psi.ArgumentList.Add(args[i]);

            // Guard against re-exec recursion if the child somehow also lands as a
            // group leader and setsid() fails again.
            psi.Environment[ReexecMarker] = "1";

            Process.Start(psi);
            LogFile.WriteLine("Daemon: re-exec'd a detached child; parent exiting");
        }
        catch (Exception e)
        {
            LogFile.Error($"Daemon: re-exec failed ({e.Message}); continuing attached to parent session");
            return;
        }

        // Exit the parent so the child is reparented to init and the group/session
        // we were leading is dissolved. _exit skips finalizers — nothing meaningful
        // has started this early. The child re-runs startup and detaches via setsid.
        try { LogFile.Dispose(); } catch { }
        LibcExit(0);
    }

    private static void DetachWindows()
    {
        // Detach from the inherited console so a parent console-close event cannot
        // terminate us. Returns false when no console is attached (already
        // detached, or launched without one) — not an error for our purposes.
        // Note: this does not break a parent Job Object set to kill on close;
        // that can only be avoided at process-creation time.
        if (FreeConsole())
            LogFile.WriteLine("Daemon: detached from parent console");
        else
            LogFile.WriteLine("Daemon: no console to detach from");
    }
}
