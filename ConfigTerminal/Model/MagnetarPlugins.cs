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
