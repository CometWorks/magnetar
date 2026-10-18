using Magnetar.Legacy.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar.Legacy.Loader;
using Pulsar.Shared;
using Sandbox;
using VRage.Game;

namespace Magnetar.Legacy.Loader;

internal static class MagnetarClientMod
{
    public const ulong WorkshopId = 3750200326UL;

    // Logged whenever Magnetar brings the mod in, so admins can tell why it is there.
    private const string Purpose =
        "Clients download it with the world; server plugins built on the PluginSdk need it to open mission-screen popups on clients. Pass -noimplicitmod to leave it out.";

    private const string WorkshopService = "Steam";
    private static bool steamUnavailableWarningLogged;
    private static bool downloadFailed;

    /// <summary>
    /// Downloads the companion before the world loads. If that fails, the mod
    /// stays out of the world mods for this run, so a Workshop problem can't
    /// stop the world from loading.
    /// </summary>
    public static void Download()
    {
        if (ServerFlags.NoImplicitMod)
        {
            LogFile.WriteLine("MagnetarMod client companion disabled by -noimplicitmod.");
            return;
        }

        if (IsCrossplayEnabled())
        {
            LogFile.WriteLine("Crossplay enabled; skipping MagnetarMod client companion.");
            return;
        }

        if (!SteamMods.IsSteamWorkshopAvailable())
        {
            WarnSteamUnavailable();
            return;
        }

        LogFile.WriteLine($"Checking MagnetarMod client companion ({WorkshopId}) for updates.");
        if (SteamMods.Update([WorkshopId]))
            return;

        downloadFailed = true;
        LogFile.Warn(
            $"MagnetarMod client companion ({WorkshopId}) could not be downloaded from the Steam Workshop; leaving it out of the world mods so the server can start. Mission-screen popups will be unavailable."
        );
    }

    public static void ApplyToCheckpoint(MyObjectBuilder_Checkpoint checkpoint)
    {
        if (checkpoint == null)
            return;

        ApplyToModList(ref checkpoint.Mods);
    }

    public static void ApplyToModList(ref List<MyObjectBuilder_Checkpoint.ModItem> mods)
    {
        if (mods == null)
            mods = new List<MyObjectBuilder_Checkpoint.ModItem>();

        ApplyToModList(mods);
    }

    public static void ApplyToModList(List<MyObjectBuilder_Checkpoint.ModItem> mods)
    {
        if (mods == null)
            return;

        if (ServerFlags.NoImplicitMod)
        {
            int removed = mods.RemoveAll(IsMagnetarMod);
            if (removed > 0)
                LogFile.WriteLine($"Removed MagnetarMod client companion ({WorkshopId}) from world mods because -noimplicitmod is set.");
            return;
        }

        if (IsCrossplayEnabled())
        {
            int removed = mods.RemoveAll(IsMagnetarMod);
            if (removed > 0)
                LogFile.WriteLine($"Crossplay enabled; removed MagnetarMod client companion ({WorkshopId}) from world mods.");
            return;
        }

        if (!SteamMods.IsSteamWorkshopAvailable())
        {
            mods.RemoveAll(IsMagnetarMod);
            WarnSteamUnavailable();
            return;
        }

        if (downloadFailed)
        {
            mods.RemoveAll(IsMagnetarMod);
            return;
        }

        if (mods.Any(IsMagnetarMod))
            return;

        mods.Add(CreateModItem());
        LogFile.WriteLine($"Added MagnetarMod client companion ({WorkshopId}) to world mods. {Purpose}");
    }

    private static bool IsCrossplayEnabled()
    {
        if (MySandboxGame.ConfigDedicated == null)
            return false;

        return MySandboxGame.ConfigDedicated.CrossPlatform ||
               MySandboxGame.ConfigDedicated.ConsoleCompatibility ||
               string.Equals(MySandboxGame.ConfigDedicated.NetworkType, "eos", StringComparison.OrdinalIgnoreCase);
    }

    private static void WarnSteamUnavailable()
    {
        if (steamUnavailableWarningLogged)
            return;

        steamUnavailableWarningLogged = true;
        LogFile.Warn(
            $"Steam Workshop service unavailable; MagnetarMod client companion ({WorkshopId}) will not be loaded. Mission-screen popups will be unavailable."
        );
    }

    private static bool IsMagnetarMod(MyObjectBuilder_Checkpoint.ModItem mod)
        => mod.PublishedFileId == WorkshopId;

    private static MyObjectBuilder_Checkpoint.ModItem CreateModItem()
        => new MyObjectBuilder_Checkpoint.ModItem(WorkshopId, WorkshopService)
        {
            FriendlyName = "MagnetarMod",
        };
}
