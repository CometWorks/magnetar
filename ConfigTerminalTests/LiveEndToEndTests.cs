using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Logs;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.Process;
using Xunit;
using Xunit.Abstractions;

namespace Magnetar.ConfigTerminal.Tests;

/// <summary>
/// The real create → start → "Game ready" → stop flow, exercising the exact
/// model/process code paths the New-World wizard drives, against a live DS
/// install + patched Magnetar launcher. Gated behind MAGNETAR_LIVE=1 so it never
/// runs in a normal `dotnet test`. Snapshots and restores the instance's cfg and
/// LastSession.sbl and removes the created world, leaving the instance as found.
/// </summary>
public class LiveEndToEndTests
{
    private readonly ITestOutputHelper output;
    public LiveEndToEndTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public void Create_start_ready_stop()
    {
        if (Environment.GetEnvironmentVariable("MAGNETAR_LIVE") != "1")
            return; // skipped unless explicitly enabled

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var binding = new InstanceBinding
        {
            DataDir = Env("MC_DATA", Path.Combine(home, ".config", "SpaceEngineersDedicated")),
            MagnetarConfigDir = Env("MC_CONFIG", Path.Combine(home, ".config", "Magnetar")),
            MagnetarExePath = Env("MC_EXE", Path.Combine(home, ".local", "share", "Magnetar", "MagnetarInterim")),
            Ds64Dir = Env("MC_DS64", Path.Combine(home, ".steam", "steam", "steamapps", "common", "SpaceEngineersDedicatedServer", "DedicatedServer64")),
        };
        string templateName = Env("MC_TEMPLATE", "Empty World");
        string worldName = "MCTest_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string cfgPath = binding.ConfigPath;
        string sblPath = LastSessionFile.PathFor(binding.SavesPath);
        byte[] cfgBackup = File.Exists(cfgPath) ? File.ReadAllBytes(cfgPath) : null;
        byte[] sblBackup = File.Exists(sblPath) ? File.ReadAllBytes(sblPath) : null;

        var writer = new AtomicFile();
        var process = new MagnetarProcess(binding);
        bool started = false;

        try
        {
            DsInstance instance = DsInstance.Open(binding);
            WorldTemplate template = instance.Templates.Templates
                .FirstOrDefault(t => t.FolderName.Equals(templateName, StringComparison.OrdinalIgnoreCase));
            Assert.True(template != null, $"template '{templateName}' not found");
            Log($"Using template: {template.DisplayName} ({template.FolderName})");

            // --- stage the new world (same steps as NewWorldWizard) ---
            DedicatedConfigDocument cfg = instance.Config;
            WorldConfigDocument seed = WorldTemplateCatalog.OpenSeed(template);
            foreach (OptionDefinition def in OptionRegistry.SessionOptions)
                if (seed.IsSet(def))
                    cfg.Set(def, seed.Get(def));

            OptionDefinition maxPlayers = OptionRegistry.ById("Session.MaxPlayers");
            if (!ConfigDocumentBase.TryParseLong(cfg.Get(maxPlayers), out long mp) || mp <= 0)
                cfg.Set(maxPlayers, "4");

            cfg.WorldName = worldName;
            cfg.PremadeCheckpointPath = template.FolderPath;
            cfg.LoadWorld = string.Empty;
            cfg.IgnoreLastSession = false;
            cfg.Save(writer);
            Log($"Staged world '{worldName}' (PremadeCheckpointPath set, MaxPlayers={cfg.Get(maxPlayers)}).");

            // --- start with -ignorelastsession so the DS runs its new-world branch ---
            var spec = new LaunchSpec { Binding = binding, IgnoreLastSession = true };
            OpResult startResult = process.Start(spec, TimeSpan.FromSeconds(120));
            started = true;
            Log($"Start: ok={startResult.Ok} msg={startResult.Message}");
            Assert.True(startResult.Ok, "server did not report its pid (patched launcher / pid file)");

            ServerStatus running = process.Query();
            Assert.Equal(ServerState.Running, running.State);
            Log($"Running: pid {running.Pid}");

            // --- wait for the DS to reach "Game ready" ---
            bool ready = WaitForReady(binding, TimeSpan.FromMinutes(6));
            Log($"Game ready reached: {ready}");
            Assert.True(ready, "server did not reach 'Game ready' within the timeout");

            // The created world folder should now exist under Saves/.
            string createdWorld = Path.Combine(binding.SavesPath, worldName);
            Log($"Created world folder exists: {Directory.Exists(createdWorld)} ({createdWorld})");

            // --- stop gracefully (SIGTERM → save + quit) ---
            OpResult stopResult = process.Stop(TimeSpan.FromMinutes(3));
            Log($"Stop: ok={stopResult.Ok} msg={stopResult.Message}");
            Assert.True(stopResult.Ok, "server did not stop gracefully");
            Assert.Equal(ServerState.NotRunning, process.Query().State);
            started = false;
        }
        finally
        {
            // Best-effort cleanup so we never leave the user's server running.
            if (started)
            {
                try { process.Stop(TimeSpan.FromMinutes(2)); } catch { }
                try { process.ForceKill(TimeSpan.FromSeconds(20)); } catch { }
            }

            // Restore cfg + LastSession and remove the created test world.
            try { if (cfgBackup != null) File.WriteAllBytes(cfgPath, cfgBackup); } catch { }
            try
            {
                if (sblBackup != null) File.WriteAllBytes(sblPath, sblBackup);
                else if (File.Exists(sblPath)) File.Delete(sblPath);
            }
            catch { }
            try
            {
                string created = Path.Combine(binding.SavesPath, worldName);
                if (Directory.Exists(created)) Directory.Delete(created, true);
            }
            catch { }
            try { if (File.Exists(cfgPath + ".bak")) File.Delete(cfgPath + ".bak"); } catch { }
            Log("Restored cfg + LastSession, removed test world.");
        }
    }

    private bool WaitForReady(InstanceBinding binding, TimeSpan timeout)
    {
        var catalog = new LogCatalog(binding);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            catalog.Scan();
            LogFileInfo game = catalog.ActiveGameLog;
            if (game != null && ReadinessDetector.IsReady(game.Path))
                return true;
            Thread.Sleep(2000);
        }
        return false;
    }

    private static string Env(string name, string fallback)
    {
        string v = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrEmpty(v) ? fallback : v;
    }

    private void Log(string m) => output.WriteLine($"[{DateTime.Now:HH:mm:ss}] {m}");
}
