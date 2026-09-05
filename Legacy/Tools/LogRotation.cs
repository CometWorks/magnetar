using System;
using System.IO;
using System.Linq;

namespace Magnetar.Legacy;

/// <summary>
/// Pulsar's LogFile overwrites a single info.log on every launch. Dedicated
/// servers restart often (updates, world reloads, crashes), so the previous
/// log is renamed to info_yyyyMMdd_HHmmss.log (from its last write time)
/// before LogFile.Init recreates info.log. Old rotated logs are pruned so an
/// unattended server cannot fill the disk. Fail-soft: rotation problems only
/// cost history, never startup.
/// </summary>
internal static class LogRotation
{
    private const string CurrentName = "info.log";
    private const string RotatedPattern = "info_????????_??????*.log";
    private const int KeepRotated = 20;

    public static void RotatePrevious(string magnetarDir)
    {
        try
        {
            string current = Path.Combine(magnetarDir, CurrentName);
            if (File.Exists(current))
            {
                DateTime stamp = File.GetLastWriteTime(current);
                string rotated = Path.Combine(
                    magnetarDir,
                    $"info_{stamp:yyyyMMdd_HHmmss}.log"
                );

                // A same-second restart collides; add a numeric suffix.
                for (int i = 1; File.Exists(rotated); i++)
                    rotated = Path.Combine(
                        magnetarDir,
                        $"info_{stamp:yyyyMMdd_HHmmss}_{i}.log"
                    );

                File.Move(current, rotated);
            }

            Prune(magnetarDir);
        }
        catch (Exception)
        {
            // LogFile is not initialized yet; there is nowhere to report this.
        }
    }

    private static void Prune(string magnetarDir)
    {
        var stale = Directory
            .EnumerateFiles(magnetarDir, RotatedPattern)
            .OrderByDescending(File.GetLastWriteTime)
            .Skip(KeepRotated);

        foreach (string file in stale)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception)
            {
                // Best effort; a locked or unreadable file is left in place.
            }
        }
    }
}
