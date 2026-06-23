using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar.Shared;
using Sandbox;
using VRage.Game;

namespace Pulsar.Legacy.Loader;

internal static class MagnetarClientMod
{
    public const ulong WorkshopId = 3750200326UL;

    private const string WorkshopService = "Steam";

    public static HashSet<ulong> GetWorkshopIdsForUpdate(IEnumerable<ulong> configuredIds)
    {
        var ids = new HashSet<ulong>(configuredIds ?? Enumerable.Empty<ulong>());

        if (Flags.NoImplicitMod)
        {
            ids.Remove(WorkshopId);
            LogFile.WriteLine("MagnetarMod client companion disabled by -noimplicitmod.");
            return ids;
        }

        if (IsCrossplayEnabled())
        {
            ids.Remove(WorkshopId);
            LogFile.WriteLine("Crossplay enabled; skipping MagnetarMod client companion.");
            return ids;
        }

        if (ids.Add(WorkshopId))
            LogFile.WriteLine($"Adding required MagnetarMod client companion ({WorkshopId}).");

        return ids;
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

        if (Flags.NoImplicitMod)
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

        if (mods.Any(IsMagnetarMod))
            return;

        mods.Add(CreateModItem());
        LogFile.WriteLine($"Added required MagnetarMod client companion ({WorkshopId}) to world mods.");
    }

    private static bool IsCrossplayEnabled()
    {
        if (MySandboxGame.ConfigDedicated == null)
            return false;

        return MySandboxGame.ConfigDedicated.CrossPlatform ||
               MySandboxGame.ConfigDedicated.ConsoleCompatibility ||
               string.Equals(MySandboxGame.ConfigDedicated.NetworkType, "eos", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMagnetarMod(MyObjectBuilder_Checkpoint.ModItem mod)
        => mod.PublishedFileId == WorkshopId;

    private static MyObjectBuilder_Checkpoint.ModItem CreateModItem()
        => new MyObjectBuilder_Checkpoint.ModItem(WorkshopId, WorkshopService)
        {
            FriendlyName = "MagnetarMod",
        };
}
