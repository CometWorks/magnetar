using System;
using HarmonyLib;
using Magnetar.Legacy.Launcher;
using Sandbox;

namespace Magnetar.Legacy.Patch;

// On Linux, Ctrl+C is SIGINT, and the DS's own Ctrl+C handler gets it
// instead of Magnetar's signal handler. Its ExitThreadSafe would make the
// stop a requested one (Terminating(Shutdown)), which a supervisor such as
// Quasar keeps off. A signal comes from outside the server, so stop the way
// SIGTERM does. On Windows, Ctrl+C stays the DS's own exit.
[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MySandboxGame), "Console_CancelKeyPress")]
internal static class Patch_ConsoleCancelKeyPress
{
#if NETCOREAPP
    public static bool Prepare() => OperatingSystem.IsLinux();
#else
    public static bool Prepare() => false;
#endif

    public static bool Prefix(ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        ServerControl.QuitOnSignal("SIGINT");
        return false;
    }
}
