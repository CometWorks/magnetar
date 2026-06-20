using System;
using System.IO;
using Pulsar.Shared.Config;

namespace Pulsar.Shared.Votes;

public static class ConsentManager
{
    public static bool Granted { get; private set; }
    public static bool PendingServerConsent { get; private set; }
    public static string PlayerHash { get; private set; }

    // Withdraws consent: asks the server to erase this instance's data, deletes
    // instance.id, and records the denial locally. Used by -withdraw-consent,
    // after which Magnetar exits without starting the server. Best effort: a
    // server that cannot be reached still leaves telemetry disabled locally.
    public static void Withdraw(string votesServer)
    {
        ConfigManager mgr = ConfigManager.Instance;
        CoreConfig config = mgr.Core;

        string existingId = mgr.ReadInstanceId();
        if (existingId != null)
        {
            VotesClient.BaseUrl = config.VotesServerBaseUrl ?? votesServer;
            if (VotesClient.Consent(false, DerivePlayerHash(existingId)))
            {
                LogFile.WriteLine("Consent: withdrawn from the statistics server");
                Console.WriteLine("Consent withdrawn: your data has been erased from the statistics server.");
            }
            else
            {
                LogFile.Error("Consent: failed to withdraw from the statistics server");
                Console.WriteLine("Consent withdraw: could not reach the statistics server; telemetry disabled locally.");
            }
            mgr.DeleteInstanceId();
        }
        else
        {
            LogFile.WriteLine("Consent: no instance.id to withdraw from the server");
            Console.WriteLine("Consent: nothing to withdraw (no local consent on record); recorded denial.");
        }

        config.DataHandlingConsent = false;
        config.DataHandlingConsentDate = DateTime.UtcNow.ToString("o");
        config.Save();
    }

    public static void Resolve()
    {
        ConsentChoice flag = Flags.Consent;
        ConfigManager mgr = ConfigManager.Instance;
        CoreConfig config = mgr.Core;

        // Reconcile: a grant (true) is only valid alongside its instance.id, so a
        // leftover/legacy grant without one is stale — clear it to undecided. A
        // denial (false) intentionally has no instance.id, so it must be kept;
        // otherwise the user would be re-prompted on every start.
        if (!mgr.HasInstanceId() && config.DataHandlingConsent == true)
        {
            config.DataHandlingConsent = null;
            config.DataHandlingConsentDate = null;
            config.Save();
        }

        switch (flag)
        {
            case ConsentChoice.Deny:
                LogFile.WriteLine("Consent: -noconsent specified, telemetry suppressed this run");
                // Leave instance.id in place if it exists
                return;

            case ConsentChoice.Accept:
                Accept(mgr, config, "-consent flag");
                return;
        }

        // No flag — check stored state
        if (mgr.HasInstanceId())
        {
            // instance.id exists → consent was granted previously
            string id = mgr.ReadInstanceId();
            PlayerHash = DerivePlayerHash(id);
            Granted = true;
            PendingServerConsent = true; // idempotent re-register
            LogFile.WriteLine("Consent: active (instance.id present)");
            return;
        }

        if (config.DataHandlingConsent == false)
        {
            // User previously declined — don't re-prompt
            LogFile.WriteLine("Consent: previously declined");
            return;
        }

        // Undecided — prompt if interactive TTY, otherwise silent no-consent
        if (!Tools.IsInteractiveTerminal())
        {
            LogFile.Warn("Consent: no interactive terminal, telemetry disabled. Use -consent to enable.");
            return;
        }

        // Interactive prompt — loop until Y or N
        Console.WriteLine();
        Console.WriteLine("Magnetar can send anonymous plugin usage statistics to help prioritize development.");
        Console.WriteLine("What is sent: the list of enabled plugin IDs (including built-in compatibility plugins),");
        Console.WriteLine("tied to a random anonymous instance ID stored locally. Nothing else is collected — no");
        Console.WriteLine("personal data, no account or Steam ID, no IP address, no world or server content.");
        Console.WriteLine("You can change this later with -consent, -noconsent, or -withdraw-consent.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Enable anonymous plugin usage statistics? Type Y or N: ");

            string input;
            try
            {
                input = Console.ReadLine()?.Trim().ToUpperInvariant();
            }
            catch (IOException)
            {
                input = null;
            }

            if (input == null)
            {
                // stdin reached EOF or is not actually readable (e.g. launched
                // from an IDE run console with no real keyboard). Do not spin or
                // block again: leave the choice undecided and disable telemetry
                // for this run. The flags still work non-interactively.
                LogFile.Warn("Consent: no usable console input, telemetry disabled this run. Use -consent or -noconsent.");
                return;
            }

            if (input == "Y")
            {
                Accept(mgr, config, "interactive prompt");
                return;
            }
            if (input == "N")
            {
                Deny(config, "interactive prompt");
                return;
            }
        }
    }

    // Records granted consent and persists it immediately: the random UUID4
    // instance.id is the server-side identity, and config.xml records the
    // decision so it survives an interrupted startup and is human-visible.
    private static void Accept(ConfigManager mgr, CoreConfig config, string source)
    {
        string id = mgr.CreateInstanceId();
        PlayerHash = DerivePlayerHash(id);
        Granted = true;
        PendingServerConsent = true;
        config.DataHandlingConsent = true;
        config.DataHandlingConsentDate = DateTime.UtcNow.ToString("o");
        config.Save();
        LogFile.WriteLine($"Consent: granted via {source}");
    }

    // Records a denial immediately in config.xml so the user is not re-prompted.
    private static void Deny(CoreConfig config, string source)
    {
        config.DataHandlingConsent = false;
        config.DataHandlingConsentDate = DateTime.UtcNow.ToString("o");
        config.Save();
        LogFile.WriteLine($"Consent: declined via {source}");
    }

    private static string DerivePlayerHash(string instanceId)
    {
        // Strip dashes from the UUID, lowercase, take first 20 chars.
        // This satisfies the server's ^[a-z0-9]{20}$ validation.
        return instanceId.Replace("-", "").ToLowerInvariant().Substring(0, 20);
    }
}
