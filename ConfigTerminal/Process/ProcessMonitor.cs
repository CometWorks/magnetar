using System;

namespace Magnetar.ConfigTerminal.Process;

/// <summary>
/// Polls the server status and raises <see cref="Changed"/> when it moves. UI-
/// agnostic: the view drives <see cref="Poll"/> from the Terminal.Gui main-loop
/// timer so all callbacks stay on the single UI thread.
/// </summary>
internal sealed class ProcessMonitor
{
    private readonly MagnetarProcess process;

    public ProcessMonitor(MagnetarProcess process)
    {
        this.process = process;
        Latest = new ServerStatus();
    }

    public ServerStatus Latest { get; private set; }

    public event Action<ServerStatus> Changed;

    /// <summary>Refreshes the status; fires <see cref="Changed"/> on a state or pid transition.</summary>
    public ServerStatus Poll()
    {
        ServerStatus next = process.Query();
        if (next.State != Latest.State || next.Pid != Latest.Pid)
        {
            Latest = next;
            Changed?.Invoke(next);
        }
        else
        {
            Latest = next; // keep uptime fresh even without a transition
        }
        return next;
    }
}
