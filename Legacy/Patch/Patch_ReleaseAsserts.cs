using System.Diagnostics;
using HarmonyLib;
using VRage;
using VRage.Utils;

namespace Magnetar.Legacy.Patch;

// The game's release asserts (MyDebug.FailRelease, MyDebug.AssertRelease) go to
// the listener that VRage.Platform.Windows registers when the platform is made.
// It logs "ASSERT: ..." and then shows a WinForms Abort/Retry/Ignore box. A
// server has nobody to click it, and on .NET 10 System.Windows.Forms does not
// load, so the listener throws and the assert takes the server down. A world
// with two grids sharing an entity id does that while loading, where the
// Windows game logs the assert and drops one of the grids.
//
// Replace the listener with one that only logs, which is what pressing Ignore
// does.
[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MyVRage), nameof(MyVRage.Init))]
internal static class Patch_ReleaseAsserts
{
    public static void Postfix()
    {
        MyDebug.Listeners.Clear();
        MyDebug.Listeners.Add(new LogOnlyListener());
    }

    private sealed class LogOnlyListener : TraceListener, IAdvancedDebugListener
    {
        public override void Write(string message) => WriteLine(message, null, -1);

        public override void WriteLine(string message) => WriteLine(message, null, -1);

        public override void Fail(string message, string detail) => Fail(message, detail, null, -1);

        public void WriteLine(string message, string file, int line) =>
            Log(message, null, file, line);

        public void Fail(string message, string detail, string file, int line) =>
            Log(message, detail, file, line);

        private static void Log(string message, string detail, string file, int line)
        {
            string text = "ASSERT: " + message;
            if (file != null)
                text += $"\n{file}:{line}";
            if (!string.IsNullOrEmpty(detail))
                text += "\n\nDetail: " + detail;
            // Skip this method, the listener method and MyDebug's, like the game does.
            MyLog.Default?.AppendToClosedLog(text + "\n\n" + new StackTrace(3, true));
        }
    }
}
