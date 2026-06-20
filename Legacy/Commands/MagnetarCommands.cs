using System.Threading.Tasks;
using PluginSdk.Commands;
using Pulsar.Legacy.Launcher;
using Pulsar.Shared;

namespace Pulsar.Legacy.Commands;

// Built-in chat commands registered by Magnetar before plugins load, so a
// plugin may override any of them (last registration wins). Each is the
// root's default command, run for a bare "!save" / "!restart" / "!quit" /
// "!stop", and defaults to Admin permission. The lifecycle work is offloaded
// to a worker thread so the saving fast path can block for the disk write to
// finish before the process exits or restarts; the caller is acknowledged
// first.

[CommandRoot("save", "Magnetar", "Save the world")]
public sealed class SaveCommand : CommandModule
{
    [Command("", "Save the world")]
    public void Save()
    {
        var context = Context;
        Context.Respond("Saving world\u2026");
        Task.Run(() =>
        {
            try
            {
                var reply = ServerControl.SaveWorld()
                    ? "World saved."
                    : "World save did not finish before the timeout.";
                Game.RunOnGameThread(() => context.Respond(reply));
            }
            catch (System.Exception e)
            {
                LogFile.Error($"!save failed: {e}");
                Game.RunOnGameThread(() => context.Respond(CommandReply.Error($"World save failed: {e.Message}")));
            }
        });
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

[CommandRoot("stop", "Magnetar", "Save the world then shut the server down")]
public sealed class StopCommand : CommandModule
{
    [Command("", "Save the world then shut the server down")]
    public void Stop()
    {
        var context = Context;
        Context.Respond("Saving world and shutting the server down\u2026");
        Task.Run(() =>
        {
            try
            {
                // Block for the disk write to finish, then quit. The world is
                // already persisted by SaveWorld(), so quit without saving again.
                var reply = ServerControl.SaveWorld()
                    ? "World saved, shutting down\u2026"
                    : "World save did not finish before the timeout, shutting down anyway\u2026";
                Game.RunOnGameThread(() => context.Respond(reply));
            }
            catch (System.Exception e)
            {
                LogFile.Error($"!stop failed: {e}");
                Game.RunOnGameThread(() => context.Respond(CommandReply.Error($"World save failed: {e.Message}, shutting down anyway\u2026")));
            }

            ServerControl.QuitWithoutSaving();
        });
    }
}
