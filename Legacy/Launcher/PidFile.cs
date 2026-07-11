using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Pulsar.Shared;

namespace Pulsar.Legacy.Launcher;

/// <summary>
/// Writes and removes <c>magnetar.pid</c> in the Magnetar config dir so an
/// external tool (MagnetarConfig) can discover this instance and verify the
/// running process belongs to it. Two lines: the process id, then the resolved
/// DS data directory (the <c>-path</c> value). Written once startup has passed
/// the daemon detach so the id is final; removed on every clean shutdown path.
///
/// <para>
/// A crash leaves the file behind, so readers must always re-verify the pid is
/// live and its identity matches — the file's presence alone is not proof the
/// server is running. The DS-data-dir line lets a reader confirm the process is
/// bound to the same instance it launched.
/// </para>
/// </summary>
internal static class PidFile
{
    private const string FileName = "magnetar.pid";

    // The path last written, so Delete() works without re-resolving the config
    // dir from the shutdown path (ServerControl has no reference to it).
    private static string writtenPath;

    /// <summary>
    /// Writes the pid file into <paramref name="configDir"/> (Magnetar's config
    /// directory). <paramref name="dataDir"/> is the resolved DS data dir, used
    /// as the identity line; pass null/empty when the DS runs on its default
    /// instance. Never throws — a failed write only costs external observability.
    /// </summary>
    public static void Write(string configDir, string dataDir)
    {
        if (string.IsNullOrEmpty(configDir))
            return;

        try
        {
            string path = Path.Combine(configDir, FileName);
            int pid = Process.GetCurrentProcess().Id;
            string content = pid.ToString(CultureInfo.InvariantCulture)
                             + "\n" + (dataDir ?? string.Empty) + "\n";
            File.WriteAllText(path, content);
            writtenPath = path;
            LogFile.WriteLine($"Wrote pid file {path} (pid {pid})");
        }
        catch (Exception e)
        {
            LogFile.Warn($"Could not write pid file: {e.Message}");
        }
    }

    /// <summary>Removes the pid file written by <see cref="Write"/>. Idempotent, never throws.</summary>
    public static void Delete()
    {
        string path = writtenPath;
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // A leftover pid file is treated as stale by readers; ignore.
        }
        finally
        {
            writtenPath = null;
        }
    }
}
