using HarmonyLib;
using Magnetar.Legacy.Launcher;
using Sandbox;

namespace Pulsar.Legacy.Patch;

[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MySandboxGame), "ExitThreadSafe")]
public class Patch_ExitThreadSafe
{
    // LinuxCompat prefixes ExitThreadSafe with a plain process kill (the
    // client's exit can hang on Linux), which skips the save. Run first:
    // SaveAndQuit ends the process before that prefix gets its turn.
    [HarmonyPriority(Priority.First)]
    public static bool Prefix()
    {
        // SE's normal unload path hangs in this in-process hosting setup and
        // does not save anyway, so route in-game/admin exit through the same
        // graceful save+quit used for SIGTERM. SaveWorld's update-thread fast
        // path keeps this safe when the prefix runs on the update thread.
        // The DS's timed auto-restart doesn't come here, see
        // Patch_DedicatedAutoRestart.
        ServerControl.SaveAndQuit();
        return false;
    }
}
