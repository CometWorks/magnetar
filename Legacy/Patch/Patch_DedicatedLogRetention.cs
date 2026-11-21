using System;
using HarmonyLib;
using Magnetar.Legacy.Arguments;
using Sandbox;
using VRage.FileSystem;
using VRage.Utils;

namespace Magnetar.Legacy.Patch;

// The DS calls InvokeBeforeRun without a max log age, so the game's own
// MyLog.RemoveExcessLogs never deletes anything. Once the new log is open,
// prune the old ones by age and total size (-logMaxAgeDays, -logMaxSizeMB).
[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MyInitializer), nameof(MyInitializer.InvokeBeforeRun))]
internal static class Patch_DedicatedLogRetention
{
    public static void Postfix(string appName)
    {
        try
        {
            var deleted = DedicatedLogRetention.Prune(
                MyFileSystem.UserDataPath,
                appName + "_*.log",
                MyLog.Default?.GetFilePath(),
                ServerFlags.LogMaxAgeDays,
                ServerFlags.LogMaxSizeMB * 1024L * 1024L,
                DateTime.UtcNow
            );
            if (deleted.Count > 0)
                MyLog.Default?.WriteLine(
                    $"Magnetar: deleted {deleted.Count} old log(s) (-logMaxAgeDays {ServerFlags.LogMaxAgeDays}, -logMaxSizeMB {ServerFlags.LogMaxSizeMB}): {string.Join(", ", deleted)}"
                );
        }
        catch (Exception error)
        {
            MyLog.Default?.WriteLine("Magnetar: old log cleanup failed: " + error.Message);
        }
    }
}
