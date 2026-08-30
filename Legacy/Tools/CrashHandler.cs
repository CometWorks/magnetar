using System;
using System.Runtime.InteropServices;
using Pulsar.Shared;

namespace Magnetar.Legacy;

/// <summary>
/// Last-resort native (SEH) crash reporting on Windows. Without it a native
/// access violation (Havok, Steamworks, ...) kills the server process with no
/// line in the log, leaving the operator with zero diagnostics.
/// </summary>
internal static class CrashHandler
{
    private delegate int UnhandledExceptionFilterDelegate(IntPtr exceptionInfo);

    [DllImport("kernel32.dll")]
    private static extern IntPtr SetUnhandledExceptionFilter(
        UnhandledExceptionFilterDelegate lpTopLevelExceptionFilter
    );

    // Rooted deliberately: a collected delegate would crash the crash handler.
    private static UnhandledExceptionFilterDelegate nativeFilterDelegate;

    public static void InstallNative(string label)
    {
        // SEH is a Windows kernel concept; the dedicated server on Linux
        // surfaces native faults via SIGSEGV/etc. that the CoreCLR signal
        // handler already turns into managed exceptions, so we no-op here.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        nativeFilterDelegate = exceptionInfo =>
        {
            Console.Error.WriteLine($"[{label}] Native crash detected (unhandled SEH exception)");
            Console.Error.Flush();
            LogFile.Error("Native crash detected (unhandled SEH exception)");
            Environment.Exit(-1);
            return 0;
        };
        SetUnhandledExceptionFilter(nativeFilterDelegate);
    }
}
