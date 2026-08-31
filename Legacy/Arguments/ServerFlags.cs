using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Magnetar.Legacy.Arguments;

public enum ConsentChoice
{
    Unset,
    Accept,
    Deny,
    Withdraw,
}

/// <summary>
/// Magnetar-specific command line flags, parsed directly from the raw argv.
///
/// Everything else is handled by Pulsar's own parser
/// (<c>Pulsar.Shared.Arguments.Parser</c> / <c>Flags.Current</c>), which
/// Program feeds after appending the forced headless defaults
/// (-noSplash, -noPrompt, -lazySteam). Magnetar-specific and dedicated-server
/// arguments are unknown to Pulsar's parser and pass through untouched
/// (it collects unrecognized arguments and continues).
///
/// The members used on the -help/-version fast path (the static constructor,
/// PrintHelp, PrintVersion) must not reference any Pulsar.Shared type: they
/// run before the assembly resolver is installed.
/// </summary>
public static class ServerFlags
{
    public static bool Daemon { get; private set; }
    public static bool NoImplicitMod { get; private set; }
    public static string GitHubToken { get; private set; }
    public static ConsentChoice Consent { get; private set; }
    public static bool Help { get; private set; }
    public static bool Version { get; private set; }

    static ServerFlags()
    {
        Daemon = HasArg("daemon");
        NoImplicitMod = HasArg("noimplicitmod");
        GitHubToken =
            GetArgValue("github-token")
            ?? Environment.GetEnvironmentVariable("MAGNETAR_GITHUB_TOKEN");

        if (HasArg("withdraw-consent"))
            Consent = ConsentChoice.Withdraw;
        else if (HasArg("consent"))
            Consent = ConsentChoice.Accept;
        else if (HasArg("noconsent"))
            Consent = ConsentChoice.Deny;

        // -h/-help/-?/--help, plus the /h //help style Pulsar also accepts.
        Help = HasArg("h") || HasArg("help") || HasArg("?");
        Version = HasArg("v") || HasArg("version");
    }

    // Magnetar and dedicated-server options that take a value in the next
    // argv element. Their pairs are stripped from what is handed to Pulsar's
    // parser, for two reasons.
    //
    // The first is simply that Pulsar's parser has no business seeing
    // dedicated-server options.
    //
    // The second is Pulsar's Normalize step, which strips '-'/'/' from a token
    // and rewrites it when the result matches an option short name. Upstream
    // now skips tokens that do not start with '-' or '/', so a relative value
    // like `-path debug` is safe. A '/'-rooted value is not: on Linux
    // `-path /debug` still normalizes to "debug" and would flip Pulsar's
    // -debug. Stripping the pairs closes that off for every value-taking
    // option at once.
    private static readonly string[] valueOptions =
    [
        "config",
        "ds64",
        "github-token",
        "path",
        "ip",
        "port",
        "maxPlayers",
    ];

    /// <summary>
    /// The arguments to hand to Pulsar's <c>Parser.Initialize</c>: the raw
    /// argv minus the value-taking Magnetar/dedicated-server option pairs
    /// (see <see cref="valueOptions"/>) and minus DS session selectors, with
    /// the forced headless defaults appended — a dedicated server has no
    /// display for the splash or the prompt dialogs, and must never block on
    /// (or launch) the Steam client. The dedicated server itself always
    /// receives the ORIGINAL argv, never this filtered list.
    /// </summary>
    public static string[] PulsarParserArgs(string[] args)
    {
        List<string> filtered = [];

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];

            if (arg.StartsWith("-session:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (valueOptions.Any(name => IsOption(arg, name)))
            {
                index++; // skip the value as well
                continue;
            }

            filtered.Add(arg);
        }

        filtered.Add("-noSplash");
        filtered.Add("-noPrompt");
        filtered.Add("-lazySteam");
        return [.. filtered];
    }

    public static void LogFlags()
    {
        List<string> changed = [];

        if (Daemon)
            changed.Add("Daemon");
        if (NoImplicitMod)
            changed.Add("NoImplicitMod");
        if (!string.IsNullOrWhiteSpace(GitHubToken))
            changed.Add("GitHubToken");
        if (Consent != ConsentChoice.Unset)
            changed.Add(Consent.ToString());

        if (changed.Count > 0)
            Pulsar.Shared.LogFile.WriteLine($"Magnetar flags: {string.Join(" ", changed)}");
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
        Console.WriteLine("  -noimplicitmod      Do not auto-load the MagnetarMod client companion mod");
        Console.WriteLine("  -github-token <pat> GitHub token for API downloads (lifts the anonymous");
        Console.WriteLine("                      rate limit; also reaches private repositories)");
        Console.WriteLine();
        Console.WriteLine("Plugin loader options (shared with Pulsar):");
        Console.WriteLine("  -profile <name>     Force a specific plugin profile");
        Console.WriteLine("  -safeMode           Start with user plugins disabled");
        Console.WriteLine("  -bare               Disable force-loading core (compatibility) plugins");
        Console.WriteLine("  -sources            Enable custom plugin sources");
        Console.WriteLine("  -hardened           Load only trusted mods, stripping untrusted Workshop mods");
        Console.WriteLine("  -multiInstance      Allow multiple launcher instances on this machine");
        Console.WriteLine("  -useHome            Store Magnetar data under the user's app-data folder");
        Console.WriteLine("                      instead of next to the launcher");
        Console.WriteLine("  -lazyPreload        Reuse existing preloader assemblies");
        Console.WriteLine("  -stableLogs         Overwrite game logs instead of timestamping them");
        Console.WriteLine("  -noUpdate           Disable update checks");
        Console.WriteLine("  -preRelease         Use pre-release updates");
        Console.WriteLine("  -mkCheck            Regenerate the Libraries checksum file (bitrot detection)");
        Console.WriteLine("  -debug              Launch the managed debugger at startup");
        Console.WriteLine("  -debugMods          Build game mods in debug mode");
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
        Console.WriteLine("  -version, -v        Show the Magnetar version and exit");
    }

    // Matches -name, --name and /name (the prefix styles Pulsar accepts).
    private static bool IsOption(string arg, string name)
    {
        if (string.IsNullOrEmpty(arg) || (arg[0] != '-' && arg[0] != '/'))
            return false;

        string trimmed = arg.TrimStart('-', '/');
        return trimmed.Equals(name, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasArg(string argument) =>
        Environment.GetCommandLineArgs().Skip(1).Any(arg => IsOption(arg, argument));

    private static string GetArgValue(string argument)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (var index = 1; index < args.Length - 1; index++)
        {
            if (IsOption(args[index], argument))
                return args[index + 1];
        }

        return null;
    }
}
