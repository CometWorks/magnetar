using Sandbox.Engine.Platform;
using Sandbox.Game.World;
using VRage;
using VRage.Plugins;
using VRage.Utils;

namespace ReleaseAssertProbe;

// Test only. Fires one release assert on the game thread, the way the game does
// on bad world data, and logs whether the call returned. A server fires once the
// world runs; a client fires at the main menu, after about ten seconds of updates.
// See Tests/ReleaseAsserts/README.md.
// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    private bool fired;
    private int updates;

    public void Init(object gameInstance) { }

    public void Update()
    {
        bool ready = Game.IsDedicated ? MySession.Static?.Ready == true : ++updates > 600;
        if (fired || !ready)
            return;
        fired = true;
        MyLog.Default.WriteLineAndConsole("ReleaseAssertProbe: firing");
        MyDebug.FailRelease("ReleaseAssertProbe assert");
        MyLog.Default.WriteLineAndConsole("ReleaseAssertProbe: returned");
    }

    public void Dispose() { }
}
