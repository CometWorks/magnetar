using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Magnetar.ConfigTerminal.Io;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>A DLL discovered in the Magnetar instance's Local/ folder.</summary>
internal sealed class LocalDllInfo
{
    public string FileName;   // == the plugin Id (filename WITH extension)
    public string FullPath;
    public bool Enabled;
}

/// <summary>An enabled dev-folder plugin, joined with its source folder.</summary>
internal sealed class DevFolderPlugin
{
    public string Id;         // folder name
    public string Folder;     // absolute source folder (from sources.xml), may be null if orphaned
    public string DataFile;   // manifest filename
    public bool DebugBuild;
    public bool SourceMissing; // profile enables it but no sources entry (or folder gone)
}

/// <summary>A hub/remote plugin from a cached catalog, joined with its enabled state.</summary>
internal sealed class HubPluginView
{
    public HubPluginInfo Info;
    public bool Enabled;      // named in Profile.GitHub
    public string Id => Info.Id;
}

/// <summary>A Magnetar mod source, joined with its active (Profile.Mods) state.</summary>
internal sealed class ModView
{
    public long Id;
    public string Name;
    public bool SourceEnabled;   // ModSources <Enabled>
    public bool InProfile;       // id present in Profile.Mods
    public bool IsDependency;    // marked as a resolved dependency (name suffix)
    public bool Active => SourceEnabled && InProfile;
}

/// <summary>
/// Facade over Magnetar's plugin config for one instance: the active profile
/// (enabled set) and the dev-folder sources. Enables/disables local DLLs (from
/// the Local/ folder) and dev-folder plugins (Quasar-style: pick a manifest XML;
/// the folder + filename + folder-name id are derived). All writes go through
/// AtomicFile (backup + atomic replace), matching Magnetar's .bak convention.
/// </summary>
internal sealed class MagnetarPlugins
{
    // Infrastructure DLLs Magnetar deploys into Local/ — not user plugins.
    private static readonly HashSet<string> ImplicitIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "0Harmony.dll", "Magnetar.Protocol.dll", "Quasar.Agent.dll",
    };

    private readonly string configDir;
    private readonly AtomicFile writer;
    private PluginProfileDocument profile;
    private PluginSourcesDocument sources;

    public MagnetarPlugins(string magnetarConfigDir, AtomicFile writer)
    {
        configDir = magnetarConfigDir;
        this.writer = writer;
        Reload();
    }

    public string LocalDir => Path.Combine(configDir, "Local");

    public void Reload()
    {
        profile = PluginProfileDocument.Open(configDir);
        sources = PluginSourcesDocument.Open(configDir);
    }

    // --- local DLLs ---

    /// <summary>DLLs in Local/ (recursive), excluding infrastructure, with their enabled state.</summary>
    public IReadOnlyList<LocalDllInfo> LocalDlls()
    {
        var enabled = new HashSet<string>(profile.LocalDlls, StringComparer.OrdinalIgnoreCase);
        var result = new List<LocalDllInfo>();

        if (Directory.Exists(LocalDir))
        {
            foreach (string path in SafeEnumerate(LocalDir))
            {
                string name = Path.GetFileName(path);
                if (ImplicitIds.Contains(name))
                    continue;
                result.Add(new LocalDllInfo { FileName = name, FullPath = path, Enabled = enabled.Contains(name) });
            }
        }

        // Surface enabled entries whose DLL is missing so the user can clean them up.
        foreach (string name in profile.LocalDlls)
        {
            if (ImplicitIds.Contains(name))
                continue;
            if (!result.Any(r => string.Equals(r.FileName, name, StringComparison.OrdinalIgnoreCase)))
                result.Add(new LocalDllInfo { FileName = name, FullPath = null, Enabled = true });
        }

        return result.OrderBy(r => r.FileName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public bool SetLocalDllEnabled(string dllFileName, bool enabled)
    {
        bool changed = enabled ? profile.EnableLocalDll(dllFileName) : profile.DisableLocalDll(dllFileName);
        if (changed)
            profile.Save(writer);
        return changed;
    }

    // --- dev folders ---

    public IReadOnlyList<DevFolderPlugin> DevFolderPlugins()
    {
        var srcById = sources.LocalPlugins
            .Where(s => s.Folder != null)
            .GroupBy(s => Path.GetFileName(s.Folder.TrimEnd('/', '\\')), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = new List<DevFolderPlugin>();
        foreach (DevFolderEntry e in profile.DevFolders)
        {
            srcById.TryGetValue(e.Id ?? string.Empty, out LocalPluginSource src);
            result.Add(new DevFolderPlugin
            {
                Id = e.Id,
                DataFile = e.DataFile,
                DebugBuild = e.DebugBuild,
                Folder = src?.Folder,
                SourceMissing = src == null || !Directory.Exists(src.Folder ?? string.Empty),
            });
        }
        return result;
    }

    /// <summary>
    /// Adds a dev-folder plugin from a picked manifest XML: registers the folder
    /// as a source and enables it in the profile. Folder = the manifest's
    /// directory; Id/Name = that folder's name (matching Magnetar's
    /// LocalFolderPlugin identity). Returns the resolved folder name (id).
    /// </summary>
    public string AddDevFolderFromManifest(string manifestXmlPath)
    {
        if (string.IsNullOrEmpty(manifestXmlPath) || !File.Exists(manifestXmlPath))
            throw new FileNotFoundException("Manifest XML not found.", manifestXmlPath);

        string folder = Path.GetFullPath(Path.GetDirectoryName(manifestXmlPath));
        string dataFile = Path.GetFileName(manifestXmlPath);
        string id = Path.GetFileName(folder.TrimEnd('/', '\\'));

        sources.AddLocalPlugin(id, folder, enabled: true);
        sources.Save(writer);

        profile.EnableDevFolder(id, dataFile, debugBuild: true);
        profile.Save(writer);
        return id;
    }

    /// <summary>Disables a dev-folder plugin and removes its source entry.</summary>
    public void RemoveDevFolder(DevFolderPlugin plugin)
    {
        bool changed = profile.DisableDevFolder(plugin.Id);
        if (changed)
            profile.Save(writer);

        if (plugin.Folder != null && sources.RemoveByFolder(plugin.Folder))
            sources.Save(writer);
    }

    // --- hub / remote plugins ---

    public string HubDir => Path.Combine(configDir, "Sources", "Hubs");
    public string RemotePluginDir => Path.Combine(configDir, "Sources", "Plugins");

    /// <summary>
    /// The browsable plugin catalog: every plugin from the cached hub/plugin
    /// blobs (<c>Sources/Hubs/*.bin</c>, <c>Sources/Plugins/*.bin</c>) joined with
    /// its enabled state (named in <c>Profile.GitHub</c>). Obsolete and hidden
    /// entries are dropped; duplicates across sources are merged by Id. Offline —
    /// it reads only what Magnetar has already downloaded.
    /// </summary>
    public IReadOnlyList<HubPluginView> HubCatalogPlugins()
    {
        var enabled = new HashSet<string>(profile.GitHubPlugins, StringComparer.OrdinalIgnoreCase);
        var byId = new Dictionary<string, HubPluginInfo>(StringComparer.OrdinalIgnoreCase);

        foreach ((string dir, IReadOnlyList<(string repo, string name)> labels) in HubBinFiles())
        {
            if (!Directory.Exists(dir))
                continue;
            foreach (string bin in SafeEnumerateBin(dir))
            {
                string label = LabelForBin(bin, labels);
                foreach (HubPluginInfo p in HubCatalog.ReadFile(bin, label))
                {
                    if (p.Kind == HubPluginKind.Obsolete || p.Hidden)
                        continue;
                    if (!byId.ContainsKey(p.Id))
                        byId[p.Id] = p;
                }
            }
        }

        return byId.Values
            .Select(p => new HubPluginView { Info = p, Enabled = enabled.Contains(p.Id) })
            .OrderBy(v => v.Info.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Enables or disables a hub plugin in the profile. Enabling also pulls in the
    /// plugin's catalog-declared dependencies (matching Magnetar's own
    /// <c>UpdateProfile</c>). Returns the ids actually enabled (for user feedback).
    /// </summary>
    public IReadOnlyList<string> SetHubPluginEnabled(string id, bool enabled)
    {
        var touched = new List<string>();
        if (enabled)
        {
            var catalog = HubCatalogPlugins().ToDictionary(v => v.Id, v => v.Info, StringComparer.OrdinalIgnoreCase);
            var toEnable = new List<string> { id };
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < toEnable.Count; i++)
            {
                string cur = toEnable[i];
                if (!seen.Add(cur))
                    continue;
                if (profile.EnableGitHub(cur))
                    touched.Add(cur);
                if (catalog.TryGetValue(cur, out HubPluginInfo info) && info.DependencyIds != null)
                    toEnable.AddRange(info.DependencyIds);
            }
        }
        else
        {
            if (profile.DisableGitHub(id))
                touched.Add(id);
        }

        if (touched.Count > 0)
            profile.Save(writer);
        return touched;
    }

    // --- plugin sources (remote hub / remote plugin / local hub) ---

    public IReadOnlyList<RemoteHubSource> RemoteHubs() => sources.RemoteHubs;
    public IReadOnlyList<RemotePluginSource> RemotePlugins() => sources.RemotePlugins;
    public IReadOnlyList<LocalHubSource> LocalHubs() => sources.LocalHubs;

    public bool AddRemoteHub(string name, string repo, string branch)
    {
        bool added = sources.AddRemoteHub(name, repo, branch);
        if (added) sources.Save(writer);
        return added;
    }

    public bool AddRemotePlugin(string name, string repo, string branch, string file)
    {
        bool added = sources.AddRemotePlugin(name, repo, branch, file);
        if (added) sources.Save(writer);
        return added;
    }

    public bool AddLocalHub(string name, string folder)
    {
        bool added = sources.AddLocalHub(name, folder);
        if (added) sources.Save(writer);
        return added;
    }

    public void RemoveRemoteHub(string repo) { if (sources.RemoveRemoteHub(repo)) sources.Save(writer); }
    public void RemoveRemotePlugin(string repo) { if (sources.RemoveRemotePlugin(repo)) sources.Save(writer); }
    public void RemoveLocalHub(string folder) { if (sources.RemoveLocalHub(folder)) sources.Save(writer); }

    public void SetRemoteHubEnabled(string repo, bool on) { if (sources.SetRemoteHubEnabled(repo, on)) sources.Save(writer); }
    public void SetRemotePluginEnabled(string repo, bool on) { if (sources.SetRemotePluginEnabled(repo, on)) sources.Save(writer); }
    public void SetLocalHubEnabled(string folder, bool on) { if (sources.SetLocalHubEnabled(folder, on)) sources.Save(writer); }

    // --- mods (ModSources joined with Profile.Mods) ---

    /// <summary>The managed mod list: ModSources entries joined with Profile.Mods membership.</summary>
    public IReadOnlyList<ModView> Mods()
    {
        var inProfile = new HashSet<ulong>(profile.Mods);
        return sources.ModSources
            .Select(m => new ModView
            {
                Id = m.Id,
                Name = m.Name,
                SourceEnabled = m.Enabled,
                InProfile = m.Id > 0 && inProfile.Contains((ulong)m.Id),
            })
            .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Adds a mod to ModSources and (when active) the profile, in lockstep.</summary>
    public void AddMod(long id, string name, bool active = true)
    {
        if (id <= 0)
            return;
        sources.AddMod(id, name, active);
        sources.Save(writer);
        if (active ? profile.EnableMod((ulong)id) : profile.DisableMod((ulong)id))
            profile.Save(writer);
    }

    public void SetModName(long id, string name)
    {
        if (sources.SetModName(id, name))
            sources.Save(writer);
    }

    /// <summary>Toggles a mod on/off, keeping ModSources.Enabled and Profile.Mods in sync.</summary>
    public void SetModActive(long id, bool active)
    {
        if (id <= 0)
            return;
        if (sources.SetModEnabled(id, active))
            sources.Save(writer);
        if (active ? profile.EnableMod((ulong)id) : profile.DisableMod((ulong)id))
            profile.Save(writer);
    }

    /// <summary>Removes a mod from both ModSources and Profile.Mods.</summary>
    public void RemoveMod(long id)
    {
        bool changed = sources.RemoveMod(id);
        if (changed) sources.Save(writer);
        if (id > 0 && profile.DisableMod((ulong)id))
            profile.Save(writer);
    }

    // --- helpers ---

    private IEnumerable<(string dir, IReadOnlyList<(string repo, string name)> labels)> HubBinFiles()
    {
        yield return (HubDir, sources.RemoteHubs.Select(h => (h.Repo, h.Name)).ToList());
        yield return (RemotePluginDir, sources.RemotePlugins.Select(p => (p.Repo, p.Name)).ToList());
    }

    private static string LabelForBin(string binPath, IReadOnlyList<(string repo, string name)> labels)
    {
        string stem = Path.GetFileNameWithoutExtension(binPath);
        foreach ((string repo, string name) in labels)
        {
            if (repo != null && string.Equals(repo.Replace('/', '-'), stem, StringComparison.OrdinalIgnoreCase))
                return name ?? stem;
        }
        return stem;
    }

    private static IEnumerable<string> SafeEnumerateBin(string dir)
    {
        try
        {
            return Directory.EnumerateFiles(dir, "*.bin", SearchOption.TopDirectoryOnly);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerate(string dir)
    {
        try
        {
            return Directory.EnumerateFiles(dir, "*.dll", SearchOption.AllDirectories);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
