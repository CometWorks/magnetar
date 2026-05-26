using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Win32;
using Steamworks;

namespace Pulsar.Shared;

public static class Steam
{
    public const uint AppIdSe1 = 244850u;
    public const uint AppIdSe1DS = 298740u;
    public const uint AppIdSe2 = 1133870u;
    private const int SteamTimeout = 30; // seconds
    private const string registryKey = @"SOFTWARE\Valve\Steam";
    private const string registryName = "SteamPath";
    private const string Steamworks = "Steamworks.NET";

    // Dedicated servers must not initialize the Steam client API; only the game-server
    // UGC is available. A workshop item the server has downloaded reports Installed
    // (Subscribed is a client-only concept), so trust checks key off Installed.
    public static bool IsItemInstalled(ulong id)
    {
        uint state = SteamGameServerUGC.GetItemState(new PublishedFileId_t(id));
        return (state & (uint)EItemState.k_EItemStateInstalled) != 0;
    }

    public static ResolveEventHandler SteamworksResolver(string baseDir)
    {
        return (sender, args) =>
        {
            string targetName = new AssemblyName(args.Name).Name;
            if (targetName != Steamworks)
                return null;

            string targetPath = Path.Combine(baseDir, $"{Steamworks}.dll");
            if (File.Exists(targetPath))
                return Assembly.LoadFrom(targetPath);

            return null;
        };
    }

    public static string GetSteamPath()
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            RegistryHive.CurrentUser,
            RegistryView.Registry64
        );

        using var key = baseKey.OpenSubKey(registryKey);
        if (key is null)
            return null;

        var path = key.GetValue(registryName) as string;
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return path;
    }

    private static void ShowWarning()
    {
        LogFile.WriteLine("Steam failed to start!");
    }
}
