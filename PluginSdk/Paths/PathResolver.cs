namespace PluginSdk.Paths
{
    /// <summary>
    /// Plugin-facing facade for case-insensitive path resolution. Plugins call
    /// these methods unconditionally; the active backend is a no-op shim on a
    /// case-insensitive OS (Windows) and the LinuxCompat case-insensitive path
    /// cache on Linux. The host swaps the backend once at startup via
    /// <see cref="Install"/>; until then the shim is in effect.
    /// </summary>
    public static class PathResolver
    {
        private static IPathResolver _backend = new ShimPathResolver();

        /// <summary>
        /// True when a real case-insensitive resolver (e.g. LinuxCompat) is wired
        /// in; false while the no-op shim is active.
        /// </summary>
        public static bool IsCaseInsensitiveResolverActive => !(_backend is ShimPathResolver);

        /// <summary>
        /// Installs the active backend. Host-only; called once at startup before
        /// plugin hot paths run. Last-wins; a null argument resets to the shim.
        /// </summary>
        public static void Install(IPathResolver backend)
            => _backend = backend ?? new ShimPathResolver();

        /// <inheritdoc cref="IPathResolver.Normalize"/>
        public static string Normalize(string path) => _backend.Normalize(path);

        /// <inheritdoc cref="IPathResolver.ToWindowsPath"/>
        public static string ToWindowsPath(string path) => _backend.ToWindowsPath(path);

        /// <inheritdoc cref="IPathResolver.GetFileName"/>
        public static string GetFileName(string path) => _backend.GetFileName(path);

        /// <inheritdoc cref="IPathResolver.GetFileNameWithoutExtension"/>
        public static string GetFileNameWithoutExtension(string path)
            => _backend.GetFileNameWithoutExtension(path);

        /// <inheritdoc cref="IPathResolver.ResolveContentFilePath"/>
        public static string ResolveContentFilePath(string relativePath, string rootPath)
            => _backend.ResolveContentFilePath(relativePath, rootPath);

        /// <inheritdoc cref="IPathResolver.ResolveAbsolute"/>
        public static string ResolveAbsolute(string absolutePath)
            => _backend.ResolveAbsolute(absolutePath);
    }
}
