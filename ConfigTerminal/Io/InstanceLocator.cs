using System;
using System.Collections.Generic;
using System.IO;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Io;

/// <summary>A Windows Magnetar launcher variant the tool can configure.</summary>
internal sealed class MagnetarLauncher
{
    public string Name;       // assembly / exe base name, e.g. "MagnetarLegacy"
    public string Label;      // human label for the picker
    public string ConfigDir;  // where this launcher reads its config.xml / pid
    public string ExePath;    // launcher executable to start/stop
}

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

    /// <summary>The %APPDATA%\Magnetar root the Windows launchers are deployed under.</summary>
    private static string WindowsMagnetarRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Magnetar");

    /// <summary>
    /// The Windows Magnetar launchers whose executable is installed under
    /// %APPDATA%\Magnetar (Legacy first, then Interim). Empty off Windows. The
    /// tool uses this to let the operator pick which launcher to configure when
    /// both are present, and to auto-select when only one is.
    /// </summary>
    public static IReadOnlyList<MagnetarLauncher> PresentWindowsLaunchers()
    {
        var launchers = new List<MagnetarLauncher>();
        if (!PlatformPaths.IsWindows)
            return launchers;

        string root = WindowsMagnetarRoot();
        foreach ((string name, string label) in new[]
                 {
                     ("MagnetarLegacy", "Legacy (.NET Framework 4.8)"),
                     ("MagnetarInterim", "Interim (.NET 10)"),
                 })
        {
            string exe = Path.Combine(root, name + ".exe");
            if (File.Exists(exe))
                launchers.Add(new MagnetarLauncher
                {
                    Name = name,
                    Label = label,
                    ExePath = exe,
                    ConfigDir = ResolveLauncherConfigDir(root, name),
                });
        }
        return launchers;
    }

    /// <summary>
    /// The config dir a launcher actually reads, mirroring the launcher's own
    /// resolution (<c>Legacy\Program.cs</c> <c>GetConfigDir</c>): the folder named
    /// after the launcher if it exists, otherwise the <c>MagnetarLegacy</c>
    /// fallback both launchers share until a named folder is created.
    /// </summary>
    private static string ResolveLauncherConfigDir(string root, string name)
    {
        string named = Path.Combine(root, name);
        return Directory.Exists(named) ? named : Path.Combine(root, "MagnetarLegacy");
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
