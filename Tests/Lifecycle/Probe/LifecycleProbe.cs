using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using PluginSdk;
using PluginSdk.Commands;
using Sandbox;
using Sandbox.Game.World;
using VRage.FileSystem;
using VRage.Game.ModAPI;
using VRage.Plugins;

namespace LifecycleProbe;

// Test only. Lets Tests/Lifecycle drive a running server: the driver writes
// command lines into probe-cmd.txt in the DS data folder, the probe runs them
// and appends what happened to probe.log next to it. See Tests/Lifecycle/README.md.
// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    private static readonly string RunId = Guid.NewGuid().ToString("N").Substring(0, 8);
    private static int slowSaveSeconds;
    private string dir;
    private int tick;
    private bool ready;

    public void Init(object gameInstance)
    {
        dir =
            Environment.GetEnvironmentVariable("LIFECYCLE_PROBE_DIR") ?? MyFileSystem.UserDataPath;
        Log($"init run={RunId}");
        HoldInit();
        ServerControl.Terminating += kind => Log($"terminating {kind}");
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            Log($"process-exit code={Environment.ExitCode}");
        new Harmony("LifecycleProbe").Patch(
            AccessTools.Method(typeof(MySessionSnapshot), nameof(MySessionSnapshot.Save)),
            prefix: new HarmonyMethod(typeof(Plugin), nameof(SlowSave))
        );
        new Harmony("LifecycleProbe").Patch(
            AccessTools.Method(
                typeof(MySession),
                nameof(MySession.Save),
                new[]
                {
                    typeof(MySessionSnapshot).MakeByRefType(),
                    typeof(string),
                    typeof(Action<SaveProgress>),
                }
            ),
            prefix: new HarmonyMethod(typeof(Plugin), nameof(LogSave))
        );
    }

    // probe-hold-init.txt holds the server in Init for that many seconds, once, so a
    // supervisor sees it starting for a while.
    private void HoldInit()
    {
        string hold = Path.Combine(dir, "probe-hold-init.txt");
        if (!File.Exists(hold))
            return;

        int seconds = int.Parse(File.ReadAllText(hold).Trim());
        File.Delete(hold);
        Log($"holding init {seconds}s");
        Thread.Sleep(seconds * 1000);
    }

    public void Update()
    {
        if (++tick % 10 != 0)
            return;

        if (!ready && MySession.Static?.Ready == true)
        {
            ready = true;
            Log($"ready run={RunId}");
        }

        string cmd = Path.Combine(dir, "probe-cmd.txt");
        if (!File.Exists(cmd))
            return;

        // The driver writes probe-cmd.tmp and renames it, so the file is complete here.
        string taken = cmd + ".taken";
        try
        {
            File.Delete(taken);
            File.Move(cmd, taken);
        }
        catch (IOException)
        {
            return;
        }

        foreach (
            string line in File.ReadAllLines(taken).Select(l => l.Trim()).Where(l => l.Length > 0)
        )
            Run(line);
    }

    // Waits a second and logs how often the game thread updated the plugins
    // meanwhile. Magnetar must not dispose while it does: the loader drops
    // disposed plugins from the list it walks (SE1-0203).
    public void Dispose()
    {
        int before = Volatile.Read(ref tick);
        Thread.Sleep(1000);
        Log($"dispose updates={Volatile.Read(ref tick) - before}");
    }

    // "<command> [args]", optionally prefixed with "worker " to run it on a
    // thread pool thread instead of the game thread.
    private void Run(string line)
    {
        Log($"cmd {line}");
        if (line.StartsWith("worker "))
        {
            string rest = line.Substring(7);
            Task.Run(() => Execute(rest));
            return;
        }

        Execute(line);
    }

    private void Execute(string line)
    {
        string[] words = line.Split(new[] { ' ' }, 2);
        string arg = words.Length > 1 ? words[1] : "";
        try
        {
            switch (words[0])
            {
                case "chat":
                    Chat(arg);
                    break;
                case "sdk":
                    Sdk(arg);
                    break;
                case "exit":
                    MySandboxGame.ExitThreadSafe();
                    break;
                case "envexit":
                    Environment.Exit(int.Parse(arg));
                    break;
                case "update":
                    PendUpdate();
                    break;
                case "slowsave":
                    slowSaveSeconds = int.Parse(arg);
                    break;
                case "hang":
                    Thread.Sleep(int.Parse(arg) * 1000);
                    Log("hang over");
                    break;
                case "crash":
                    new Thread(() =>
                        throw new InvalidOperationException("LifecycleProbe crash")
                    ).Start();
                    break;
                default:
                    Log($"unknown command {words[0]}");
                    break;
            }
        }
        catch (Exception e)
        {
            Log($"error {line}: {e.GetType().Name}: {e.Message}");
        }
    }

    // Puts the DS where Keen's auto-update countdown ends: an update is pending
    // and its stop is due, so the next CheckAutoRestartForDedicatedServer stops
    // the server for the update.
    private static void PendUpdate()
    {
        MySandboxGame game = MySandboxGame.Static;
        AccessTools.Field(typeof(MySandboxGame), "m_isGoingToUpdate").SetValue(game, true);
        AccessTools.Field(typeof(MySandboxGame), "m_autoUpdateRestartTimeInMin").SetValue(game, 0);
        AccessTools.Field(typeof(MySandboxGame), "m_lastRestartCheckInMilis").SetValue(game, 0);
        FieldInfo stage = AccessTools.Field(typeof(MySandboxGame), "m_autoRestartState");
        stage.SetValue(null, Enum.Parse(stage.FieldType, "Restarting"));
    }

    private static void Sdk(string operation)
    {
        switch (operation)
        {
            case "SaveWorld":
                Log($"sdk SaveWorld returned {ServerControl.SaveWorld()}");
                break;
            case "SaveAndQuit":
                ServerControl.SaveAndQuit();
                break;
            case "SaveAndRestart":
                ServerControl.SaveAndRestart();
                break;
            case "QuitWithoutSaving":
                ServerControl.QuitWithoutSaving();
                break;
            case "RestartWithoutSaving":
                ServerControl.RestartWithoutSaving();
                break;
            default:
                Log($"unknown sdk operation {operation}");
                break;
        }
    }

    // Runs a chat command as the server console, through the same dispatcher
    // that Magnetar's chat hook uses. Quasar.Agent's and Essentials' commands
    // are registered there too.
    private static void Chat(string text)
    {
        object service = ServerCommands.Registrar;
        var dispatcher = (CommandDispatcher)
            service
                .GetType()
                .GetField("dispatcher", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(service);
        var caller = new CommandCaller(
            0,
            0,
            "LifecycleProbe",
            MyPromoteLevel.Owner,
            isConsole: true
        );
        bool handled = dispatcher.Handle(text, caller, new Responder());
        Log($"chat handled={handled}");
    }

    private sealed class Responder : ICommandResponder
    {
        public void Send(in CommandReply reply, in CommandCaller caller)
        {
            Log($"reply {reply.Text}");
        }
    }

    // Who saves the world, for the suite's saved/not saved checks.
    private static void LogSave()
    {
        string callers = string.Join(
            " < ",
            new StackTrace()
                .GetFrames()!
                .Skip(2)
                .Take(6)
                .Select(f => f.GetMethod())
                .Where(m => m != null)
                .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
        );
        Log($"save from {callers}");
    }

    private static void SlowSave()
    {
        int seconds = slowSaveSeconds;
        if (seconds <= 0)
            return;
        Log($"slow save {seconds}s");
        Thread.Sleep(seconds * 1000);
    }

    private static readonly object LogLock = new object();
    private static string logPath;

    private static void Log(string message)
    {
        try
        {
            logPath ??= Path.Combine(
                Environment.GetEnvironmentVariable("LIFECYCLE_PROBE_DIR")
                    ?? MyFileSystem.UserDataPath,
                "probe.log"
            );
            string line =
                $"{DateTime.UtcNow:O} pid={Process.GetCurrentProcess().Id} {message}{Environment.NewLine}";
            lock (LogLock)
                File.AppendAllText(logPath, line);
        }
        catch
        {
            // The log is best effort; a test reads what got there.
        }
    }
}
