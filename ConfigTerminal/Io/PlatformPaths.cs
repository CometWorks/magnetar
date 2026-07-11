using System;
using System.Runtime.InteropServices;

namespace Magnetar.ConfigTerminal.Io;

/// <summary>
/// Cross-target platform helpers. <see cref="OperatingSystem"/> is unavailable
/// on net48, so OS detection goes through <see cref="RuntimeInformation"/>,
/// which both target frameworks support.
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

    /// <summary>
    /// Path.GetRelativePath equivalent that also compiles on net48 (whose BCL
    /// lacks it). Returns <paramref name="path"/> unchanged if it is not under
    /// <paramref name="relativeTo"/>.
    /// </summary>
    public static string GetRelativePath(string relativeTo, string path)
    {
#if NETFRAMEWORK
        string base_ = System.IO.Path.GetFullPath(relativeTo)
            .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
            + System.IO.Path.DirectorySeparatorChar;
        string target = System.IO.Path.GetFullPath(path);
        var baseUri = new Uri(base_);
        var targetUri = new Uri(target);
        string rel = Uri.UnescapeDataString(baseUri.MakeRelativeUri(targetUri).ToString());
        return rel.Replace('/', System.IO.Path.DirectorySeparatorChar);
#else
        return System.IO.Path.GetRelativePath(relativeTo, path);
#endif
    }
}
