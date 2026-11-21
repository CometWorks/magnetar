using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Magnetar.Legacy;

/// <summary>
/// The dedicated server starts a new SpaceEngineersDedicated_&lt;date&gt;.log on
/// every launch and, called without a max log age, never deletes the old ones.
/// Servers that restart often (cluster nodes rotate every few hours) collect
/// hundreds of them. This deletes the logs older than the age limit, then the
/// oldest ones until the rest, the active log included, fit the size limit.
/// The active log is never deleted. A limit of 0 turns that check off.
/// </summary>
internal static class DedicatedLogRetention
{
    public const int DefaultMaxAgeDays = 14;
    public const int DefaultMaxSizeMB = 1024;

    public static List<string> Prune(
        string directory,
        string pattern,
        string activeLog,
        int maxAgeDays,
        long maxTotalBytes,
        DateTime utcNow
    )
    {
        string active = activeLog is null ? null : Path.GetFullPath(activeLog);
        long activeBytes =
            active is not null && File.Exists(active) ? new FileInfo(active).Length : 0;
        DateTime cutoff = utcNow.AddDays(-maxAgeDays);

        List<string> deleted = [];
        long total = activeBytes;
        var candidates = new DirectoryInfo(directory)
            .EnumerateFiles(pattern)
            .Where(file =>
                !string.Equals(file.FullName, active, StringComparison.OrdinalIgnoreCase)
            )
            .OrderByDescending(file => file.LastWriteTimeUtc);
        foreach (FileInfo file in candidates)
        {
            total += file.Length;
            bool tooOld = maxAgeDays > 0 && file.LastWriteTimeUtc < cutoff;
            bool overSize = maxTotalBytes > 0 && total > maxTotalBytes;
            if (!tooOld && !overSize)
                continue;
            try
            {
                file.Delete();
                deleted.Add(file.Name);
                total -= file.Length;
            }
            catch (Exception)
            {
                // Best effort; a locked or unreadable log is left in place.
            }
        }
        return deleted;
    }
}
