using System.Threading.Tasks;
using PluginSdk.Commands;
using Pulsar.Legacy.Launcher;

namespace Pulsar.Legacy.Commands;

// Built-in chat commands registered by Magnetar before plugins load, so a
// plugin may override any of them (last registration wins). Each is the
// root's default command, run for a bare "!save" / "!restart" / "!quit", and
// defaults to Admin permission. The lifecycle work is offloaded to a worker
// thread so the saving fast path can block for the disk write to finish before
// the process exits or restarts; the caller is acknowledged first.

[CommandRoot("save", "Magnetar", "Save the world")]
public sealed class SaveCommand : CommandModule
{
    [Command("", "Save the world")]
    public void Save()
    {
        Context.Respond("Saving world\u2026");
        Task.Run(() => ServerControl.SaveWorld());
    }
}

[CommandRoot("restart", "Magnetar", "Save and restart the server")]
public sealed class RestartCommand : CommandModule
{
    [Command("", "Save and restart the server")]
    public void Restart()
    {
        Context.Respond("Saving world and restarting the server\u2026");
        Task.Run(ServerControl.SaveAndRestart);
    }
}

[CommandRoot("quit", "Magnetar", "Shut the server down without saving")]
public sealed class QuitCommand : CommandModule
{
    [Command("", "Shut the server down without saving")]
    public void Quit()
    {
        Context.Respond("Shutting the server down without saving\u2026");
        Task.Run(ServerControl.QuitWithoutSaving);
    }
}
