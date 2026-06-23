using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Pulsar.Shared;

public enum UpdateType
{
    None,
    Standard,
    Tester,
}

public enum ConsentChoice
{
    Unset,
    Accept,
    Deny,
    Withdraw,
}

public static class Flags
{
    public static UpdateType UpdateType { get; private set; }
    public static bool ExternalDebug { get; private set; }
    public static bool DebugMenu { get; private set; }
    public static bool CustomSources { get; private set; }
    public static bool ContinueGame { get; private set; }
    public static bool CheckAllPlugins { get; private set; }
    public static bool GameIntroVideo { get; private set; }
    public static bool MakeCheckFile { get; private set; }
    public static bool TrustedMods { get; private set; }
    public static bool Daemon { get; private set; }
    public static bool NoImplicitMod { get; private set; }
    public static ConsentChoice Consent { get; private set; }
    public static bool Help { get; private set; }

    static Flags()
    {
        if (HasArg("noupdate"))
            UpdateType = UpdateType.None;
        else if (HasArg("prerelease"))
            UpdateType = UpdateType.Tester;
        else
            UpdateType = UpdateType.Standard;

        ExternalDebug = HasArg("debug");
        DebugMenu = HasArg("f12menu");
        CustomSources = HasArg("sources");
        ContinueGame = HasArg("continue");
        CheckAllPlugins = HasArg("debugCompileAll");
        GameIntroVideo = HasArg("keepintro");
        MakeCheckFile = HasArg("mkcheck");
        TrustedMods = HasArg("hardened");
        Daemon = HasArg("daemon");
        NoImplicitMod = HasArg("noimplicitmod");

        if (HasArg("withdraw-consent"))
            Consent = ConsentChoice.Withdraw;
        else if (HasArg("consent"))
            Consent = ConsentChoice.Accept;
        else if (HasArg("noconsent"))
            Consent = ConsentChoice.Deny;

        // -h, -help and --help (the latter matches as the "-help" argument).
        Help = HasArg("h") || HasArg("help") || HasArg("-help");
    }

    public static void LogFlags()
    {
        List<string> changed = [];

        if (UpdateType == UpdateType.None)
            changed.Add("NoUpdates");
        else if (UpdateType == UpdateType.Tester)
            changed.Add("EarlyUpdates");

        if (ExternalDebug)
            changed.Add("ExternalDebug");
        if (DebugMenu)
            changed.Add("DebugMenu");
        if (CustomSources)
            changed.Add("CustomSources");
        if (ContinueGame)
            changed.Add("ContinueGame");
        if (CheckAllPlugins)
            changed.Add("CheckAllPlugins");
        if (GameIntroVideo)
            changed.Add("GameIntroVideo");
        if (MakeCheckFile)
            changed.Add("MakeCheckFile");
        if (TrustedMods)
            changed.Add("TrustedMods");
        if (Daemon)
            changed.Add("Daemon");
        if (NoImplicitMod)
            changed.Add("NoImplicitMod");
        if (Consent != ConsentChoice.Unset)
            changed.Add(Consent.ToString());

        if (changed.Count > 0)
            LogFile.WriteLine($"Enabled flags: {string.Join(" ", changed)}");
    }

    public static void PrintHelp()
    {
        Version version = Assembly.GetEntryAssembly()?.GetName().Version;
        string versionText = version is null ? "" : $" v{version.ToString(3)}";

        Console.WriteLine($"Magnetar{versionText} - Space Engineers Dedicated Server plugin loader");
        Console.WriteLine();
        Console.WriteLine("Usage: MagnetarInterim [options]");
        Console.WriteLine();
        Console.WriteLine("Magnetar options:");
        Console.WriteLine("  -config <dir>       Use a custom Magnetar config and log directory");
        Console.WriteLine("  -ds64 <dir>         Path to the Space Engineers DedicatedServer64 directory");
        Console.WriteLine("                      (overrides auto-detection)");
        Console.WriteLine("  -daemon             Detach from the parent process and console so the");
        Console.WriteLine("                      server keeps running after the parent exits");
        Console.WriteLine("  -hardened           Load only trusted mods, stripping untrusted Workshop mods");
        Console.WriteLine("  -noimplicitmod      Do not auto-load the MagnetarMod client companion mod");
        Console.WriteLine("  -mkcheck            Regenerate the Libraries checksum file (bitrot detection)");
        Console.WriteLine("  -keepintro          Do not suppress the game intro video");
        Console.WriteLine("  -debug              Launch the managed debugger at startup");
        Console.WriteLine("  -f12menu            Enable the in-game F12 debug menu");
        Console.WriteLine("  -debugCompileAll    Compile-check every available plugin (diagnostics)");
        Console.WriteLine();
        Console.WriteLine("Telemetry consent:");
        Console.WriteLine("  -consent            Enable sending anonymous plugin usage statistics (remembers the decision)");
        Console.WriteLine("  -noconsent          Disable sending usage statistics for this run only");
        Console.WriteLine("  -withdraw-consent   Withdraw consent and erase data from the statistics server");
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
    }

    private static bool HasArg(string argument) =>
        Environment
            .GetCommandLineArgs()
            .Any(arg => arg.Equals($"-{argument}", StringComparison.OrdinalIgnoreCase));
}
