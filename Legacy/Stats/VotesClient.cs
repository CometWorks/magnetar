using System;
using System.Net.Http;
using System.Text;
using Magnetar.Legacy.Stats.Model;
using Newtonsoft.Json;
using Pulsar.Shared;

namespace Magnetar.Legacy.Stats;

/// <summary>
/// Minimal client for the Magnetar statistics server (consent registration and
/// anonymous plugin usage tracking). Pulsar's own StatsClient is unusable on a
/// dedicated server because it derives the player identity from the Steam
/// client API, which Magnetar never initializes; identity here is the local
/// instance.id managed by <see cref="ConsentManager"/>.
/// </summary>
public static class VotesClient
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    // API address
    public static string BaseUrl { get; set; }

    // API endpoints
    private static string ConsentUri => $"{BaseUrl}/Consent";
    private static string TrackUri => $"{BaseUrl}/Track";

    public static bool Consent(bool consent, string playerHash = null)
    {
        playerHash ??= ConsentManager.PlayerHash;

        if (consent)
            LogFile.WriteLine("Registering consent on the statistics server");
        else
            LogFile.WriteLine(
                "Withdrawing consent, removing this instance's data from the statistics server"
            );

        var consentRequest = new ConsentRequest() { PlayerHash = playerHash, Consent = consent };
        return Post(ConsentUri, consentRequest);
    }

    public static bool Track(string[] pluginIds)
    {
        var trackRequest = new TrackRequest
        {
            PlayerHash = ConsentManager.PlayerHash,
            EnabledPluginIds = pluginIds,
        };

        return Post(TrackUri, trackRequest);
    }

    private static bool Post<T>(string url, T request)
    {
        try
        {
            using HttpClient client = new() { Timeout = Timeout };
            string json = JsonConvert.SerializeObject(request);
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = client
                .PostAsync(url, content)
                .GetAwaiter()
                .GetResult();
            return response.IsSuccessStatusCode;
        }
        catch (Exception e)
        {
            LogFile.Error($"REST API request failed: POST {url} [{e.Message}]");
            return false;
        }
    }
}
