using System;
using System.IO;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Io;

/// <summary>
/// Resolves the default folder pair and the Magnetar/DS install locations, with
/// the same semantics as Magnetar itself so a non-standard deployment resolves
/// end to end. Explicit CLI values always win; nothing here silently falls back
/// past a value the user gave.
/// </summary>
internal static class InstanceLocator
{
    private static string Home =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>Default DS data dir (holds the cfg + Saves).</summary>
    public static string DefaultDataDir()
    {
        if (PlatformPaths.IsWindows)
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SpaceEngineersDedicated");
        return Path.Combine(Home, ".config", "SpaceEngineersDedicated");
    }

    /// <summary>Default Magnetar config dir (config.xml, logs, magnetar.pid).</summary>
    public static string DefaultMagnetarConfigDir()
    {
        if (PlatformPaths.IsWindows)
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Magnetar", "MagnetarLegacy");

        string xdg = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (!string.IsNullOrWhiteSpace(xdg))
            return Path.Combine(xdg, "Magnetar");
        return Path.Combine(Home, ".config", "Magnetar");
    }

    /// <summary>Default Magnetar launcher executable to spawn.</summary>
    public static string DefaultMagnetarExe()
    {
        if (PlatformPaths.IsWindows)
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Magnetar", "MagnetarLegacy.exe");

        string xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        string baseDir = !string.IsNullOrWhiteSpace(xdgData)
            ? xdgData
            : Path.Combine(Home, ".local", "share");
        return Path.Combine(baseDir, "Magnetar", "MagnetarInterim");
    }

    /// <summary>Best-effort DS install (DedicatedServer64) auto-detection; null when not found.</summary>
    public static string DetectDs64()
    {
        foreach (string candidate in Ds64Candidates())
        {
            if (IsDs64(candidate))
                return Path.GetFullPath(candidate);
        }
        return null;
    }

    private static System.Collections.Generic.IEnumerable<string> Ds64Candidates()
    {
        if (PlatformPaths.IsWindows)
        {
            yield return @"C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineersDedicatedServer\DedicatedServer64";
        }
        else
        {
            yield return Path.Combine(Home, ".steam", "steam", "steamapps", "common",
                "SpaceEngineersDedicatedServer", "DedicatedServer64");
            yield return Path.Combine(Home, ".local", "share", "Steam", "steamapps", "common",
                "SpaceEngineersDedicatedServer", "DedicatedServer64");
        }
    }

    private static bool IsDs64(string dir)
    {
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return false;
        // The DS launcher is the reliable marker.
        return File.Exists(Path.Combine(dir, "SpaceEngineersDedicated.exe"))
               || File.Exists(Path.Combine(dir, "VRage.dll"));
    }

    /// <summary>Fills in any unset binding fields with their resolved defaults.</summary>
    public static InstanceBinding ResolveDefaults(InstanceBinding binding)
    {
        binding.DataDir ??= DefaultDataDir();
        binding.MagnetarConfigDir ??= DefaultMagnetarConfigDir();
        binding.MagnetarExePath ??= DefaultMagnetarExe();
        binding.Ds64Dir ??= DetectDs64();
        return binding;
    }
}
