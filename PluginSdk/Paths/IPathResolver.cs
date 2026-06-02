namespace PluginSdk.Paths
{
    /// <summary>
    /// Backend contract behind <see cref="PathResolver"/>. The default
    /// implementation is the no-op <see cref="ShimPathResolver"/> (correct on a
    /// case-insensitive OS like Windows). On Linux the host installs a backend
    /// that forwards to the LinuxCompat plugin's case-insensitive path cache, so
    /// plugins write one code path that works on both platforms.
    /// </summary>
    public interface IPathResolver
    {
        /// <summary>
        /// Normalizes a path: converts backslashes to forward slashes and trims
        /// whitespace. On a case-insensitive OS this is a no-op.
        /// </summary>
        string Normalize(string path);

        /// <summary>
        /// Converts a (Linux) path to a Windows-shape path for read-only egress
        /// to mods. On a case-insensitive OS this is a no-op.
        /// </summary>
        string ToWindowsPath(string path);

        /// <summary>
        /// Cross-platform replacement for <c>System.IO.Path.GetFileName</c> that
        /// treats <c>\</c> as a separator even on Linux.
        /// </summary>
        string GetFileName(string path);

        /// <summary>
        /// Cross-platform replacement for
        /// <c>System.IO.Path.GetFileNameWithoutExtension</c>.
        /// </summary>
        string GetFileNameWithoutExtension(string path);

        /// <summary>
        /// Resolves a content-relative file path against an explicit root,
        /// matching directory/file casing case-insensitively where supported.
        /// </summary>
        string ResolveContentFilePath(string relativePath, string rootPath);

        /// <summary>
        /// Resolves an absolute path to its real on-disk casing. Returns the
        /// input unchanged on a miss or on a case-insensitive OS.
        /// </summary>
        string ResolveAbsolute(string absolutePath);
    }
}
