using System.Collections.Generic;
using Pulsar.Shared.Network;
using Pulsar.Shared.Votes.Model;

namespace Pulsar.Shared.Votes;

public static class VotesClient
{
    // API address
    public static string BaseUrl { get; set; }

    // API endpoints
    private static string ConsentUri => $"{BaseUrl}/Consent";
    private static string VotesUri => $"{BaseUrl}/Stats";
    private static string TrackUri => $"{BaseUrl}/Track";
    private static string VoteUri => $"{BaseUrl}/Vote";

    // Latest voting token received
    private static string votingToken;

    public static bool Consent(bool consent, string playerHash = null)
    {
        playerHash ??= ConsentManager.PlayerHash;

        if (consent)
            LogFile.WriteLine($"Registering player consent on the votes server");
        else
            LogFile.WriteLine(
                $"Withdrawing player consent, removing user data from the votes server"
            );

        var consentRequest = new ConsentRequest() { PlayerHash = playerHash, Consent = consent };

        return SimpleHttpClient.Post(ConsentUri, consentRequest);
    }

    // This function may be called from another thread.
    public static PluginVotes DownloadVotes()
    {
        if (!ConsentManager.Granted)
        {
            LogFile.WriteLine("Downloading plugin votes anonymously...");
            votingToken = null;
            return SimpleHttpClient.Get<PluginVotes>(VotesUri);
        }

        LogFile.WriteLine("Downloading plugin votes for " + ConsentManager.PlayerHash);

        var parameters = new Dictionary<string, string> { ["playerHash"] = ConsentManager.PlayerHash };
        var pluginVotes = SimpleHttpClient.Get<PluginVotes>(VotesUri, parameters);

        votingToken = pluginVotes?.VotingToken;

        return pluginVotes;
    }

    public static bool Track(string[] pluginIds)
    {
        var trackRequest = new TrackRequest
        {
            PlayerHash = ConsentManager.PlayerHash,
            EnabledPluginIds = pluginIds,
        };

        return SimpleHttpClient.Post(TrackUri, trackRequest);
    }

    public static PluginVote Vote(string pluginId, int vote)
    {
        if (votingToken is null)
        {
            LogFile.Error($"Voting token is not available, cannot vote");
            return null;
        }

        LogFile.WriteLine($"Voting {vote} on plugin {pluginId}");
        var voteRequest = new VoteRequest
        {
            PlayerHash = ConsentManager.PlayerHash,
            PluginId = pluginId,
            VotingToken = votingToken,
            Vote = vote,
        };

        var pluginVote = SimpleHttpClient.Post<PluginVote, VoteRequest>(VoteUri, voteRequest);
        return pluginVote;
    }
}
