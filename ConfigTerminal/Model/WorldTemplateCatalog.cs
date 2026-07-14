using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>A world template shipped with the DS under Content/CustomWorlds.</summary>
internal sealed class WorldTemplate
{
    public string FolderName;
    public string FolderPath;        // absolute — becomes PremadeCheckpointPath
    public string DisplayName;       // raw SessionName (may be a MyTexts key) or folder name
    public bool HasWorldConfig;
    public bool HasCheckpoint;

    public string WorldConfigPath => Path.Combine(FolderPath, "Sandbox_config.sbc");
    public string SandboxPath => Path.Combine(FolderPath, "Sandbox.sbc");
}

/// <summary>
/// Enumerates world templates under <c>&lt;ContentPath&gt;/CustomWorlds/</c>,
/// where ContentPath is the <c>Content/</c> folder sibling to
/// <c>DedicatedServer64/</c>. Empty when the DS install is not found.
/// </summary>
internal sealed class WorldTemplateCatalog
{
    public IReadOnlyList<WorldTemplate> Templates { get; private set; } = Array.Empty<WorldTemplate>();
    public string CustomWorldsPath { get; }

    private WorldTemplateCatalog(string customWorldsPath) => CustomWorldsPath = customWorldsPath;

    public static WorldTemplateCatalog Scan(string ds64Dir)
    {
        string customWorlds = ds64Dir == null
            ? null
            : Path.GetFullPath(Path.Combine(ds64Dir, "..", "Content", "CustomWorlds"));

        var catalog = new WorldTemplateCatalog(customWorlds);
        catalog.Rescan();
        return catalog;
    }

    public void Rescan()
    {
        var list = new List<WorldTemplate>();
        if (!string.IsNullOrEmpty(CustomWorldsPath) && Directory.Exists(CustomWorldsPath))
        {
            foreach (string dir in Directory.EnumerateDirectories(CustomWorldsPath))
            {
                string sandbox = Path.Combine(dir, "Sandbox.sbc");
                string worldConfig = Path.Combine(dir, "Sandbox_config.sbc");
                bool hasCheckpoint = File.Exists(sandbox);
                bool hasWorldConfig = File.Exists(worldConfig);
                if (!hasCheckpoint && !hasWorldConfig)
                    continue;

                var tpl = new WorldTemplate
                {
                    FolderName = Path.GetFileName(dir),
                    FolderPath = Path.GetFullPath(dir),
                    HasCheckpoint = hasCheckpoint,
                    HasWorldConfig = hasWorldConfig,
                };
                tpl.DisplayName = ResolveDisplayName(tpl);
                list.Add(tpl);
            }
        }

        Templates = list.OrderBy(t => t.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ResolveDisplayName(WorldTemplate tpl)
    {
        // Template SessionNames are frequently localization keys (e.g.
        // "{LOCG:CustomWorld_StarSystem}") which the DS resolves via MyTexts. We
        // have no localization tables, so a loc key is useless as a label — fall
        // back to the folder name, which is human-readable ("Star System").
        try
        {
            string name = null;
            if (tpl.HasWorldConfig)
                name = WorldConfigDocument.Open(tpl.WorldConfigPath).SessionName;
            if (string.IsNullOrEmpty(name) && tpl.HasCheckpoint)
                name = CheckpointReader.TryRead(tpl.SandboxPath)?.SessionName;

            if (!string.IsNullOrEmpty(name) && !IsLocalizationKey(name))
                return name;
        }
        catch
        {
        }
        return tpl.FolderName;
    }

    private static bool IsLocalizationKey(string name)
    {
        string t = name.Trim();
        return t.StartsWith("{", StringComparison.Ordinal) && t.EndsWith("}", StringComparison.Ordinal);
    }

    /// <summary>Opens the template's settings as an editable (in-memory) seed document.</summary>
    public static WorldConfigDocument OpenSeed(WorldTemplate tpl)
    {
        if (tpl.HasWorldConfig)
            return WorldConfigDocument.Open(tpl.WorldConfigPath);

        // No Sandbox_config.sbc: start from an empty settings skeleton and seed
        // the name from the checkpoint so the form shows what was imported.
        var doc = WorldConfigDocument.Open(tpl.WorldConfigPath); // absent → skeleton
        doc.SeedFrom(CheckpointReader.TryRead(tpl.SandboxPath));
        return doc;
    }
}
