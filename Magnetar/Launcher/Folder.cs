using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using Microsoft.Win32;
using Pulsar.Shared;

namespace Pulsar.Magnetar.Launcher;

internal class Folder
{
    private const string registryKey =
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {0}";
    private const string registryName = "InstallLocation";

    private const string dsLauncher = "SpaceEngineersDedicated.exe";
    private static readonly HashSet<string> dsFiles =
    [
        dsLauncher,
        "SpaceEngineers.Game.dll",
        "VRage.dll",
        "Sandbox.Game.dll",
    ];

    public static string GetDS64() =>
        FromOverride() ?? FromSteamArgs() ?? FromSteamFiles() ?? FromRegistry();

    private static bool IsDS64(string path)
    {
        if (!Directory.Exists(path))
            return false;

        foreach (string file in dsFiles)
            if (!File.Exists(Path.Combine(path, file)))
                return false;

        return true;
    }

    private static string TryConvertUnix(string path)
    {
        if (!Tools.IsNative() && path.StartsWith("/"))
            return "Z:" + path;
        return path;
    }

    private static string FromRegistry()
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            RegistryHive.LocalMachine,
            RegistryView.Registry64
        );

        using var key = baseKey.OpenSubKey(string.Format(registryKey, Steam.AppIdSe1DS));
        if (key is null)
            return null;

        var installLocation = key.GetValue(registryName) as string;
        if (string.IsNullOrWhiteSpace(installLocation))
            return null;

        string path = Path.Combine(installLocation, "DedicatedServer64");
        if (!IsDS64(path))
            return null;

        return path;
    }

    private static string FromOverride()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.FindIndex(
            args,
            arg => arg.Equals("-ds64", StringComparison.OrdinalIgnoreCase)
        );

        if (index < 0 || index >= args.Length - 1)
            return null;

        string path = args[index + 1];
        if (!Path.IsPathRooted(path))
        {
            string currentPath = Assembly.GetExecutingAssembly().Location;
            string currentDir = Path.GetDirectoryName(currentPath);
            path = Path.Combine(currentDir, path);
        }
        else
            path = TryConvertUnix(path);

        if (!IsDS64(path))
            return null;

        return Path.GetFullPath(path);
    }

    private static string FromSteamArgs()
    {
        IEnumerable<string> dsPaths = Environment
            .GetCommandLineArgs()
            .Where(arg => arg.Contains("DedicatedServer64") && arg.Contains(dsLauncher))
            .Select(TryConvertUnix)
            .Select(Path.GetDirectoryName);

        foreach (string path in dsPaths)
            if (IsDS64(path))
                return path;

        return null;
    }

    private static string FromSteamFiles()
    {
        if (!Tools.IsNative())
            return null;

        string steamPath = Steam.GetSteamPath();

        if (steamPath is null)
            return null;

        string libraryPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        VProperty libraries = VdfConvert.Deserialize(File.ReadAllText(libraryPath));

        foreach (var library in libraries.Value.Children<VProperty>())
        {
            var data = (VObject)library.Value;
            var apps = (VObject)data["apps"];

            if (!apps.ContainsKey(Steam.AppIdSe1DS.ToString()))
                continue;

            string targetPath = data.Value<string>("path");
            string ds64 = Path.Combine(
                targetPath,
                "steamapps",
                "common",
                "SpaceEngineersDedicatedServer",
                "DedicatedServer64"
            );

            if (IsDS64(ds64))
                return ds64;
        }

        return null;
    }
}
