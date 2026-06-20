namespace Pulsar.Shared.Votes.Model;

// Request data sent to the backend server to count usage each time the game is started
public class TrackRequest
{
    // Anonymous instance identifier (first 20 hex chars of the instance UUID)
    public string PlayerHash { get; set; }

    // Ids of enabled plugins when the game started
    public string[] EnabledPluginIds { get; set; }
}
