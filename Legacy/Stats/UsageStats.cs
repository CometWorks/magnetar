using System.Linq;
using System.Threading.Tasks;
using Pulsar.Shared;
using Pulsar.Shared.Config;

namespace Magnetar.Legacy.Stats;

/// <summary>
/// Fire-and-forget reporting of the enabled plugin set to the Magnetar
/// statistics server, gated on <see cref="ConsentManager"/>. Replaces the
/// reporting Pulsar's Loader does for game clients (which is inert here
/// because the Steam client API is never initialized on a server). The
/// implicit compatibility plugins are included so their usage is visible.
/// </summary>
public static class UsageStats
{
    public static void ReportEnabledPlugins(string votesServer, string[] corePluginIds)
    {
        if (!ConsentManager.Granted)
            return;

        VotesClient.BaseUrl = ConfigManager.Instance.Core.StatsServerBaseUrl ?? votesServer;

        Task.Run(() =>
        {
            if (ConsentManager.PendingServerConsent)
            {
                if (VotesClient.Consent(true))
                    LogFile.WriteLine("Consent has been registered on the statistics server");
                else
                    LogFile.Error("Failed to register consent on the statistics server");
            }

            LogFile.WriteLine("Reporting plugin usage");

            string[] profilePluginIds =
            [
                .. ConfigManager.Instance.Profiles.Current.GetPluginIDs(false),
            ];
            string[] trackablePluginIds = [.. profilePluginIds.Union(corePluginIds ?? [])];

            if (VotesClient.Track(trackablePluginIds))
                LogFile.WriteLine("List of enabled plugins has been sent to the statistics server");
            else
                LogFile.Error(
                    "Failed to send the list of enabled plugins to the statistics server"
                );
        });
    }
}
