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
        var ids = new HashSet<ulong>(configuredIds ?? []);

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

        checkpoint.Mods ??= [];

        if (IsCrossplayEnabled())
        {
            int removed = checkpoint.Mods.RemoveAll(IsMagnetarMod);
            if (removed > 0)
                LogFile.WriteLine($"Crossplay enabled; removed MagnetarMod client companion ({WorkshopId}) from world mods.");
            return;
        }

        if (checkpoint.Mods.Any(IsMagnetarMod))
            return;

        checkpoint.Mods.Add(new MyObjectBuilder_Checkpoint.ModItem(WorkshopId, WorkshopService));
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
}
