using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar.Shared.Data;

namespace Pulsar.Shared.Votes.Model;

// Votes and usage counts for all plugins
public class PluginVotes
{
    // Key: pluginId
    // Serialized as "Stats" to preserve API compatibility with the backend server storing the votes
    [JsonProperty("Stats")]
    public Dictionary<string, PluginVote> Votes { get; set; } = [];

    // Token the player is required to present for voting (making it harder to spoof votes)
    public string VotingToken { get; set; }

    public PluginVote GetVotesForPlugin(PluginData data)
    {
        if (Votes.TryGetValue(data.Id, out PluginVote result))
            return result;
        return new PluginVote();
    }
}
