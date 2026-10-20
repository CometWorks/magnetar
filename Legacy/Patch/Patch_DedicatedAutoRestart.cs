using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Pulsar.Shared;
using Sandbox;

namespace Magnetar.Legacy.Patch;

// The dedicated server's timed auto-restart (AutoRestartEnabled with
// AutoRestatTimeInMin) ends in MySandboxGame.ExitThreadSafe, which only stops
// the server: Keen's configurator is what starts it again, and Magnetar does
// not run it. Replace that one call so the auto-restart restarts. It goes
// through the PluginSdk facade, like !restart, so a registered lifecycle
// provider (Quasar.Agent, a cluster) handles it instead of Magnetar.
// Bypassing ExitThreadSafe also keeps LinuxCompat's kill prefix on it out of
// the way while a provider is still saving.
//
// Keen's auto-update stop comes here too. Standalone it keeps the plain exit,
// so whatever updates the DS can run before it starts again. A provider such
// as Quasar.Agent gets a restart request instead: its supervisor updates the
// DS when it starts the server again. A cluster process keeps the exit too,
// because without a provider its requests fail closed and the server would
// never update.
[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MySandboxGame), "CheckAutoRestartForDedicatedServer")]
internal static class Patch_DedicatedAutoRestart
{
    private static bool requested;

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo exit = AccessTools.Method(
            typeof(MySandboxGame),
            nameof(MySandboxGame.ExitThreadSafe)
        );
        MethodInfo restart = AccessTools.Method(
            typeof(Patch_DedicatedAutoRestart),
            nameof(Restart)
        );

        List<CodeInstruction> code = instructions.ToList();
        int replaced = 0;
        foreach (CodeInstruction instruction in code.Where(i => i.Calls(exit)))
        {
            instruction.operand = restart;
            replaced++;
        }

        if (replaced != 1)
            LogFile.Warn(
                $"Dedicated server auto-restart: expected one ExitThreadSafe call, replaced {replaced}"
            );

        return code;
    }

    private static void Restart()
    {
        // The DS calls this again every minute until the process ends; a
        // provider may still be saving by then.
        if (requested)
            return;
        requested = true;

        if (MySandboxGame.Static?.IsGoingToUpdate == true)
        {
            if (PluginSdk.ServerControl.TryRequestRestart("Dedicated server update"))
            {
                LogFile.WriteLine(
                    "Dedicated server update pending: asked the lifecycle provider for a restart"
                );
                return;
            }

            MySandboxGame.ExitThreadSafe();
            return;
        }

        PluginSdk.ServerControl.SaveAndRestart();
    }
}
