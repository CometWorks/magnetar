using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Lists the worlds under Saves/ and offers per-world settings/mods editing and
/// activation (writing LastSession.sbl + clearing IgnoreLastSession).
/// </summary>
internal sealed class WorldsView : Window
{
    private readonly AppShell shell;
    private readonly ListView list;
    private List<WorldInfo> worlds;

    public WorldsView(AppShell shell) : base("Worlds")
    {
        this.shell = shell;
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        list = new ListView(Array.Empty<string>())
        {
            X = 1, Y = 1, Width = Dim.Fill(1), Height = Dim.Fill(3),
            ColorScheme = TurboVisionTheme.Window,
        };
        list.OpenSelectedItem += _ => OpenSettings();

        var header = new Label(Row("Name", "Last saved", "Mods", "Config", "Active"))
        { X = 1, Y = 0, ColorScheme = TurboVisionTheme.Window };

        var settings = new Button("_Settings") { X = 1, Y = Pos.AnchorEnd(2) };
        settings.Clicked += OpenSettings;
        var mods = new Button("_Mods") { X = Pos.Right(settings) + 1, Y = Pos.AnchorEnd(2) };
        mods.Clicked += OpenMods;
        var activate = new Button("_Activate (F6)") { X = Pos.Right(mods) + 1, Y = Pos.AnchorEnd(2) };
        activate.Clicked += ActivateWorld;
        var refresh = new Button("_Refresh") { X = Pos.Right(activate) + 1, Y = Pos.AnchorEnd(2) };
        refresh.Clicked += Reload;
        var newWorld = new Button("_New World") { X = Pos.Right(refresh) + 1, Y = Pos.AnchorEnd(2) };
        newWorld.Clicked += shell.ShowNewWorldWizard;

        Add(header, list, settings, mods, activate, refresh, newWorld);
        Reload();
    }

    public override bool ProcessKey(KeyEvent kb)
    {
        if (kb.Key == Key.F6)
        {
            ActivateWorld();
            return true;
        }
        return base.ProcessKey(kb);
    }

    private void Reload()
    {
        shell.ReloadInstance();
        worlds = shell.Instance.Worlds.Worlds.ToList();
        list.SetSource(worlds.Select(Format).ToList());
    }

    // Single column layout shared by the header and the data rows so they stay aligned.
    private static string Row(string name, string saved, string mods, string cfg, string active) =>
        $"{name,-28}{saved,-22}{mods,-7}{cfg,-9}{active}";

    private static string Format(WorldInfo w)
    {
        string saved = w.LastSaveTime?.ToString("yyyy-MM-dd HH:mm") ?? "—";
        string cfg = w.HasWorldConfig ? "ok" : "missing";
        string active = w.IsActive ? "◀ ACTIVE" : "";
        string name = w.SessionName.Length > 26 ? w.SessionName.Substring(0, 26) : w.SessionName;
        return Row(name, saved, w.ModCount.ToString(), cfg, active);
    }

    private WorldInfo Selected =>
        worlds != null && list.SelectedItem >= 0 && list.SelectedItem < worlds.Count
            ? worlds[list.SelectedItem]
            : null;

    private void OpenSettings()
    {
        WorldInfo w = Selected;
        if (w == null) return;

        WorldConfigDocument doc = WorldConfigDocument.Open(w.WorldConfigPath);
        var view = new OptionFormView(
            $"World Settings — {w.SessionName}",
            OptionRegistry.SessionOptions,
            doc,
            new EditSession(doc, OptionRegistry.SessionOptions),
            shell.Writer,
            Reload);
        // Reuse the shell's content host.
        shell.ShowWorldContent(view);
    }

    private void OpenMods()
    {
        WorldInfo w = Selected;
        if (w == null) return;
        shell.ShowWorldContent(new ModListView(w, shell.Writer));
    }

    private void ActivateWorld()
    {
        WorldInfo w = Selected;
        if (w == null)
        {
            Dialogs.Info("Activate", "Select a world first.");
            return;
        }
        if (!w.HasCheckpoint)
        {
            Dialogs.Error("Activate", "This world has no Sandbox.sbc and cannot be activated.");
            return;
        }
        if (w.FolderName.IndexOfAny(new[] { '/', '\\' }) >= 0)
        {
            Dialogs.Error("Activate", "World folder name contains a path separator.");
            return;
        }

        DedicatedConfigDocument cfg = shell.Instance.Config;
        bool willClearIgnore = cfg.IgnoreLastSession;
        bool loadWorldSet = !string.IsNullOrEmpty(cfg.LoadWorld);

        string question = $"Activate '{w.SessionName}'?";
        var details = "Writes:\n  • Saves/LastSession.sbl";
        if (willClearIgnore) details += "\n  • cfg IgnoreLastSession → false";
        if (loadWorldSet) details += "\n  • cfg LoadWorld → cleared";
        details += "\n\nTakes effect on the next server start.";

        if (!Dialogs.ConfirmDetails("Activate world", question, details, "Activate", "Cancel"))
            return;

        try
        {
            LastSessionFile sbl = LastSessionFile.ForWorld(w, shell.Instance.Binding.SavesPath);
            sbl.Write(shell.Writer, LastSessionFile.PathFor(shell.Instance.Binding.SavesPath));

            if (willClearIgnore || loadWorldSet)
            {
                if (willClearIgnore) cfg.IgnoreLastSession = false;
                if (loadWorldSet) cfg.LoadWorld = string.Empty;
                cfg.Save(shell.Writer);
            }

            Reload();
            Dialogs.Info("Activated", $"'{w.SessionName}' will load on the next start.\nNote: a -session: on the command line overrides this.");
        }
        catch (Exception e)
        {
            Dialogs.Error("Activate failed", e.Message);
        }
    }
}
