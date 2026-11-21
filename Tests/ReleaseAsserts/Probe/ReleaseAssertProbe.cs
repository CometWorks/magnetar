using Sandbox.Game.World;
using VRage;
using VRage.Plugins;
using VRage.Utils;

namespace ReleaseAssertProbe;

// Test only. Fires one release assert on the game thread once the world runs,
// the way the game does on bad world data, and logs whether the call returned.
// See Tests/ReleaseAsserts/README.md.
// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    private bool fired;

    public void Init(object gameInstance) { }

    public void Update()
    {
        if (fired || MySession.Static?.Ready != true)
            return;
        fired = true;
        MyLog.Default.WriteLineAndConsole("ReleaseAssertProbe: firing");
        MyDebug.FailRelease("ReleaseAssertProbe assert");
        MyLog.Default.WriteLineAndConsole("ReleaseAssertProbe: returned");
    }

    public void Dispose() { }
}
