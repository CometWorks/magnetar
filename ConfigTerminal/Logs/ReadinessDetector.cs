using System;
using System.IO;
using System.Text;

namespace Magnetar.ConfigTerminal.Logs;

/// <summary>
/// Detects that the DS has finished loading a world by scanning the game log for
/// its readiness marker ("Game ready", §2.9). Reads only the tail of the file so
/// it stays cheap when polled during world creation, and never throws for IO
/// problems (a missing/locked log simply reads as "not ready yet").
/// </summary>
internal static class ReadinessDetector
{
    /// <summary>The DS prints this line once the session is loaded and joinable.</summary>
    private const string Marker = "Game ready";

    private const int TailBytes = 64 * 1024;

    public static bool IsReady(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            long start = Math.Max(0, fs.Length - TailBytes);
            fs.Seek(start, SeekOrigin.Begin);

            using var reader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
            string tail = reader.ReadToEnd();
            return tail.IndexOf(Marker, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
