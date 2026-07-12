using System.Text;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.Process;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// The home window: server status, a config summary, the active world, any
/// warnings, and the Start/Stop/Restart/Reload/New-World controls.
/// </summary>
internal sealed class DashboardView : Window
{
    private readonly AppShell shell;
    private readonly Label statusLine;
    private readonly TextView summary;

    public DashboardView(AppShell shell) : base("Dashboard")
    {
        this.shell = shell;
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        statusLine = new Label(" ") { X = 1, Y = 0, Width = Dim.Fill(1), Height = 1, ColorScheme = TurboVisionTheme.Window };

        summary = new TextView
        {
            X = 1,
            Y = 2,
            Width = Dim.Fill(1),
            Height = Dim.Fill(4),
            ReadOnly = true,
            ColorScheme = TurboVisionTheme.Window,
        };

        var start = new Button("_Start") { X = 1, Y = Pos.AnchorEnd(2) };
        start.Clicked += () => shell.StartServer(confirm: true);
        var stop = new Button("Sto_p") { X = Pos.Right(start) + 1, Y = Pos.AnchorEnd(2) };
        stop.Clicked += shell.StopServer;
        var restart = new Button("_Restart") { X = Pos.Right(stop) + 1, Y = Pos.AnchorEnd(2) };
        restart.Clicked += shell.RestartServer;
        var reload = new Button("Re_load") { X = Pos.Right(restart) + 1, Y = Pos.AnchorEnd(2) };
        reload.Clicked += shell.ReloadServer;
        var worlds = new Button("_Worlds") { X = Pos.Right(reload) + 1, Y = Pos.AnchorEnd(2) };
        worlds.Clicked += shell.ShowWorlds;
        var newWorld = new Button("_New World") { X = Pos.Right(worlds) + 1, Y = Pos.AnchorEnd(2) };
        newWorld.Clicked += shell.ShowNewWorldWizard;

        Add(statusLine, summary, start, stop, restart, reload, worlds, newWorld);
        BuildSummary();
    }

    public void UpdateStatus(ServerStatus status)
    {
        if (statusLine == null)
            return;
        statusLine.Text = $"Server: {status}";
        if (!string.IsNullOrEmpty(status.Detail))
            statusLine.Text += $"  ({status.Detail})";
    }

    private void BuildSummary()
    {
        DsInstance instance = shell.Instance;
        DedicatedConfigDocument cfg = instance.Config;
        var sb = new StringBuilder();

        sb.AppendLine($"Instance data dir : {shell.Binding.DataDir}");
        sb.AppendLine($"Magnetar config   : {shell.Binding.MagnetarConfigDir}");
        sb.AppendLine($"Launcher          : {shell.Binding.MagnetarExePath}");
        sb.AppendLine($"DS install        : {shell.Binding.Ds64Dir ?? "(not found — templates disabled)"}");
        sb.AppendLine();

        if (cfg != null)
        {
            sb.AppendLine($"Server name : {Val(cfg, "Dedicated.ServerName")}");
            sb.AppendLine($"Ports       : game {Val(cfg, "Dedicated.ServerPort")}  steam {Val(cfg, "Dedicated.SteamPort")}  api {Val(cfg, "Dedicated.RemoteApiPort")}");
            sb.AppendLine($"Network     : {Val(cfg, "Dedicated.NetworkType")}");
            sb.AppendLine($"Password    : {(cfg.HasPassword ? "set" : "not set")}");
        }

        WorldInfo active = instance.ActiveWorld;
        sb.AppendLine();
        sb.AppendLine($"Active world : {(active != null ? active.SessionName : "(none selected)")}");
        sb.AppendLine($"Worlds on disk : {instance.Worlds.Worlds.Count}");
        sb.AppendLine($"Templates      : {instance.Templates.Templates.Count}");

        if (cfg != null && cfg.IgnoreLastSession)
            sb.AppendLine("\n! IgnoreLastSession is TRUE — LastSession.sbl will be ignored.");
        if (cfg != null && !string.IsNullOrEmpty(cfg.PremadeCheckpointPath))
            sb.AppendLine("! A world-creation is staged (PremadeCheckpointPath is set).");
        if (cfg != null && !string.IsNullOrEmpty(cfg.LoadWorld))
            sb.AppendLine($"! LoadWorld is set to '{cfg.LoadWorld}'.");

        if (instance.Problems.Any)
        {
            sb.AppendLine("\nProblems:");
            foreach (string p in instance.Problems.Messages)
                sb.AppendLine("  • " + p);
        }

        summary.Text = sb.ToString();
    }

    private static string Val(DedicatedConfigDocument cfg, string id)
    {
        OptionDefinition def = OptionRegistry.ById(id);
        return def == null ? "" : cfg.Get(def);
    }
}
