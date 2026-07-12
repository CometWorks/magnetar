using System;
using System.Runtime.InteropServices;

namespace Magnetar.ConfigTerminal.Io;

/// <summary>
/// Platform helpers: OS detection and filesystem-correct path comparison.
/// </summary>
internal static class PlatformPaths
{
    public static bool IsWindows { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public static bool IsLinux { get; } =
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// File-path comparer: case-insensitive on Windows, case-sensitive on Linux,
    /// matching real filesystem semantics for world-folder identity checks.
    /// </summary>
    public static StringComparer PathComparer { get; } =
        IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static StringComparison PathComparison { get; } =
        IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static string GetRelativePath(string relativeTo, string path) =>
        System.IO.Path.GetRelativePath(relativeTo, path);
}
