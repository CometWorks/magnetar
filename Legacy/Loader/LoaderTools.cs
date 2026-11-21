using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Pulsar.Shared;

namespace Pulsar.Legacy.Loader;

public static class LoaderTools
{
    private const string ContinueArg = "-continue";
    private const string DebugArg = "-debug";

#if NETCOREAPP
    [DllImport("libc", SetLastError = true)]
    private static extern int execv(string path, string[] argv);
#endif

    public static void Restart(bool autoRejoin = false, bool? debugger = null)
    {
        Start(autoRejoin, debugger ?? Debugger.IsAttached);
        Process.GetCurrentProcess().Kill();
    }

    private static void Start(bool autoRejoin, bool debugger)
    {
        // First "argument" is the invoked executable
        List<string> args = [.. Environment.GetCommandLineArgs().Skip(1)];

        args.Remove(ContinueArg);
        if (autoRejoin)
            args.Add(ContinueArg);

        args.Remove(DebugArg);
        if (debugger)
            args.Add(DebugArg);

        string mainModule = Process.GetCurrentProcess().MainModule.FileName;

#if NETCOREAPP
        if (OperatingSystem.IsLinux())
        {
            // execv replaces the current process image. Combined with the
            // Kill() in Restart() this preserves the parent's PID, stdio
            // and tty (matters for systemd / tmux supervision). argv[0]
            // must be the program name by convention.
            string[] argv = new string[args.Count + 2];
            argv[0] = mainModule;
            for (int i = 0; i < args.Count; i++)
                argv[i + 1] = args[i];
            argv[^1] = null;

            execv(mainModule, argv);
            // Only reached if execv fails.
            LogFile.Error($"execv failed: {Marshal.GetLastWin32Error()}");
            return;
        }
#endif

        ProcessStartInfo startInfo = new(
            fileName: mainModule,
            arguments: string.Join(" ", args.Select(a => $"\"{a}\""))
        );

        Process.Start(startInfo);
    }
}
