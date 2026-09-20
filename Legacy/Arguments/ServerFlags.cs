using System;
using System.Collections.Generic;
using System.Reflection;

namespace Magnetar.Legacy.Arguments;

/// <summary>Server option state and help. Shared loader options still belong to Pulsar.</summary>
public static class ServerFlags
{
    private static ServerArguments current;
    public static bool Daemon => current.Daemon;
    public static bool NoImplicitMod => current.NoImplicitMod;
    public static ConsentChoice Consent => current.Consent;
    public static bool Help => current.Help;
    public static bool Version => current.Version;
    public static bool PrepareManaged => current.PreparationDirectory != null;
    public static string PreparationDirectory => current.PreparationDirectory;
    public static string ConfigDirectory => current.ConfigDirectory;
    public static string DedicatedServerDirectory => current.DedicatedServerDirectory;
    public static string[] PulsarArguments => current.PulsarArguments;

    // Called after the assembly resolver is installed, before native bootstrap.
    public static void Initialize(string[] args) => current = ServerArguments.Parse(args);

    public static void LogFlags()
    {
        List<string> changed = [];

        if (Daemon)
            changed.Add("Daemon");
        if (NoImplicitMod)
            changed.Add("NoImplicitMod");
        if (Consent != ConsentChoice.Unset)
            changed.Add(Consent.ToString());

        if (changed.Count > 0)
            Pulsar.Shared.LogFile.WriteLine($"Magnetar flags: {string.Join(" ", changed)}");

        foreach (string warning in current.Warnings)
        {
            Pulsar.Shared.LogFile.Warn(warning);
            // Also on the console: an operator running a stale launch line has
            // to see this without going looking for the log.
            Console.Error.WriteLine($"Warning: {warning}");
        }
    }

    public static void PrintVersion()
    {
        System.Version version = Assembly.GetEntryAssembly()?.GetName().Version;
        Console.WriteLine(version is null ? "Magnetar" : $"Magnetar v{version.ToString(3)}");
    }

    public static void PrintHelp()
    {
        System.Version version = Assembly.GetEntryAssembly()?.GetName().Version;
        string versionText = version is null ? "" : $" v{version.ToString(3)}";
        string launcher = Assembly.GetEntryAssembly()?.GetName().Name ?? "MagnetarInterim";

        Console.WriteLine($"Magnetar{versionText} - Space Engineers Dedicated Server plugin loader");
        Console.WriteLine();
        Console.WriteLine($"Usage: {launcher} [options]");
        Console.WriteLine();
        Console.WriteLine("Magnetar options:");
        Console.WriteLine("  -config <dir>       Use a custom Magnetar config and log directory");
        Console.WriteLine("  -ds64 <dir>         Path to the Space Engineers DedicatedServer64 directory");
        Console.WriteLine("                      (overrides auto-detection)");
        Console.WriteLine("  -daemon             Detach from the parent process and console so the");
        Console.WriteLine("                      server keeps running after the parent exits");
        Console.WriteLine("  -prepareManaged <dir> Compile and export managed plugin bundles and SDK defaults");
        Console.WriteLine("                      without starting the server (Linux/CoreCLR; new output directory)");
        Console.WriteLine("  -noimplicitmod      Do not auto-load the MagnetarMod client companion mod");
        Console.WriteLine();
        // Only the Pulsar flags that change something on a dedicated server are
        // listed. Pulsar's parser still accepts the rest (client-only ones like
        // -f12Menu and -keepIntro, and -sources/-noUpdate/-preRelease, which
        // reach no live code path here); they are simply not advertised.
        Console.WriteLine("Plugin loader options (shared with Pulsar):");
        Console.WriteLine("  -profile <name|file> Force a specific plugin profile by name or file path");
        Console.WriteLine("  -safeMode           Start with user plugins disabled");
        Console.WriteLine("  -bare               Disable force-loading core (compatibility) plugins");
        Console.WriteLine("  -hardened           Load only trusted mods, stripping untrusted Workshop mods");
        Console.WriteLine("  -multiInstance      Allow multiple launcher instances on this machine");
        Console.WriteLine("  -useHome            Store Magnetar data under the user's app-data folder");
        Console.WriteLine("                      instead of next to the launcher");
        Console.WriteLine("  -lazyPreload        Reuse existing preloader assemblies");
        Console.WriteLine("  -stableLogs         Overwrite game logs instead of timestamping them");
        Console.WriteLine("  -mkCheck            Regenerate the Libraries checksum file (bitrot detection)");
        Console.WriteLine("  -debug              Launch the managed debugger at startup");
        Console.WriteLine("  -debugMods          Build game mods in debug mode");
        Console.WriteLine("  -debugCompileAll    Compile-check every available plugin (diagnostics)");
        Console.WriteLine();
        Console.WriteLine("Telemetry consent:");
        Console.WriteLine("  -consent <choice>   accept   Send anonymous plugin usage statistics (remembers");
        Console.WriteLine("                               the decision)");
        Console.WriteLine("                      deny     Do not send usage statistics for this run only");
        Console.WriteLine("                      withdraw Withdraw consent, erase the data from the statistics");
        Console.WriteLine("                               server, then exit without starting the server");
        Console.WriteLine();
        Console.WriteLine("Dedicated server options (passed through):");
        Console.WriteLine("  -path <dir>         Server instance directory (worlds and Dedicated.cfg);");
        Console.WriteLine("                      Magnetar enables -console automatically when this is set");
        Console.WriteLine("  -console            Run headless in console mode");
        Console.WriteLine("  -noconsole          Run headless without a console window");
        Console.WriteLine("  -session:<path>     Load the world save at <path>");
        Console.WriteLine("  -ignorelastsession  Do not auto-load the last session");
        Console.WriteLine("  -maxPlayers <n>     Override the maximum player count");
        Console.WriteLine("  -ip <addr>          Override the bind IP address");
        Console.WriteLine("  -port <n>           Override the server port");
        Console.WriteLine("  -checkAlive         Shut down when the parent process exits");
        Console.WriteLine();
        Console.WriteLine("Help:");
        Console.WriteLine("  -help, -h, --help   Show this help and exit");
        Console.WriteLine("  -version, -v        Show the Magnetar version and exit");
    }
}
