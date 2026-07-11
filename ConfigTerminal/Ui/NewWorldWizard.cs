using System;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Logs;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.Process;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// DS-faithful new-world creation (§2.7 / §9.6): pick a template, name the world,
/// seed the cfg's SessionSettings from the template, stage the cfg
/// (WorldName / PremadeCheckpointPath / clear LoadWorld), then optionally start
/// the DS with -ignorelastsession so it materializes the world and reaches
/// "Game ready". Post-creation the staged PremadeCheckpointPath is cleared.
/// </summary>
internal static class NewWorldWizard
{
    public static void Run(AppShell shell)
    {
        DsInstance instance = shell.Instance;
        if (instance.Templates.Templates.Count == 0)
        {
            Dialogs.Error("New World", "No world templates found. A DS install (Content/CustomWorlds) is required; set it with -ds64.");
            return;
        }

        WorldTemplate template = PickTemplate(instance);
        if (template == null)
            return;

        string name = PromptName(template, instance);
        if (string.IsNullOrWhiteSpace(name))
            return;

        // Seed the cfg session settings from the template ("config created from
        // the world, so it matches"), then stage the creation fields.
        DedicatedConfigDocument cfg = instance.Config;
        SeedSessionSettings(cfg, template);
        EnsureNonZeroMaxPlayers(cfg);

        cfg.WorldName = name;
        cfg.PremadeCheckpointPath = template.FolderPath;
        cfg.LoadWorld = string.Empty;

        string summary =
            $"Stage new world '{name}' from template '{template.DisplayName}'?\n\n" +
            "cfg writes:\n" +
            $"  • WorldName = {name}\n" +
            $"  • PremadeCheckpointPath = {template.FolderName}\n" +
            "  • LoadWorld = (cleared)\n" +
            "  • SessionSettings = seeded from the template\n\n" +
            "The DS materializes the world on the next -ignorelastsession start.";

        if (!Dialogs.Confirm("Create world", summary, "Stage", "Cancel"))
            return;

        try
        {
            cfg.Save(shell.Writer);
        }
        catch (Exception e)
        {
            Dialogs.Error("Save failed", e.Message);
            return;
        }
        shell.ReloadInstance();

        int choice = MessageBox.Query("Create world",
            "\nConfig staged. Create the world now (start the server), or on the next start?\n",
            "Create now", "On next start", "Cancel");

        if (choice == 0)
            CreateNow(shell);
        else if (choice == 1)
            Dialogs.Info("Staged", "The world will be created on the next tool-initiated start (which will pass -ignorelastsession).");
    }

    private static void CreateNow(AppShell shell)
    {
        if (shell.Monitor.Latest.State == ServerState.Running)
        {
            if (!Dialogs.Confirm("Server running", "The server must be stopped before creating a world. Stop it now?"))
                return;
            OpResult stop = shell.Process.Stop(TimeSpan.FromMinutes(2));
            if (!stop.Ok)
            {
                Dialogs.Error("Stop failed", stop.Message);
                return;
            }
            shell.Monitor.Poll();
        }

        Dialogs.Info("Creating",
            "Starting the server with -ignorelastsession to create the world.\n" +
            "Watch the status bar and Tools → Logs for 'Game ready'.");

        shell.StartServer(ignoreLastSession: true, onReady: () => WatchForReady(shell));
    }

    /// <summary>Polls the game log for the readiness marker, then clears the staged PremadeCheckpointPath.</summary>
    private static void WatchForReady(AppShell shell)
    {
        var catalog = new LogCatalog(shell.Binding);
        var started = DateTime.UtcNow;
        bool cleaned = false;

        Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(3), _ =>
        {
            if (shell.Monitor.Latest.State != ServerState.Running)
                return false; // process gone — stop watching

            catalog.Scan();
            LogFileInfo game = catalog.ActiveGameLog;
            if (game != null && ReadinessDetector.IsReady(game.Path))
            {
                if (!cleaned)
                {
                    cleaned = true;
                    ClearStaging(shell);
                    shell.ReloadInstance();
                    Dialogs.Info("Game ready",
                        "The world was created and the server reports 'Game ready'.\n" +
                        "You can now stop the server (Server → Stop) to complete the flow.");
                }
                return false;
            }

            // Give up watching after a generous window (creation still proceeds).
            return DateTime.UtcNow - started < TimeSpan.FromMinutes(10);
        });
    }

    private static void ClearStaging(AppShell shell)
    {
        try
        {
            DedicatedConfigDocument cfg = shell.Instance.Config;
            if (!string.IsNullOrEmpty(cfg.PremadeCheckpointPath))
            {
                cfg.PremadeCheckpointPath = string.Empty;
                cfg.Save(shell.Writer);
            }
        }
        catch
        {
            // Non-fatal: a leftover PremadeCheckpointPath only matters when no
            // world is selected, and the DS wrote LastSession.sbl on first save.
        }
    }

    private static void SeedSessionSettings(DedicatedConfigDocument cfg, WorldTemplate template)
    {
        WorldConfigDocument seed = WorldTemplateCatalog.OpenSeed(template);
        foreach (OptionDefinition def in OptionRegistry.SessionOptions)
        {
            if (seed.IsSet(def))
                cfg.Set(def, seed.Get(def));
        }
    }

    private static void EnsureNonZeroMaxPlayers(DedicatedConfigDocument cfg)
    {
        OptionDefinition maxPlayers = OptionRegistry.ById("Session.MaxPlayers");
        if (maxPlayers == null)
            return;
        if (!ConfigDocumentBase.TryParseLong(cfg.Get(maxPlayers), out long v) || v <= 0)
            cfg.Set(maxPlayers, "4"); // new-world branch aborts if MaxPlayers == 0
    }

    private static WorldTemplate PickTemplate(DsInstance instance)
    {
        var dlg = new Dialog("Pick a template", 60, 18) { ColorScheme = TurboVisionTheme.Dialog };
        var list = new ListView(instance.Templates.Templates.Select(t => $"{t.DisplayName}   ({t.FolderName})").ToList())
        {
            X = 1, Y = 1, Width = Dim.Fill(2), Height = Dim.Fill(3), ColorScheme = TurboVisionTheme.Dialog,
        };
        WorldTemplate result = null;
        var ok = new Button("Select", true);
        ok.Clicked += () =>
        {
            int i = list.SelectedItem;
            if (i >= 0 && i < instance.Templates.Templates.Count)
                result = instance.Templates.Templates[i];
            Application.RequestStop(dlg);
        };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop(dlg);
        dlg.Add(new Label("Templates from the DS install (Content/CustomWorlds):") { X = 1, Y = 0 }, list);
        dlg.AddButton(ok);
        dlg.AddButton(cancel);
        list.SetFocus();
        Application.Run(dlg);
        return result;
    }

    private static string PromptName(WorldTemplate template, DsInstance instance)
    {
        var dlg = new Dialog("World name", 56, 10) { ColorScheme = TurboVisionTheme.Dialog };
        var field = new TextField(template.DisplayName) { X = 1, Y = 2, Width = Dim.Fill(2) };
        var warn = new Label("") { X = 1, Y = 4, Width = Dim.Fill(2), ColorScheme = TurboVisionTheme.Dialog };
        string result = null;

        void Validate()
        {
            string n = field.Text.ToString();
            if (n.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
                warn.Text = "Name must not contain / \\ or :";
            else if (instance.Worlds.Worlds.Any(w =>
                         string.Equals(w.SessionName, n, StringComparison.OrdinalIgnoreCase)
                         || Io.PlatformPaths.PathComparer.Equals(w.FolderName, n)))
                warn.Text = "A world with this name already exists.";
            else
                warn.Text = string.Empty;
        }
        field.TextChanged += _ => Validate();

        var ok = new Button("OK", true);
        ok.Clicked += () =>
        {
            string n = field.Text.ToString().Trim();
            if (string.IsNullOrEmpty(n) || n.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
            {
                Dialogs.Error("World name", "Enter a valid name (no / \\ or :).");
                return;
            }
            result = n;
            Application.RequestStop(dlg);
        };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop(dlg);
        dlg.Add(new Label("World name:") { X = 1, Y = 1 }, field, warn);
        dlg.AddButton(ok);
        dlg.AddButton(cancel);
        Validate();
        field.SetFocus();
        Application.Run(dlg);
        return result;
    }
}
