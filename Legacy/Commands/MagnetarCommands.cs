using System;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using PluginSdk.Commands;
using Magnetar.Legacy.Launcher;
using Pulsar.Shared;
using SdkServerControl = PluginSdk.ServerControl;
using ServerTerminationKind = PluginSdk.ServerTerminationKind;

namespace Magnetar.Legacy.Commands;

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
                var reply = SdkServerControl.SaveWorld()
                    ? "World saved."
                    : "World save did not finish before the timeout.";
                Game.RunOnGameThread(() => context.Respond(reply));
            }
            catch (Exception e)
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
        if (LifecycleCommandRouting.TryRoute(Context, ServerTerminationKind.Restart,
                saveFirst: true, "Magnetar !restart"))
            return;

        Context.Respond("Saving world and restarting the server\u2026");
        Task.Run(SdkServerControl.SaveAndRestart);
    }
}

[CommandRoot("quit", "Magnetar", "Shut the server down without saving")]
public sealed class QuitCommand : CommandModule
{
    [Command("", "Shut the server down without saving")]
    public void Quit()
    {
        if (LifecycleCommandRouting.TryRoute(Context, ServerTerminationKind.Shutdown,
                saveFirst: false, "Magnetar !quit"))
            return;

        Context.Respond("Shutting the server down without saving\u2026");
        Task.Run(SdkServerControl.QuitWithoutSaving);
    }
}

[CommandRoot("stop", "Magnetar", "Save the world then shut the server down")]
public sealed class StopCommand : CommandModule
{
    [Command("", "Save the world then shut the server down")]
    public void Stop()
    {
        if (LifecycleCommandRouting.TryRoute(Context, ServerTerminationKind.Shutdown,
                saveFirst: true, "Magnetar !stop"))
            return;

        var context = Context;
        Context.Respond("Saving world and shutting the server down\u2026");
        Task.Run(() =>
        {
            try
            {
                // Block for the disk write to finish, then quit. The world is
                // already persisted by SaveWorld(), so quit without saving again.
                var reply = SdkServerControl.SaveWorld()
                    ? "World saved, shutting down\u2026"
                    : "World save did not finish before the timeout, shutting down anyway\u2026";
                Game.RunOnGameThread(() => context.Respond(reply));
            }
            catch (Exception e)
            {
                LogFile.Error($"!stop failed: {e}");
                Game.RunOnGameThread(() => context.Respond(CommandReply.Error($"World save failed: {e.Message}, shutting down anyway\u2026")));
            }

            SdkServerControl.QuitWithoutSaving();
        });
    }
}

internal static class LifecycleCommandRouting
{
    public static bool TryRoute(CommandContext context, ServerTerminationKind kind,
        bool saveFirst, string reason)
    {
        var request = new ClusterLifecycleRequest(Guid.NewGuid(), kind,
            ClusterLifecycleOrigin.ChatCommand, saveFirst, context.Caller.SteamId, reason);
        if (!ClusterLifecycle.TryRequest(request,
                out Task<ClusterLifecycleAcknowledgement> acknowledgement))
            return false;

        context.Respond(kind == ServerTerminationKind.Restart
            ? "Requesting a coordinated node restart\u2026"
            : "Requesting coordinated shutdown\u2026");
        _ = ReplyWhenAcknowledged(context, acknowledgement);
        return true;
    }

    private static async Task ReplyWhenAcknowledged(CommandContext context,
        Task<ClusterLifecycleAcknowledgement> pending)
    {
        ClusterLifecycleAcknowledgement acknowledgement = await pending.ConfigureAwait(false);
        bool accepted = acknowledgement.Disposition is ClusterLifecycleDisposition.Accepted
            or ClusterLifecycleDisposition.AlreadyApplied;
        string message = string.IsNullOrWhiteSpace(acknowledgement.Message)
            ? acknowledgement.ReasonCode
            : acknowledgement.Message;
        Game.RunOnGameThread(() =>
        {
            if (accepted)
                context.Respond(message);
            else
                context.Respond(CommandReply.Error(
                    $"Request denied ({acknowledgement.ReasonCode}): {message}"));
        });
    }
}
