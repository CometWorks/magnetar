using System;
using System.IO;
using System.Runtime.InteropServices;
using Magnetar.Legacy.Arguments;
using Pulsar.Shared;
using Pulsar.Shared.Config;

namespace Magnetar.Legacy.Stats;

/// <summary>
/// Owns the telemetry consent state machine and the instance.id file that
/// anchors it (a random UUID under the Magnetar config directory; its presence
/// is the durable record that consent was granted). Pulsar's CoreConfig stores
/// only a plain bool + timestamp, so the tri-state is encoded as:
///   instance.id present            -> granted
///   no instance.id, date recorded  -> previously declined
///   no instance.id, no date        -> undecided (prompt when interactive)
/// Only the first 20 hex characters of the UUID ever leave the machine.
/// </summary>
public static class ConsentManager
{
    public static bool Granted { get; private set; }
    public static bool PendingServerConsent { get; private set; }
    public static string PlayerHash { get; private set; }

    // Withdraws consent: asks the server to erase this instance's data, deletes
    // instance.id, and records the denial locally. Used by -consent withdraw,
    // after which Magnetar exits without starting the server. Best effort: a
    // server that cannot be reached still leaves telemetry disabled locally.
    public static void Withdraw(string votesServer)
    {
        CoreConfig config = ConfigManager.Instance.Core;

        string existingId = ReadInstanceId();
        if (existingId != null)
        {
            VotesClient.BaseUrl = config.StatsServerBaseUrl ?? votesServer;
            if (VotesClient.Consent(false, DerivePlayerHash(existingId)))
            {
                LogFile.WriteLine("Consent: withdrawn from the statistics server");
                Console.WriteLine(
                    "Consent withdrawn: your data has been erased from the statistics server."
                );
            }
            else
            {
                LogFile.Error("Consent: failed to withdraw from the statistics server");
                Console.WriteLine(
                    "Consent withdraw: could not reach the statistics server; telemetry disabled locally."
                );
            }
            DeleteInstanceId();
        }
        else
        {
            LogFile.WriteLine("Consent: no instance.id to withdraw from the server");
            Console.WriteLine(
                "Consent: nothing to withdraw (no local consent on record); recorded denial."
            );
            DeleteInstanceId(); // clears a corrupted (unreadable) id file too
        }

        Deny(config, "-consent withdraw");
    }

    public static void Resolve()
    {
        ConsentChoice flag = ServerFlags.Consent;
        CoreConfig config = ConfigManager.Instance.Core;
        string instanceId = ReadInstanceId();

        // Reconcile: a grant is only valid alongside its instance.id, so a
        // leftover/legacy grant without one is stale — clear it to undecided.
        if (instanceId is null && config.DataHandlingConsent)
        {
            config.DataHandlingConsent = false;
            config.DataHandlingConsentDate = null;
            config.Save();
        }

        switch (flag)
        {
            case ConsentChoice.Deny:
                LogFile.WriteLine("Consent: -consent deny given, telemetry suppressed this run");
                // Leave instance.id in place if it exists
                return;

            case ConsentChoice.Accept:
                Accept(config, "-consent accept flag");
                return;
        }

        // No flag — check stored state
        if (instanceId is not null)
        {
            // instance.id exists → consent was granted previously
            PlayerHash = DerivePlayerHash(instanceId);
            Granted = true;
            PendingServerConsent = true; // idempotent re-register
            LogFile.WriteLine("Consent: active (instance.id present)");
            return;
        }

        if (config.DataHandlingConsentDate != null)
        {
            // User previously declined — don't re-prompt
            LogFile.WriteLine("Consent: previously declined");
            return;
        }

        // Undecided — prompt if interactive TTY, otherwise silent no-consent
        if (!IsInteractiveTerminal())
        {
            LogFile.Warn(
                "Consent: no interactive terminal, telemetry disabled. Use -consent accept to enable."
            );
            return;
        }

        // Interactive prompt — loop until Y or N
        Console.WriteLine();
        Console.WriteLine(
            "Magnetar can send anonymous plugin usage statistics to help prioritize development."
        );
        Console.WriteLine(
            "What is sent: the list of enabled plugin IDs (including built-in compatibility plugins),"
        );
        Console.WriteLine(
            "tied to a random anonymous instance ID stored locally. Nothing else is collected — no"
        );
        Console.WriteLine(
            "personal data, no account or Steam ID, no IP address, no world or server content."
        );
        Console.WriteLine(
            "You can change this later with -consent accept, -consent deny or -consent withdraw."
        );
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
                LogFile.Warn(
                    "Consent: no usable console input, telemetry disabled this run. Use -consent accept or -consent deny."
                );
                return;
            }

            if (input == "Y")
            {
                Accept(config, "interactive prompt");
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
    private static void Accept(CoreConfig config, string source)
    {
        string id = CreateInstanceId();
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

    private static string InstanceIdPath =>
        Path.Combine(ConfigManager.Instance.PulsarDir, "instance.id");

    // Returns null for a missing OR corrupted/truncated file (one that cannot
    // yield the 20-character player hash), so a damaged id never crashes
    // startup — the state machine then treats consent as undecided.
    private static string ReadInstanceId()
    {
        if (!File.Exists(InstanceIdPath))
            return null;

        string id = File.ReadAllText(InstanceIdPath).Trim();
        return id.Replace("-", "").Length >= 20 ? id : null;
    }

    private static string CreateInstanceId()
    {
        if (ReadInstanceId() is string existing)
            return existing;

        string id = Guid.NewGuid().ToString("D");
        File.WriteAllText(InstanceIdPath, id);
        return id;
    }

    private static void DeleteInstanceId()
    {
        try
        {
            File.Delete(InstanceIdPath);
        }
        catch (Exception e)
        {
            LogFile.Error($"Failed to delete instance.id: {e.Message}");
        }
    }

    [DllImport("libc", EntryPoint = "isatty")]
    private static extern int IsAttyLinux(int fd);

    private static bool IsInteractiveTerminal()
    {
        if (ServerFlags.Daemon)
            return false;

#if NETCOREAPP
        if (OperatingSystem.IsLinux())
            return IsAttyLinux(0) != 0;
#endif
        return !Console.IsInputRedirected;
    }
}
