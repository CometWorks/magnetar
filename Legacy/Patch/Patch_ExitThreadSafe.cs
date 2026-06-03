using HarmonyLib;
using Pulsar.Legacy.Launcher;
using Sandbox;

namespace Pulsar.Legacy.Patch;

[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MySandboxGame), "ExitThreadSafe")]
public class Patch_ExitThreadSafe
{
    public static bool Prefix()
    {
        // SE's normal unload path hangs in this in-process hosting setup and
        // does not save anyway, so route in-game/admin exit through the same
        // graceful save+quit used for SIGTERM. SaveWorld's update-thread fast
        // path keeps this safe when the prefix runs on the update thread.
        ServerControl.SaveAndQuit();
        return false;
    }
}
