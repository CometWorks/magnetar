using System;
using System.Reflection;
using PluginSdk.Paths;
using Pulsar.Shared;

namespace Pulsar.Legacy.Paths;

/// <summary>
/// Wires the SDK <see cref="PathResolver"/> facade to the LinuxCompat plugin's
/// case-insensitive path cache via reflection. Called once at startup. When the
/// LinuxCompat types are absent (Windows, where the OS is already
/// case-insensitive) the SDK shim stays in place and plugins keep working
/// unchanged.
/// </summary>
internal static class PathResolverBinder
{
    private const string AssemblyName = "LinuxCompatServer";
    private const string PathHelpersTypeName = "ServerPlugin.Patches.PathHandling.PathHelpers";
    private const string PathCacheTypeName = "ServerPlugin.Patches.PathHandling.PathCache";

    /// <summary>
    /// Discovers the LinuxCompat path-cache types and installs a forwarding
    /// backend. Never throws — on any failure the shim is left active.
    /// </summary>
    public static void Bind()
    {
        try
        {
            Type pathHelpers = FindType(PathHelpersTypeName);
            Type pathCache = FindType(PathCacheTypeName);

            ReflectionPathResolver backend = ReflectionPathResolver.TryCreate(pathHelpers, pathCache);
            if (backend == null)
            {
                LogFile.WriteLine(
                    "PathResolver: LinuxCompat path cache not found, using pass-through shim");
                return;
            }

            PathResolver.Install(backend);
            LogFile.WriteLine("PathResolver: bound to LinuxCompat case-insensitive path cache");
        }
        catch (Exception e)
        {
            LogFile.Error($"PathResolver: failed to bind LinuxCompat path cache, using shim: {e}");
        }
    }

    /// <summary>
    /// Locates a type by full name. Prefers the expected LinuxCompat assembly
    /// (fast path), then falls back to scanning all loaded assemblies so a
    /// rename of the dll alone does not break binding.
    /// </summary>
    private static Type FindType(string fullName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly a in assemblies)
        {
            if (a.GetName().Name == AssemblyName)
            {
                Type t = a.GetType(fullName, throwOnError: false);
                if (t != null)
                    return t;
            }
        }

        foreach (Assembly a in assemblies)
        {
            Type t = a.GetType(fullName, throwOnError: false);
            if (t != null)
                return t;
        }

        return null;
    }
}
