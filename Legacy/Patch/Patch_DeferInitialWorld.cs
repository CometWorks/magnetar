using HarmonyLib;
using Magnetar.Legacy.Launcher;
using Sandbox;
using Sandbox.Game.World;
using VRage.Utils;

namespace Magnetar.Legacy.Patch;

// The dedicated server loads its initial world in MySandboxGame.Initialize,
// before MySandboxGame.Run calls IPlugin.Init. A client loads worlds only after
// the plugins' Init. Deferring the dedicated server's initial world until
// Magnetar has initialized the plugins gives server plugins the client's order:
// their patches are applied and their session components are registered (and
// restored from the save) before the world loads.
[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MySandboxGame), "InitQuickLaunch")]
internal static class Patch_DeferInitialWorld
{
    private static bool deferred;

    public static bool Prefix(ref bool __result)
    {
        if (deferred || !Sandbox.Engine.Platform.Game.IsDedicated)
            return true;

        deferred = true;
        __result = true;
        return false;
    }

    /// <summary>Loads the deferred initial world, once the plugins are initialized.</summary>
    public static void LoadInitialWorld()
    {
        if (!deferred || MySession.Static is not null)
            return;

        bool loaded = Traverse.Create(MySandboxGame.Static).Method("InitQuickLaunch").GetValue<bool>();
        if (loaded || MySession.Static is not null)
            return;

        // What MySandboxGame.Run does when Initialize fails to start a session
        MyLog.Default.WriteLineAndConsole("Session can not start. Save is corrupted or not valid. See log file for more information.");
        ServerControl.QuitWithoutSaving();
    }
}
