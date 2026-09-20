using System;
using System.Collections.Generic;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace Magnetar.Legacy.Arguments;

public enum ConsentChoice
{
    Unset,
    Accept,
    Deny,
    Withdraw,
}

/// <summary>Uses Pulsar's command-line library while retaining the server/loader boundary.</summary>
internal sealed class ServerArguments
{
    private const string BareConsentWarning = "-consent without a value is deprecated, use -consent accept";
    public bool Daemon { get; private set; }
    public bool NoImplicitMod { get; private set; }
    public bool Help { get; private set; }
    public bool Version { get; private set; }
    public ConsentChoice Consent { get; private set; }
    public string PreparationDirectory { get; private set; }
    public string ConfigDirectory { get; private set; }
    public string DedicatedServerDirectory { get; private set; }
    public string[] PulsarArguments { get; private set; }
    public List<string> Warnings { get; } = [];

    public static ServerArguments Parse(string[] args)
    {
        using var app = new CommandLineApplication
        {
            OptionsComparison = StringComparison.OrdinalIgnoreCase,
            UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue,
        };
        CommandOption Flag(string name) => app.Option("-" + name, "", CommandOptionType.NoValue);
        CommandOption Value(string name) => app.Option("-" + name + " <value>", "", CommandOptionType.SingleValue);
        var daemon = Flag("daemon");
        var noImplicitMod = Flag("noimplicitmod");
        var help = Flag("help");
        var version = Flag("version");
        var preparation = Value("prepareManaged");
        var config = Value("config");
        var ds64 = Value("ds64");
        var consent = Value("consent");
        var noConsent = Flag("noconsent");
        var withdraw = Flag("withdraw-consent");
        var retiredToken = Value("github-token");
        // Consume DS values here so a path such as /debug cannot become a loader
        // flag. The game still receives the untouched original argv.
        foreach (string name in new[] { "path", "ip", "port", "maxPlayers" }) Value(name);
        // Preserve shared values across Pulsar's token normalizer. Selection and
        // validation of the profile itself remain entirely in ProfilesConfig.
        var shared = new[] { Value("profile"), Value("bin64"), Value("game2") };
        var result = new ServerArguments();
        try { app.Parse(Normalize(args, app.Options, result.Warnings)); }
        catch (CommandParsingException)
        {
            // Library diagnostics can echo rejected values, including a retired
            // command-line token. Do not expose those values to logs or stderr.
            throw new ArgumentException("Invalid server options. Check option syntax and repeated values with -help.");
        }
        result.Daemon = daemon.HasValue();
        result.NoImplicitMod = noImplicitMod.HasValue();
        result.Help = help.HasValue();
        result.Version = version.HasValue();
        result.PreparationDirectory = preparation.Value();
        result.ConfigDirectory = config.Value();
        result.DedicatedServerDirectory = ds64.Value();
        if (consent.HasValue() && !result.Warnings.Contains(BareConsentWarning))
            result.Consent = consent.Value().ToLowerInvariant() switch
            {
                "accept" => ConsentChoice.Accept,
                "deny" => ConsentChoice.Deny,
                "withdraw" => ConsentChoice.Withdraw,
                _ => throw new ArgumentException("Invalid -consent value. Use accept, deny or withdraw."),
            };
        else if (withdraw.HasValue())
        {
            result.Consent = ConsentChoice.Withdraw;
            result.Warnings.Add("-withdraw-consent is deprecated, use -consent withdraw");
        }
        else if (noConsent.HasValue())
        {
            result.Consent = ConsentChoice.Deny;
            result.Warnings.Add("-noconsent is deprecated, use -consent deny");
        }
        else if (consent.HasValue()) result.Consent = ConsentChoice.Accept;
        if (retiredToken.HasValue())
            result.Warnings.Add("-github-token is gone; set the PULSAR_GITHUB_TOKEN environment variable instead");
        result.PulsarArguments = [.. app.RemainingArguments.Where(arg => !arg.StartsWith("-session:", StringComparison.OrdinalIgnoreCase)),
            .. shared.Where(option => option.HasValue()).Select(option => "-" + option.ShortName + "=" + option.Value()),
            "-noSplash", "-noPrompt", "-lazySteam"];
        return result;
    }

    private static string[] Normalize(string[] args, IReadOnlyCollection<CommandOption> options, List<string> warnings)
    {
        var normalized = new List<string>();
        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            if (arg == "--") { normalized.AddRange(args.Skip(index)); break; }
            if (string.IsNullOrEmpty(arg) || arg[0] != '-' && arg[0] != '/') { normalized.Add(arg); continue; }
            string name = arg.TrimStart('-', '/');
            int equals = name.IndexOf('=');
            string inline = equals < 0 ? null : name.Substring(equals + 1);
            if (equals >= 0) name = name.Substring(0, equals);
            if (name.Equals("h", StringComparison.OrdinalIgnoreCase) || name == "?") name = "help";
            if (name.Equals("v", StringComparison.OrdinalIgnoreCase)) name = "version";
            var option = options.FirstOrDefault(candidate => candidate.ShortName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (option is null) { normalized.Add(arg); continue; }
            string canonical = "-" + option.ShortName;
            if (option.OptionType == CommandOptionType.NoValue) { normalized.Add(inline is null ? canonical : canonical + "=" + inline); continue; }
            string value = inline;
            bool bareConsent = name.Equals("consent", StringComparison.OrdinalIgnoreCase) && inline is null
                && (index + 1 == args.Length || args[index + 1].StartsWith("-") || args[index + 1].StartsWith("/"));
            if (bareConsent)
            {
                value = "accept";
                warnings.Add(BareConsentWarning);
            }
            else if (value is null && index + 1 < args.Length && !args[index + 1].StartsWith("-")) value = args[++index];
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(canonical + " requires a value.");
            // Bind values before either parser normalizes option tokens. Absolute
            // paths, spaces and option-looking path names keep their literal bytes.
            normalized.Add(canonical + "=" + value);
        }
        return normalized.ToArray();
    }
}
