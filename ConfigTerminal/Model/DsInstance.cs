using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>The identity of the one instance the tool is bound to for the session.</summary>
internal sealed class InstanceBinding
{
    public string DataDir;             // -path  (DS UserDataPath: cfg + Saves)
    public string MagnetarConfigDir;   // -config (Magnetar state, logs, pid)
    public string MagnetarExePath;     // launcher to spawn
    public string Ds64Dir;             // DS install (templates; auto-detected)

    public string ConfigPath => Path.Combine(DataDir, "SpaceEngineers-Dedicated.cfg");
    public string SavesPath => Path.Combine(DataDir, "Saves");
    public string PidFilePath => Path.Combine(MagnetarConfigDir ?? string.Empty, "magnetar.pid");
}

/// <summary>Non-fatal problems found while opening an instance, surfaced in the UI.</summary>
internal sealed class InstanceProblems
{
    public List<string> Messages { get; } = new();
    public bool Any => Messages.Count > 0;
    public void Add(string m) => Messages.Add(m);
}

/// <summary>
/// Aggregate root binding the cfg, worlds, templates and last-session together.
/// Opening never throws for content problems — they are recorded so the UI can
/// open anything and show what is wrong (a config tool must be able to repair a
/// broken instance).
/// </summary>
internal sealed class DsInstance
{
    public InstanceBinding Binding { get; private set; }
    public DedicatedConfigDocument Config { get; private set; }
    public WorldCatalog Worlds { get; private set; }
    public WorldTemplateCatalog Templates { get; private set; }
    public LastSessionFile LastSession { get; private set; }
    public InstanceProblems Problems { get; } = new();

    public static DsInstance Open(InstanceBinding binding)
    {
        var instance = new DsInstance { Binding = binding };
        instance.Reload();
        return instance;
    }

    public void Reload()
    {
        try
        {
            Config = DedicatedConfigDocument.Open(Binding.ConfigPath);
        }
        catch (Exception e)
        {
            Problems.Add($"Could not read {Path.GetFileName(Binding.ConfigPath)}: {e.Message}");
        }

        if (!Directory.Exists(Binding.SavesPath))
            Problems.Add($"Saves folder does not exist yet: {Binding.SavesPath}");

        Worlds = new WorldCatalog(Binding.SavesPath);
        Worlds.Scan();

        Templates = WorldTemplateCatalog.Scan(Binding.Ds64Dir);
        if (Binding.Ds64Dir == null || Templates.Templates.Count == 0)
            Problems.Add("No world templates found (DS install not located); new-world creation is disabled.");

        LastSession = LastSessionFile.Read(LastSessionFile.PathFor(Binding.SavesPath));
        ResolveActiveWorld();
    }

    /// <summary>Marks the world the DS would load next per the LastSession precedence rules (§2.3).</summary>
    private void ResolveActiveWorld()
    {
        foreach (WorldInfo w in Worlds.Worlds)
            w.IsActive = false;

        bool ignore = Config?.IgnoreLastSession ?? false;
        if (!ignore && LastSession != null)
        {
            WorldInfo match = MatchLastSession();
            if (match != null)
            {
                match.IsActive = true;
                return;
            }
        }

        // Fall back to cfg LoadWorld (bare names resolve under Saves/).
        string loadWorld = Config?.LoadWorld;
        if (!string.IsNullOrEmpty(loadWorld))
        {
            WorldInfo match = Worlds.Find(Path.GetFileName(loadWorld.TrimEnd('/', '\\')));
            if (match != null)
                match.IsActive = true;
        }
    }

    private WorldInfo MatchLastSession()
    {
        if (!string.IsNullOrEmpty(LastSession.RelativePath))
        {
            string name = Path.GetFileName(LastSession.RelativePath.TrimEnd('/', '\\'));
            WorldInfo byRel = Worlds.Find(name);
            if (byRel != null)
                return byRel;
        }
        if (!string.IsNullOrEmpty(LastSession.Path))
        {
            string name = Path.GetFileName(LastSession.Path.TrimEnd('/', '\\'));
            return Worlds.Find(name);
        }
        return null;
    }

    public WorldInfo ActiveWorld => Worlds?.Worlds.FirstOrDefault(w => w.IsActive);
}
