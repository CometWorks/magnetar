using System;
using System.IO;
using System.Reflection;
using Steamworks;

namespace Pulsar.Shared;

public static class Steam
{
    public const uint AppIdSe1 = 244850u;
    public const uint AppIdSe1DS = 298740u;
    public const uint AppIdSe2 = 1133870u;
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
        // The native Steam client install on Linux lives under
        // ~/.steam/steam (a symlink Valve maintains to the active
        // Steam library). There is no equivalent of the Windows
        // SOFTWARE\Valve\Steam registry key.
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string steamRoot = Path.Combine(home, ".steam", "steam");
        return Directory.Exists(steamRoot) ? steamRoot : null;
    }
}
