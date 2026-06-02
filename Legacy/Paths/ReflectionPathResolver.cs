using System;
using System.Reflection;
using PluginSdk.Paths;

namespace Pulsar.Legacy.Paths;

/// <summary>
/// <see cref="IPathResolver"/> backend that forwards to the LinuxCompat plugin's
/// case-insensitive path cache. The reflection cost is paid once in the
/// constructor (binding strongly-typed delegates); every later call is a plain
/// delegate invocation. Built by <see cref="PathResolverBinder"/> when the
/// LinuxCompat types are present.
/// </summary>
internal sealed class ReflectionPathResolver : IPathResolver
{
    private readonly Func<string, string> _normalize;
    private readonly Func<string, string> _toWindowsPath;
    private readonly Func<string, string> _getFileName;
    private readonly Func<string, string> _getFileNameWithoutExtension;
    private readonly Func<string, string, string> _resolveContentFilePath;
    private readonly Func<string, string> _resolveAbsolute;

    private ReflectionPathResolver(
        Func<string, string> normalize,
        Func<string, string> toWindowsPath,
        Func<string, string> getFileName,
        Func<string, string> getFileNameWithoutExtension,
        Func<string, string, string> resolveContentFilePath,
        Func<string, string> resolveAbsolute)
    {
        _normalize = normalize;
        _toWindowsPath = toWindowsPath;
        _getFileName = getFileName;
        _getFileNameWithoutExtension = getFileNameWithoutExtension;
        _resolveContentFilePath = resolveContentFilePath;
        _resolveAbsolute = resolveAbsolute;
    }

    /// <summary>
    /// Binds all six operations from <paramref name="pathHelpers"/> (the five
    /// PathHelpers methods) and <paramref name="pathCache"/>
    /// (ResolveAbsolute). Returns null if any method is missing — the caller
    /// then leaves the shim in place rather than wiring a partial backend.
    /// </summary>
    public static ReflectionPathResolver TryCreate(Type pathHelpers, Type pathCache)
    {
        if (pathHelpers == null || pathCache == null)
            return null;

        Func<string, string> normalize = Bind1(pathHelpers, "Normalize");
        Func<string, string> toWindowsPath = Bind1(pathHelpers, "ToWindowsPath");
        Func<string, string> getFileName = Bind1(pathHelpers, "GetFileName");
        Func<string, string> getFileNameWithoutExtension = Bind1(pathHelpers, "GetFileNameWithoutExtension");
        Func<string, string, string> resolveContentFilePath = Bind2(pathHelpers, "ResolveContentFilePath");
        Func<string, string> resolveAbsolute = Bind1(pathCache, "ResolveAbsolute");

        if (normalize == null || toWindowsPath == null || getFileName == null ||
            getFileNameWithoutExtension == null || resolveContentFilePath == null ||
            resolveAbsolute == null)
            return null;

        return new ReflectionPathResolver(
            normalize, toWindowsPath, getFileName, getFileNameWithoutExtension,
            resolveContentFilePath, resolveAbsolute);
    }

    private static Func<string, string> Bind1(Type type, string name)
    {
        MethodInfo m = type.GetMethod(
            name, BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(string) }, null);
        if (m == null || m.ReturnType != typeof(string))
            return null;
        return (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), m);
    }

    private static Func<string, string, string> Bind2(Type type, string name)
    {
        MethodInfo m = type.GetMethod(
            name, BindingFlags.Public | BindingFlags.Static, null,
            new[] { typeof(string), typeof(string) }, null);
        if (m == null || m.ReturnType != typeof(string))
            return null;
        return (Func<string, string, string>)Delegate.CreateDelegate(typeof(Func<string, string, string>), m);
    }

    public string Normalize(string path) => _normalize(path);
    public string ToWindowsPath(string path) => _toWindowsPath(path);
    public string GetFileName(string path) => _getFileName(path);
    public string GetFileNameWithoutExtension(string path) => _getFileNameWithoutExtension(path);
    public string ResolveContentFilePath(string relativePath, string rootPath)
        => _resolveContentFilePath(relativePath, rootPath);
    public string ResolveAbsolute(string absolutePath) => _resolveAbsolute(absolutePath);
}
