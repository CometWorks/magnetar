using System;

namespace Pulsar.Shared;

/// <summary>
/// Plain-text console progress for plugin downloads and compilation.
/// Replaces the former WinForms splash screen.
/// </summary>
public static class PluginProgress
{
    public static int Downloaded { get; private set; }
    public static int Compiled { get; private set; }

    public static void ReportDownloading(string name)
    {
        Downloaded++;
        Write($"Downloading {name}");
    }

    public static void ReportCompiling(string name)
    {
        Write($"Compiling {name}");
    }

    public static void ReportCompiled(string name)
    {
        Compiled++;
        Write($"Compiled {name}");
    }

    public static void ReportSummary(int loaded, int implicitlyLoaded)
    {
        Write($"Downloaded {Plural(Downloaded)}");
        Write($"Compiled {Plural(Compiled)}");
        Write($"Loaded {Plural(loaded)} ({implicitlyLoaded} implicit)");
    }

    private static string Plural(int count) => count == 1 ? "1 plugin" : $"{count} plugins";

    private static void Write(string message)
    {
        Console.Out.WriteLine($"[Magnetar] {message}");
        LogFile.WriteLine(message);
    }
}
