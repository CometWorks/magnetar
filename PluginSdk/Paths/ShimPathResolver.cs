using System.IO;

namespace PluginSdk.Paths
{
    /// <summary>
    /// Default <see cref="IPathResolver"/>: a no-op pass-through that is correct
    /// on a case-insensitive filesystem (Windows). Used whenever the LinuxCompat
    /// path cache is not loaded, so plugins can call <see cref="PathResolver"/>
    /// unconditionally.
    /// </summary>
    internal sealed class ShimPathResolver : IPathResolver
    {
        public string Normalize(string path) => path;

        public string ToWindowsPath(string path) => path;

        public string GetFileName(string path)
            => string.IsNullOrEmpty(path) ? path : Path.GetFileName(path);

        public string GetFileNameWithoutExtension(string path)
            => string.IsNullOrEmpty(path) ? path : Path.GetFileNameWithoutExtension(path);

        public string ResolveContentFilePath(string relativePath, string rootPath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return relativePath;
            if (Path.IsPathRooted(relativePath))
                return relativePath;
            if (!string.IsNullOrEmpty(rootPath))
                return Path.Combine(rootPath, relativePath);
            return relativePath;
        }

        public string ResolveAbsolute(string absolutePath) => absolutePath;
    }
}
