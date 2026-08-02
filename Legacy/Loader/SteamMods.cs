using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using HarmonyLib;
using ParallelTasks;
using Pulsar.Shared;
using Pulsar.Shared.Data;
using Sandbox.Engine.Networking;
using VRage.Game;
using VRage.GameServices;
using VRage.Utils;

namespace Pulsar.Legacy.Loader;

public static class SteamMods
{
    private const string SteamWorkshopService = "Steam";

    private static MethodInfo DownloadModsBlocking;
    private static bool installStateWarningLogged;

    public static bool IsSteamWorkshopAvailable()
    {
        try
        {
            return MyGameService.GetUGC(SteamWorkshopService) is not null;
        }
        catch
        {
            return false;
        }
    }

    public static void Update(IEnumerable<ulong> ids)
    {
        var modItems = new List<MyObjectBuilder_Checkpoint.ModItem>(
            ids.Select(x => new MyObjectBuilder_Checkpoint.ModItem(x, "Steam"))
        );
        if (modItems.Count == 0)
            return;

        if (!IsSteamWorkshopAvailable())
        {
            LogFile.Warn(
                $"Steam Workshop service unavailable; skipping update for {modItems.Count} workshop items. Server startup will continue."
            );
            return;
        }

        LogFile.WriteLine($"Updating {modItems.Count} workshop items");

        try
        {
            // Source: MyWorkshop.DownloadWorldModsBlocking
            MyWorkshop.ResultData result = new();
            Task task = Parallel.Start(
                delegate
                {
                    result = UpdateInternal(modItems);
                }
            );
            while (!task.IsComplete)
            {
                MyGameService.Update();
                Thread.Sleep(10);
            }

            Exception[] exceptions = task.Exceptions;
            if (exceptions is not null && exceptions.Length > 0)
            {
                StringBuilder sb = new();
                sb.AppendLine("Unable to update workshop items; server startup will continue:");
                foreach (Exception e in exceptions)
                    sb.Append(e);
                LogFile.Warn(sb.ToString());
            }
            else if (result.Result != MyGameServiceCallResult.OK)
            {
                LogFile.Warn(
                    "Unable to update workshop items; server startup will continue. Result: "
                        + result.Result
                );
            }
        }
        catch (Exception e)
        {
            LogFile.Warn(
                "Unable to update workshop items; server startup will continue: " + e
            );
        }
    }

    public static bool IsModUntrusted(MyObjectBuilder_Checkpoint.ModItem mod)
    {
        if (mod.PublishedServiceName != SteamWorkshopService)
            return true;

        try
        {
            IMyUGCService steam = MyGameService.GetUGC(SteamWorkshopService);
            if (steam is null)
            {
                WarnInstallState(
                    "Steam Workshop service unavailable; treating Steam workshop items as untrusted."
                );
                return true;
            }

            MyWorkshopItem item = steam.CreateWorkshopItem();
            item.Id = mod.PublishedFileId;
            item.UpdateState();
            return !item.State.HasFlag(MyWorkshopItemState.Installed);
        }
        catch (Exception e)
        {
            WarnInstallState(
                "Unable to verify installed Steam Workshop items; treating them as untrusted: "
                    + e
            );
            return true;
        }
    }

    private static void WarnInstallState(string message)
    {
        if (installStateWarningLogged)
            return;

        installStateWarningLogged = true;
        LogFile.Warn(message);
    }

    public static MyWorkshop.ResultData UpdateInternal(
        List<MyObjectBuilder_Checkpoint.ModItem> mods
    )
    {
        // Source: MyWorkshop.DownloadWorldModsBlockingInternal

        MyLog.Default.IncreaseIndent();
        try
        {
            List<WorkshopId> list =
            [
                .. mods.Select(x => new WorkshopId(x.PublishedFileId, x.PublishedServiceName)),
            ];

            DownloadModsBlocking ??= AccessTools.Method(
                typeof(MyWorkshop),
                "DownloadModsBlocking"
            );
            if (DownloadModsBlocking is null)
                throw new MissingMethodException(typeof(MyWorkshop).FullName, "DownloadModsBlocking");

            MyWorkshop.ResultData resultData = (MyWorkshop.ResultData)
                DownloadModsBlocking.Invoke(
                    mods,
                    [mods, new MyWorkshop.ResultData(), list, new MyWorkshop.CancelToken()]
                );

            if (resultData.Result == MyGameServiceCallResult.OK)
                RepairLegacyArchives(mods);

            return resultData;
        }
        finally
        {
            MyLog.Default.DecreaseIndent();
        }
    }

    public static void RepairLegacyArchives(IEnumerable<MyObjectBuilder_Checkpoint.ModItem> mods)
    {
        if (mods is null)
            return;

        foreach (MyObjectBuilder_Checkpoint.ModItem mod in mods)
        {
            if (mod.PublishedFileId == 0 || !mod.IsModData())
                continue;

            try
            {
                string folder = mod.GetModData().Folder;
                LegacyWorkshopArchive.TryRepair(mod.PublishedFileId, folder);
            }
            catch (Exception e)
            {
                LogFile.Error(
                    "Failed checking legacy workshop mod "
                        + mod.PublishedFileId
                        + " for extraction: "
                        + e
                );
            }
        }
    }
}
