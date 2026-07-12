using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.Process;
using Magnetar.ConfigTerminal.State;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// The application shell: a Turbo Vision desktop hosting one primary window at a
/// time, with a menu bar, an F-key status bar carrying the live server state,
/// and a 2-second process-status poll.
/// </summary>
internal sealed class AppShell : Toplevel
{
    private readonly InstanceBinding binding;
    private readonly AtomicFile writer = new();
    private readonly MagnetarProcess process;
    private readonly ProcessMonitor monitor;
    private readonly ToolSettings settings;

    private DsInstance instance;
    private View content;
    private Label statusLabel;
    private DashboardView dashboard;
    private bool starting; // UI-only: bridges the gap before the pid file appears

    public AppShell(InstanceBinding binding)
    {
        this.binding = binding;
        instance = DsInstance.Open(binding);
        process = new MagnetarProcess(binding);
        monitor = new ProcessMonitor(process);
        settings = ToolSettings.Load(binding.MagnetarConfigDir);

        ColorScheme = TurboVisionTheme.Desktop;
        Add(new DesktopBackground());
        Add(BuildMenu());
        StatusBar bar = BuildStatusBar();
        Add(statusLabel);
        Add(bar);

        ShowWorlds();

        monitor.Changed += _ => RefreshStatus();
        Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(2), _ =>
        {
            monitor.Poll();
            RefreshStatus();
            return true;
        });
        monitor.Poll();
        RefreshStatus();
    }

    // Exposed so views can drive process/config operations through the shell.
    public InstanceBinding Binding => binding;
    public DsInstance Instance => instance;
    public MagnetarProcess Process => process;
    public ProcessMonitor Monitor => monitor;
    public AtomicFile Writer => writer;

    private MenuBar BuildMenu()
    {
        return new MenuBar(new[]
        {
            new MenuBarItem("_File", new[]
            {
                new MenuItem("_Open Instance…", "", ReopenInstance),
                new MenuItem("_Quit", "", () => RequestQuit()),
            }),
            new MenuBarItem("_Server", new[]
            {
                new MenuItem("_Settings", "", ShowServerSettings),
                new MenuItem("_Access Lists", "", ShowAccessLists),
                new MenuItem("Server _Password…", "", ShowPasswordDialog),
                new MenuItem("_New-World Defaults", "", ShowNewWorldDefaults),
                new MenuItem(null, null, null),
                new MenuItem("S_tart", "", () => StartServer()),
                new MenuItem("Sto_p", "", StopServer),
                new MenuItem("_Restart", "", RestartServer),
                new MenuItem("Reload _Config", "", ReloadServer),
            }),
            new MenuBarItem("_Worlds", new[]
            {
                new MenuItem("_Browse", "", ShowWorlds),
                new MenuItem("_New World…", "", ShowNewWorldWizard),
            }),
            new MenuBarItem("_Plugins", new[]
            {
                new MenuItem("_Hub Plugins", "", ShowHubPlugins),
                new MenuItem("_Profiles", "", ShowProfiles),
                new MenuItem("_Local & Dev Plugins", "", ShowPlugins),
                new MenuItem("Plugin _Sources", "", ShowPluginSources),
                new MenuItem("_Mods", "", ShowModSources),
            }),
            new MenuBarItem("_Tools", new[]
            {
                new MenuItem("_Logs", "", ShowLogs),
                new MenuItem("_Dashboard", "", ShowDashboard),
            }),
            new MenuBarItem("_Help", new[]
            {
                new MenuItem("_About", "", ShowAbout),
            }),
        });
    }

    private StatusBar BuildStatusBar()
    {
        statusLabel = new Label("")
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
            ColorScheme = TurboVisionTheme.Menu,
        };

        var bar = new StatusBar(new[]
        {
            new StatusItem(Key.F1, "~F1~ Help", ShowAbout),
            new StatusItem(Key.F3, "~F3~ Worlds", ShowWorlds),
            new StatusItem(Key.F4, "~F4~ Logs", ShowLogs),
            new StatusItem(Key.F5, "~F5~ Start/Stop", ToggleServer),
            new StatusItem(Key.F7, "~F7~ Settings", ShowServerSettings),
            new StatusItem(Key.F8, "~F8~ Plugins", ShowPlugins),
            new StatusItem(Key.F10, "~F10~ Quit", () => RequestQuit()),
        });
        // The status bar owns the bottom row; the live state line sits just above it.
        statusLabel.Y = Pos.AnchorEnd(2);
        return bar;
    }

    private void RefreshStatus()
    {
        ServerStatus s = monitor.Latest;
        // The pid-file query jumps NotRunning → Running, so surface a UI-driven
        // STARTING while our launch is in flight and the pid file hasn't appeared.
        bool showStarting = starting && s.State != ServerState.Running;
        ServerState state = showStarting ? ServerState.Starting : s.State;

        string glyph = state switch
        {
            ServerState.Running => "●",
            ServerState.Starting => "◌",
            ServerState.Stopping => "◌",
            ServerState.StalePidFile => "!",
            ServerState.Foreign => "!",
            _ => "○",
        };
        string label = showStarting ? "STARTING…" : s.ToString();
        string world = instance.ActiveWorld?.SessionName ?? instance.Config?.WorldName ?? "(no world selected)";
        string port = s.State == ServerState.Running && instance.Config != null
            ? $"  ·  UDP {instance.Config.ServerPort}"
            : string.Empty;
        statusLabel.Text = $" {glyph} {label} — {world}{port}";
        dashboard?.UpdateStatus(showStarting ? new ServerStatus { State = ServerState.Starting } : s);
    }

    // --- content hosting ---

    private void SetContent(View view)
    {
        if (content != null)
        {
            Remove(content);
            content.Dispose();
        }
        dashboard = view as DashboardView;
        content = view;
        view.X = 1;
        view.Y = 1;
        view.Width = Dim.Fill(1);
        view.Height = Dim.Fill(2);
        Add(view);
        view.SetFocus();
    }

    public void ShowDashboard()
    {
        var d = new DashboardView(this);
        SetContent(d);
        d.UpdateStatus(monitor.Latest);
    }

    public void ShowServerSettings()
    {
        var view = new OptionFormView(
            "Server Settings",
            OptionRegistry.DedicatedOptions,
            instance.Config,
            new EditSession(instance.Config, OptionRegistry.DedicatedOptions),
            writer,
            OnConfigSaved);
        SetContent(view);
    }

    public void ShowNewWorldDefaults()
    {
        var view = new OptionFormView(
            "New-World Defaults (template for newly created worlds)",
            OptionRegistry.SessionOptions,
            instance.Config,
            new EditSession(instance.Config, OptionRegistry.SessionOptions),
            writer,
            OnConfigSaved,
            banner: "These are defaults for NEW worlds. Existing worlds keep their own settings.");
        SetContent(view);
    }

    public void ShowAccessLists() => SetContent(new AccessListView(instance.Config, writer, OnConfigSaved));

    public void ShowPasswordDialog() => PasswordDialog.Show(instance.Config, writer, OnConfigSaved);

    public void ShowWorlds() => SetContent(new WorldsView(this));

    /// <summary>Hosts a world-scoped sub-view (settings/mods) in the content area.</summary>
    public void ShowWorldContent(View view) => SetContent(view);

    public void ShowLogs() => SetContent(new LogViewerView(binding));

    public void ShowPlugins() => SetContent(new PluginsView(binding.MagnetarConfigDir, writer, settings));

    public void ShowHubPlugins() => SetContent(new HubPluginsView(binding.MagnetarConfigDir, writer));

    public void ShowPluginSources() => SetContent(new PluginSourcesView(binding.MagnetarConfigDir, writer));

    public void ShowModSources() => SetContent(new ModSourcesView(binding.MagnetarConfigDir, writer, settings));

    public void ShowProfiles() => SetContent(new ProfilesView(binding.MagnetarConfigDir, writer, OnConfigSaved));

    public void ShowNewWorldWizard() => NewWorldWizard.Run(this);

    private void OnConfigSaved()
    {
        instance.Reload();
        RefreshStatus();
    }

    public void ReloadInstance()
    {
        instance.Reload();
        RefreshStatus();
    }

    // --- process actions ---

    private void ToggleServer()
    {
        if (monitor.Latest.State == ServerState.Running)
            StopServer();
        else
            StartServer(confirm: true);
    }

    public void StartServer(bool ignoreLastSession = false, Action onReady = null, bool confirm = false)
    {
        if (confirm && !ConfirmStart())
            return;

        var spec = new LaunchSpec { Binding = binding, IgnoreLastSession = ignoreLastSession };
        starting = true;
        RefreshStatus();
        Dialogs.RunBackground(
            () => process.Start(spec),
            result =>
            {
                starting = false;
                monitor.Poll();
                RefreshStatus();
                if (result.Ok)
                {
                    onReady?.Invoke();
                    if (onReady == null)
                        Dialogs.Info("Server", result.Message);
                }
                else
                {
                    Dialogs.Error("Start failed", result.Message);
                }
            });
    }

    /// <summary>
    /// Pre-flight confirmation for an explicit start: shows the world that will
    /// load, the game (UDP) port, and the plugins and mods that come with it.
    /// </summary>
    private bool ConfirmStart()
    {
        string world = instance.ActiveWorld?.SessionName ?? instance.Config?.WorldName;
        string question = string.IsNullOrEmpty(world)
            ? "Start the server?"
            : $"Start the server with world '{world}'?";

        int port = instance.Config?.ServerPort ?? 27016;
        var details = new StringBuilder();
        details.AppendLine($"Port    : UDP {port}");
        AppendList(details, "Plugins", CollectPlugins());
        AppendList(details, "Mods", CollectMods());

        return Dialogs.ConfirmDetails("Start server", question, details.ToString().TrimEnd(), "Start", "Cancel");
    }

    private List<string> CollectPlugins()
    {
        var names = new List<string>();
        try
        {
            PluginProfileDocument profile = PluginProfileDocument.Open(binding.MagnetarConfigDir);
            names.AddRange(profile.DevFolders.Select(d => $"{d.Id} (dev folder)"));
            names.AddRange(profile.LocalDlls);

            // Resolve enabled hub plugins to friendly names via the cached catalog;
            // any enabled id the catalog doesn't know falls back to its raw id.
            var plugins = new MagnetarPlugins(binding.MagnetarConfigDir, writer);
            var byId = plugins.HubCatalogPlugins()
                .Where(v => v.Enabled)
                .ToDictionary(v => v.Id, v => v.Info.FriendlyName, StringComparer.OrdinalIgnoreCase);
            foreach (string id in profile.GitHubPlugins)
                names.Add($"{(byId.TryGetValue(id, out string friendly) ? friendly : id)} (hub)");
        }
        catch { /* best-effort summary */ }
        return names;
    }

    private List<string> CollectMods()
    {
        WorldInfo world = instance.ActiveWorld;
        if (world == null || !world.HasWorldConfig)
            return new List<string>();
        try
        {
            ModList mods = WorldConfigDocument.Open(world.WorldConfigPath).ReadMods();
            return mods.Items
                .Select(m => string.IsNullOrEmpty(m.FriendlyName) ? m.PublishedFileId.ToString() : m.FriendlyName)
                .ToList();
        }
        catch { return new List<string>(); }
    }

    private static void AppendList(StringBuilder sb, string label, IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            sb.AppendLine($"{label,-8}: (none)");
            return;
        }
        sb.AppendLine($"{label,-8}: {items.Count}");
        foreach (string name in items.Take(12))
            sb.AppendLine($"  • {name}");
        if (items.Count > 12)
            sb.AppendLine($"  … (+{items.Count - 12} more)");
    }

    public void StopServer()
    {
        if (monitor.Latest.State != ServerState.Running)
        {
            Dialogs.Info("Server", "The server is not running.");
            return;
        }
        if (!Dialogs.Confirm("Stop server", "Send SIGTERM so the server saves the world and quits?"))
            return;

        Dialogs.RunBackground(
            () => process.Stop(TimeSpan.FromMinutes(2)),
            result =>
            {
                monitor.Poll();
                RefreshStatus();
                if (result.Ok)
                    Dialogs.Info("Server", result.Message);
                else
                    OfferForceKill(result.Message);
            });
    }

    private void OfferForceKill(string message)
    {
        if (Dialogs.Confirm("Stop timed out", message + "\n\nForce-kill the process? Progress since the last save will be LOST.", "Force kill", "Wait"))
        {
            Dialogs.RunBackground(
                () => process.ForceKill(TimeSpan.FromSeconds(15)),
                r => { monitor.Poll(); RefreshStatus(); Dialogs.Info("Server", r.Message); });
        }
    }

    public void RestartServer()
    {
        if (!Dialogs.Confirm("Restart", "Stop (save + quit) and start the server again?"))
            return;
        Dialogs.RunBackground(
            () => process.Stop(TimeSpan.FromMinutes(2)),
            _ =>
            {
                monitor.Poll();
                RefreshStatus();
                StartServer();
            });
    }

    public void ReloadServer()
    {
        OpResult r = process.Reload();
        if (r.Ok) Dialogs.Info("Reload", r.Message);
        else Dialogs.Error("Reload", r.Message);
    }

    private void ReopenInstance()
    {
        InstanceBinding chosen = InstancePickerDialog.Show(binding);
        if (chosen == null)
            return;
        binding.DataDir = chosen.DataDir;
        binding.MagnetarConfigDir = chosen.MagnetarConfigDir;
        binding.MagnetarExePath = chosen.MagnetarExePath;
        binding.Ds64Dir = chosen.Ds64Dir;
        instance = DsInstance.Open(binding);
        ShowDashboard();
        RefreshStatus();
    }

    private void ShowAbout() => HelpDialog.Show();

    private bool RequestQuit()
    {
        if (Dialogs.Confirm("Quit", "Exit MagnetarConfig?"))
        {
            Application.RequestStop();
            return true;
        }
        return false;
    }
}
