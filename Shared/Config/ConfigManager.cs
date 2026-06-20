using System;
using System.IO;
using System.Threading.Tasks;
using Pulsar.Shared.Votes;
using Pulsar.Shared.Votes.Model;

namespace Pulsar.Shared.Config;

public class ConfigManager
{
    public const string HarmonyVersion = "2.4.2.0";

    public static ConfigManager Instance { get; private set; }

    public PluginList List { get; private set; }
    public CoreConfig Core { get; private set; }
    public SourcesConfig Sources { get; private set; }
    public ProfilesConfig Profiles { get; private set; }
    public PluginVotes Votes { get; private set; }
    public Version GameVersion { get; private set; }

    public string PulsarDir { get; private set; }
    public string GameDir { get; private set; }
    public string ModDir { get; private set; }

    public bool SafeMode { get; set; }
    public bool HasLocal { get; set; }

    public static void EarlyInit(string pulsarDir)
    {
        Instance = new()
        {
            SafeMode = false,
            PulsarDir = pulsarDir,
            Core = CoreConfig.Load(pulsarDir),
        };
    }

    public static void Init(
        string gameDir,
        string modDir,
        Version gameVersion,
        RemoteHubConfig[] defaultHubs
    )
    {
        ConfigManager i = Instance;
        i.GameDir = gameDir;
        i.ModDir = modDir;
        i.GameVersion = gameVersion;
        i.Profiles = ProfilesConfig.Load(i.PulsarDir);
        i.Sources = SourcesConfig.Load(i.PulsarDir, defaultHubs);
        i.List = new PluginList(i.PulsarDir, i.Sources, i.Profiles);
    }

    private string InstanceIdPath => Path.Combine(PulsarDir, "instance.id");

    public bool HasInstanceId() => File.Exists(InstanceIdPath);

    public string ReadInstanceId()
    {
        if (!File.Exists(InstanceIdPath))
            return null;

        return File.ReadAllText(InstanceIdPath).Trim();
    }

    public string CreateInstanceId()
    {
        if (File.Exists(InstanceIdPath))
            return File.ReadAllText(InstanceIdPath).Trim();

        string id = Guid.NewGuid().ToString("D");
        File.WriteAllText(InstanceIdPath, id);
        return id;
    }

    public void DeleteInstanceId()
    {
        if (File.Exists(InstanceIdPath))
            File.Delete(InstanceIdPath);
    }

    public void UpdatePlayerVotes()
    {
        Task.Run(() =>
        {
            Votes = VotesClient.DownloadVotes();
        });
    }
}
