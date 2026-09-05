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
    public static ConsentChoice Consent { get; private set; }

    /// <summary>
    /// Set when -consent was given an unusable value. Program prints it and
    /// exits with status 1 rather than starting the server on a guess.
    /// </summary>
    public static string ConsentError { get; private set; }

    public static bool Help { get; private set; }
    public static bool Version { get; private set; }

    // Warnings about retired flags, collected while parsing and emitted from
    // LogFlags once the log file exists (parsing runs before LogFile.Init).
    private static readonly List<string> deprecated = [];

    static ServerFlags()
    {
        Daemon = HasArg("daemon");
        NoImplicitMod = HasArg("noimplicitmod");

        // Removed in 2.1.0: the token is taken from PULSAR_GITHUB_TOKEN alone.
        // A command line token is readable by every local user through
        // /proc/<pid>/cmdline, an environment variable is not.
        if (HasArg("github-token"))
            deprecated.Add("-github-token is gone; set the PULSAR_GITHUB_TOKEN environment variable instead");

        ParseConsent();

        // -h/-help/-?/--help, plus the /h //help style Pulsar also accepts.
        Help = HasArg("h") || HasArg("help") || HasArg("?");
        Version = HasArg("v") || HasArg("version");
    }

    // Whether -consent was followed by its value, so PulsarParserArgs knows
    // how many tokens to strip. A legacy bare -consent has no value and must
    // not swallow the option that follows it.
    private static bool consentHasValue;

    // -consent <accept|deny|withdraw> replaced the -consent / -noconsent /
    // -withdraw-consent trio in 2.1.0; the legacy spellings still work for one
    // release, with a warning. A -consent whose next token is missing or looks
    // like an option is the legacy bare form, not a bad value.
    private static void ParseConsent()
    {
        bool given = HasArg("consent");
        string value = GetArgValue("consent");

        if (given && value != null && !LooksLikeOption(value))
        {
            consentHasValue = true;

            switch (value.ToLowerInvariant())
            {
                case "accept":
                    Consent = ConsentChoice.Accept;
                    return;

                case "deny":
                    Consent = ConsentChoice.Deny;
                    return;

                case "withdraw":
                    Consent = ConsentChoice.Withdraw;
                    return;

                default:
                    ConsentError =
                        $"Invalid -consent value '{value}'. Use accept, deny or withdraw.";
                    return;
            }
        }

        if (HasArg("withdraw-consent"))
        {
            Consent = ConsentChoice.Withdraw;
            deprecated.Add("-withdraw-consent is deprecated, use -consent withdraw");
            return;
        }

        if (HasArg("noconsent"))
        {
            Consent = ConsentChoice.Deny;
            deprecated.Add("-noconsent is deprecated, use -consent deny");
            return;
        }

        if (given)
        {
            Consent = ConsentChoice.Accept;
            deprecated.Add("-consent without a value is deprecated, use -consent accept");
        }
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
        // Retired in 2.1.0, still stripped for one release so a legacy
        // invocation cannot leak its token into Pulsar's parser or the log.
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

            // Same rule as valueOptions: no Magnetar option value ever reaches
            // Pulsar's parser. Handled apart from the table because the legacy
            // bare -consent carries no value to skip.
            if (IsOption(arg, "consent"))
            {
                if (consentHasValue)
                    index++;
                continue;
            }

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
        if (Consent != ConsentChoice.Unset)
            changed.Add(Consent.ToString());

        if (changed.Count > 0)
            Pulsar.Shared.LogFile.WriteLine($"Magnetar flags: {string.Join(" ", changed)}");

        foreach (string warning in deprecated)
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
        Console.WriteLine("  -noimplicitmod      Do not auto-load the MagnetarMod client companion mod");
        Console.WriteLine();
        // Only the Pulsar flags that change something on a dedicated server are
        // listed. Pulsar's parser still accepts the rest (client-only ones like
        // -f12Menu and -keepIntro, and -sources/-noUpdate/-preRelease, which
        // reach no live code path here); they are simply not advertised.
        Console.WriteLine("Plugin loader options (shared with Pulsar):");
        Console.WriteLine("  -profile <name>     Force a specific plugin profile");
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

    // Matches -name, --name and /name (the prefix styles Pulsar accepts).
    private static bool IsOption(string arg, string name)
    {
        if (string.IsNullOrEmpty(arg) || (arg[0] != '-' && arg[0] != '/'))
            return false;

        string trimmed = arg.TrimStart('-', '/');
        return trimmed.Equals(name, StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeOption(string arg) =>
        !string.IsNullOrEmpty(arg) && (arg[0] == '-' || arg[0] == '/');

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
