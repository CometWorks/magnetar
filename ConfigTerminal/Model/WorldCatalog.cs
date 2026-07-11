using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>Display metadata for one world folder under <c>Saves/</c>.</summary>
internal sealed class WorldInfo
{
    public string FolderName;        // bare name — the identity used in RelativePath
    public string FolderPath;
    public string SessionName;
    public DateTime? LastSaveTime;
    public int ModCount;
    public bool HasWorldConfig;      // Sandbox_config.sbc present?
    public bool HasCheckpoint;       // Sandbox.sbc present? (activation guard)
    public bool IsActive;            // matches the resolved LastSession target

    public string SandboxPath => Path.Combine(FolderPath, "Sandbox.sbc");
    public string WorldConfigPath => Path.Combine(FolderPath, "Sandbox_config.sbc");
}

/// <summary>Enumerates worlds under a <c>Saves/</c> directory.</summary>
internal sealed class WorldCatalog
{
    public IReadOnlyList<WorldInfo> Worlds { get; private set; } = Array.Empty<WorldInfo>();
    public string SavesPath { get; }

    public WorldCatalog(string savesPath) => SavesPath = savesPath;

    /// <summary>(Re)scans the saves folder, sorted by last-save time descending.</summary>
    public void Scan()
    {
        var list = new List<WorldInfo>();
        if (Directory.Exists(SavesPath))
        {
            foreach (string dir in Directory.EnumerateDirectories(SavesPath))
            {
                string name = Path.GetFileName(dir);
                if (string.Equals(name, "Backup", StringComparison.OrdinalIgnoreCase))
                    continue;

                string sandbox = Path.Combine(dir, "Sandbox.sbc");
                string worldConfig = Path.Combine(dir, "Sandbox_config.sbc");
                bool hasCheckpoint = File.Exists(sandbox);
                bool hasWorldConfig = File.Exists(worldConfig);
                if (!hasCheckpoint && !hasWorldConfig)
                    continue;

                var info = new WorldInfo
                {
                    FolderName = name,
                    FolderPath = dir,
                    HasCheckpoint = hasCheckpoint,
                    HasWorldConfig = hasWorldConfig,
                };
                Populate(info);
                list.Add(info);
            }
        }

        Worlds = list
            .OrderByDescending(w => w.LastSaveTime ?? DateTime.MinValue)
            .ThenBy(w => w.FolderName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Populate(WorldInfo info)
    {
        if (info.HasWorldConfig)
        {
            try
            {
                WorldConfigDocument doc = WorldConfigDocument.Open(info.WorldConfigPath);
                info.SessionName = doc.SessionName;
                info.LastSaveTime = doc.LastSaveTime;
                info.ModCount = doc.ReadMods().Items.Count;
            }
            catch
            {
                // fall through to checkpoint/folder name
            }
        }

        if (string.IsNullOrEmpty(info.SessionName) && info.HasCheckpoint)
        {
            CheckpointInfo cp = CheckpointReader.TryRead(info.SandboxPath);
            if (cp != null)
                info.SessionName = cp.SessionName;
        }

        if (string.IsNullOrEmpty(info.SessionName))
            info.SessionName = info.FolderName;

        if (info.LastSaveTime == null)
        {
            try { info.LastSaveTime = File.GetLastWriteTime(info.SandboxPath); } catch { }
        }
    }

    public WorldInfo Find(string folderName) =>
        Worlds.FirstOrDefault(w => Io.PlatformPaths.PathComparer.Equals(w.FolderName, folderName));
}
