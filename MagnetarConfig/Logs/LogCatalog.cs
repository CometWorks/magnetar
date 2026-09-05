using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Magnetar.Config.Model;

namespace Magnetar.Config.Logs;

/// <summary>Which log stream a file belongs to (§2.9).</summary>
internal enum LogGroup
{
    Game,
    Magnetar,
}

/// <summary>One discovered log file, with the metadata the viewer sorts and marks by.</summary>
internal sealed class LogFileInfo
{
    public string Path;
    public LogGroup Group;
    public DateTime LastWrite;
    public long Size;
    public bool IsActive; // info.log (current launch), or newest game log

    /// <summary>Bare file name shown in the selector.</summary>
    public string Name => System.IO.Path.GetFileName(Path);

    /// <summary>
    /// Compact label for the selector: if the file name embeds a
    /// <c>yyyyMMdd_HHmmssfff</c> timestamp, show only <c>yyyyMMdd_HHmmss</c>
    /// (prefix, extension and milliseconds dropped). Otherwise the bare name.
    /// </summary>
    public string DisplayName
    {
        get
        {
            Match m = Regex.Match(Name, @"(\d{8}_\d{6})\d*");
            return m.Success ? m.Groups[1].Value : Name;
        }
    }
}

/// <summary>
/// Discovers the game and Magnetar log files for the bound instance (§2.9) and
/// marks the active file of each group: the game log named
/// <c>SpaceEngineersDedicated*.log</c> in the DS data dir (newest is active) and
/// the Magnetar logs in the config dir — <c>info.log</c> is the current
/// launch's (active) log, and <c>info_*.log</c> are the rotated logs of
/// previous launches. Pure filesystem discovery — no Terminal.Gui.
/// </summary>
internal sealed class LogCatalog
{
    private readonly InstanceBinding binding;
    private readonly List<LogFileInfo> files = new();

    public LogCatalog(InstanceBinding binding) => this.binding = binding;

    /// <summary>All discovered log files, newest first.</summary>
    public IReadOnlyList<LogFileInfo> Files => files;

    /// <summary>The active game log (where "Game ready" appears), or null if none exist.</summary>
    public LogFileInfo ActiveGameLog =>
        files.FirstOrDefault(f => f.Group == LogGroup.Game && f.IsActive)
        ?? files.FirstOrDefault(f => f.Group == LogGroup.Game);

    /// <summary>Re-scans both log directories; safe to call repeatedly (follow/refresh).</summary>
    public void Scan()
    {
        files.Clear();
        ScanGameLogs();
        ScanMagnetarLogs();
        files.Sort((a, b) => b.LastWrite.CompareTo(a.LastWrite));
    }

    private void ScanGameLogs()
    {
        List<LogFileInfo> game = Discover(binding?.DataDir, "SpaceEngineersDedicated*.log", LogGroup.Game);
        // The DS overwrites SpaceEngineersDedicated.log per start; the newest wins.
        LogFileInfo newest = game.OrderByDescending(f => f.LastWrite).FirstOrDefault();
        if (newest != null)
            newest.IsActive = true;
        files.AddRange(game);
    }

    private void ScanMagnetarLogs()
    {
        // info.log is the current launch's log; info_*.log are the rotated
        // logs of previous launches (the launcher renames info.log on start).
        string dir = binding?.MagnetarConfigDir;
        List<LogFileInfo> magnetar = Discover(dir, "info*.log", LogGroup.Magnetar);

        foreach (LogFileInfo f in magnetar)
            f.IsActive = string.Equals(f.Name, "info.log", StringComparison.OrdinalIgnoreCase);
        files.AddRange(magnetar);
    }

    private static List<LogFileInfo> Discover(string dir, string pattern, LogGroup group)
    {
        var result = new List<LogFileInfo>();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return result;

        string[] paths;
        try
        {
            paths = Directory.GetFiles(dir, pattern);
        }
        catch (IOException) { return result; }
        catch (UnauthorizedAccessException) { return result; }

        foreach (string path in paths)
        {
            FileInfo fi;
            try { fi = new FileInfo(path); }
            catch (IOException) { continue; }
            catch (UnauthorizedAccessException) { continue; }

            result.Add(new LogFileInfo
            {
                Path = path,
                Group = group,
                LastWrite = fi.LastWriteTimeUtc,
                Size = fi.Exists ? fi.Length : 0,
            });
        }
        return result;
    }

}
