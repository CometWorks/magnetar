using System;
using System.Collections.Generic;
using System.IO;
using Magnetar.Config.Model;

namespace Magnetar.Config.Io;

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
/// end to end: the Magnetar install is wherever this tool runs from. Explicit
/// CLI values always win; nothing here silently falls back past a value the
/// user gave.
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

    /// <summary>
    /// The Magnetar install root: the folder holding the MagnetarConfig binary.
    /// The launchers (MagnetarLegacy.exe / MagnetarInterim.exe|.bin) always sit
    /// next to it, and Magnetar is portable, so the install can live anywhere;
    /// nothing here assumes a deploy location. Resolved from the running
    /// executable; when the tool is hosted by <c>dotnet MagnetarConfig.dll</c>
    /// (tests, IDE) the process is the dotnet host, so the folder of the
    /// managed assembly is used instead.
    /// </summary>
    public static string InstallRoot
    {
        get
        {
            string exe = Environment.ProcessPath;
            string dir = !string.IsNullOrEmpty(exe)
                         && Path.GetFileNameWithoutExtension(exe)
                             .Equals("MagnetarConfig", StringComparison.OrdinalIgnoreCase)
                ? Path.GetDirectoryName(exe)
                : AppContext.BaseDirectory;
            return Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar);
        }
    }

    /// <summary>
    /// Default Magnetar config dir (config.xml, logs, magnetar.pid). Both
    /// launchers keep their shared config in the Magnetar folder next to the
    /// binaries (the counterpart of Pulsar's Legacy/Modern flavour folders).
    /// </summary>
    public static string DefaultMagnetarConfigDir() => LauncherConfigDir(InstallRoot);

    /// <summary>
    /// Default Magnetar launcher executable to spawn: the launcher next to this
    /// tool. Windows prefers Legacy over Interim when both are present; Linux
    /// has Interim only.
    /// </summary>
    public static string DefaultMagnetarExe()
    {
        if (!PlatformPaths.IsWindows)
            return Path.Combine(InstallRoot, "MagnetarInterim.bin");

        IReadOnlyList<MagnetarLauncher> present = PresentWindowsLaunchers();
        return present.Count > 0
            ? present[0].ExePath
            : Path.Combine(InstallRoot, "MagnetarLegacy.exe");
    }

    /// <summary>
    /// The Windows Magnetar launchers installed next to this tool (Legacy
    /// first, then Interim). Empty off Windows. The tool uses this to let the
    /// operator pick which launcher to configure when both are present, and to
    /// auto-select when only one is.
    /// </summary>
    public static IReadOnlyList<MagnetarLauncher> PresentWindowsLaunchers()
    {
        var launchers = new List<MagnetarLauncher>();
        if (!PlatformPaths.IsWindows)
            return launchers;

        string root = InstallRoot;
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
                    ConfigDir = LauncherConfigDir(root),
                });
        }
        return launchers;
    }

    /// <summary>
    /// The config dir the launchers read, mirroring their own resolution
    /// (<c>Legacy\Program.cs</c> <c>GetConfigDir</c>): the Magnetar folder
    /// inside the install, shared by both launchers and created on first
    /// start.
    /// </summary>
    private static string LauncherConfigDir(string root) => Path.Combine(root, "Magnetar");

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

    private static IEnumerable<string> Ds64Candidates()
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
