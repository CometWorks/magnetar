using System;
using HarmonyLib;
using Keen.VRage.Core.Platform.CrashReporting;
using Keen.VRage.Library.Diagnostics;
using Pulsar.Shared;

namespace Pulsar.Modern.Patch;

[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(CrashHandler), "HandlePrimaryException")]
internal class Patch_HandleCrash
{
    private static bool Prefix(Exception ex)
    {
        string message =
            "Game has crashed.\n"
            + "Space Engineers 2 encountered an unexpected error that was not handled.\n"
            + "The game has now closed as it can no longer proceed safely.\n"
            + "Try running the game without Pulsar to see if this resolves the issue.\n"
            + "Do NOT report this crash to Keen, as the crash may be caused by plugins or Pulsar.\n"
            + "Instead, report this crash in the support forum on Pulsar's Discord server.";

        Log.Default.Flush();
        Log.Default.WriteLine(ex);
        LogFile.GameLog.Write(message);
        Log.Default.Flush();

        Console.Error.WriteLine($"[Pulsar] {message}");
        Console.Error.WriteLine(ex);

        Environment.Exit(-1);
        return false;
    }
}
