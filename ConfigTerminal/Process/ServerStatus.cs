using System;

namespace Magnetar.ConfigTerminal.Process;

internal enum ServerState
{
    NotRunning,
    Starting,
    Running,
    Stopping,
    StalePidFile,   // pid file present but the process is gone
    Foreign,        // pid alive but identity does not match this instance
}

/// <summary>A snapshot of the managed instance's process state.</summary>
internal sealed class ServerStatus
{
    public ServerState State { get; set; } = ServerState.NotRunning;
    public int? Pid { get; set; }
    public DateTime? StartedAt { get; set; }
    public string Detail { get; set; } = string.Empty;

    public bool IsAlive => State == ServerState.Running || State == ServerState.Starting || State == ServerState.Stopping;

    public TimeSpan? Uptime => StartedAt.HasValue ? DateTime.Now - StartedAt.Value : (TimeSpan?)null;

    public override string ToString()
    {
        switch (State)
        {
            case ServerState.Running:
                string up = Uptime.HasValue ? $" up {FormatUptime(Uptime.Value)}" : string.Empty;
                return $"RUNNING pid {Pid}{up}";
            case ServerState.Starting: return "STARTING…";
            case ServerState.Stopping: return "STOPPING…";
            case ServerState.StalePidFile: return "STALE PID FILE";
            case ServerState.Foreign: return $"FOREIGN pid {Pid}";
            default: return "STOPPED";
        }
    }

    public static string FormatUptime(TimeSpan t) =>
        $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
}
