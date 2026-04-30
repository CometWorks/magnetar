using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using HarmonyLib;
using Pulsar.Shared;

namespace Pulsar.Legacy.Loader;

public static class LoaderTools
{
    private const string ContinueArg = "-continue";
    private const string DebugArg = "-debug";

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

        ProcessStartInfo startInfo = new(
            fileName: Application.ExecutablePath,
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
