using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using HarmonyLib;
using Pulsar.Shared;

namespace Pulsar.Legacy.Loader;

public static class LoaderTools
{
    private const string ContinueArg = "-continue";
    private const string DebugArg = "-debug";

    [DllImport("libc", SetLastError = true)]
    private static extern int execv(string path, string[] argv);

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

        ProcessStartInfo startInfo = new(
            fileName: mainModule,
            arguments: string.Join(" ", args.Select(a => $"\"{a}\""))
        );

        Process.Start(startInfo);
    }

    /// <summary>
    /// This method attempts to disable JIT compiling for the assembly.
    /// This method will force any member access exceptions by methods to be thrown now instead of later.
    /// </summary>
    public static void Precompile(Assembly a)
    {
        Type[] types;
        try
        {
            types = a.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            StringBuilder sb = new();
            sb.AppendLine("LoaderExceptions: ");
            foreach (Exception e2 in e.LoaderExceptions)
                sb.Append(e2).AppendLine();
            LogFile.WriteLine(sb.ToString());
            throw;
        }

        foreach (Type t in types)
        {
            // Static constructors allow for early code execution which can cause issues later in the game
            if (HasStaticConstructor(t))
                continue;

            foreach (
                MethodInfo m in t.GetMethods(
                    BindingFlags.DeclaredOnly
                        | BindingFlags.NonPublic
                        | BindingFlags.Public
                        | BindingFlags.Instance
                        | BindingFlags.Static
                )
            )
            {
                if (m.HasAttribute<HarmonyReversePatch>())
                    throw new Exception(
                        "Harmony attribute 'HarmonyReversePatch' found on the method '"
                            + m.Name
                            + "' is not compatible with Pulsar!"
                    );
                Precompile(m);
            }
        }
    }

    private static void Precompile(MethodInfo m)
    {
        if (!m.IsAbstract && !m.ContainsGenericParameters)
            RuntimeHelpers.PrepareMethod(m.MethodHandle);
    }

    private static bool HasStaticConstructor(Type t)
    {
        return t.GetConstructors(
                BindingFlags.Public
                    | BindingFlags.Static
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
            )
            .Any(c => c.IsStatic);
    }
}
