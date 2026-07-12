using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.State;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Manages the Magnetar instance's local plugin sources: local DLLs from the Local/
/// folder (pressing SPACE toggles enabled state), and registered dev folders added
/// Quasar-style by picking a manifest XML (the folder + filename + folder-name id
/// are derived, and the last-visited folder is remembered for the next add).
/// Registering a dev folder only makes it selectable — it is enabled/disabled in
/// the Hub Plugins list. This pane just shows what's registered and its state.
/// </summary>
internal sealed class PluginsView : Window
{
    private readonly MagnetarPlugins plugins;
    private readonly ToolSettings settings;
    private readonly AtomicFile writer;

    private readonly ListView localList;
    private readonly ListView devList;
    private List<LocalDllInfo> locals = new();
    private List<DevFolderPlugin> devs = new();

    public PluginsView(string magnetarConfigDir, AtomicFile writer, ToolSettings settings) : base("Plugins")
    {
        this.writer = writer;
        this.settings = settings;
        plugins = new MagnetarPlugins(magnetarConfigDir, writer);

        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        var localFrame = new FrameView("Local DLLs (from Local/ folder) — Press SPACE to toggle")
        {
            X = 0, Y = 0, Width = Dim.Percent(50), Height = Dim.Fill(2), ColorScheme = TurboVisionTheme.Window,
        };
        localList = new ListView(Array.Empty<string>())
        { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = TurboVisionTheme.Window };
        localList.OpenSelectedItem += _ => ToggleLocal();
        localFrame.Add(localList);

        var devFrame = new FrameView("Registered dev folders (enable under Hub Plugins)")
        {
            X = Pos.Percent(50), Y = 0, Width = Dim.Fill(), Height = Dim.Fill(2), ColorScheme = TurboVisionTheme.Window,
        };
        devList = new ListView(Array.Empty<string>())
        { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = TurboVisionTheme.Window };
        devFrame.Add(devList);

        var toggle = new Button("_Toggle DLL") { X = 0, Y = Pos.AnchorEnd(1) };
        toggle.Clicked += ToggleLocal;
        var add = new Button("_Add Dev Folder…") { X = Pos.Right(toggle) + 1, Y = Pos.AnchorEnd(1) };
        add.Clicked += AddDevFolder;
        var remove = new Button("_Remove Dev Folder") { X = Pos.Right(add) + 1, Y = Pos.AnchorEnd(1) };
        remove.Clicked += RemoveDevFolder;
        var refresh = new Button("Re_fresh") { X = Pos.Right(remove) + 1, Y = Pos.AnchorEnd(1) };
        refresh.Clicked += Refresh;

        Add(localFrame, devFrame, toggle, add, remove, refresh);
        Refresh();
    }

    public override bool ProcessKey(KeyEvent kb)
    {
        if (kb.Key == (Key)' ' && localList.HasFocus)
        {
            ToggleLocal();
            return true;
        }
        return base.ProcessKey(kb);
    }

    private void Refresh()
    {
        plugins.Reload();
        locals = plugins.LocalDlls().ToList();
        devs = plugins.DevFolderPlugins().ToList();

        localList.SetSource(locals.Select(FormatLocal).ToList());
        devList.SetSource(FormatDevList(devs));
    }

    private static string FormatLocal(LocalDllInfo d)
    {
        string box = d.Enabled ? "[x]" : "[ ]";
        string missing = d.FullPath == null ? "  (file missing)" : "";
        return $"{box} {d.FileName}{missing}";
    }

    // Pad the id column to a common width so the rows line up. The [x]/[ ] box
    // reflects whether the active profile enables it (toggled under Hub Plugins).
    private static List<string> FormatDevList(List<DevFolderPlugin> devs)
    {
        if (devs.Count == 0)
            return new List<string>();

        int idWidth = devs.Max(p => (p.Id ?? string.Empty).Length);

        return devs.Select(p =>
        {
            string box = p.Enabled ? "[x]" : "[ ]";
            string flag = p.SourceMissing ? "  ! folder missing" : "";
            string folder = p.Folder ?? "(no folder)";
            string id = (p.Id ?? string.Empty).PadRight(idWidth);
            return $"{box} {id}   {folder}{flag}";
        }).ToList();
    }

    private void ToggleLocal()
    {
        int i = localList.SelectedItem;
        if (i < 0 || i >= locals.Count)
            return;
        LocalDllInfo d = locals[i];
        plugins.SetLocalDllEnabled(d.FileName, !d.Enabled);
        Refresh();
        if (i < localList.Source.Count)
            localList.SelectedItem = i;
    }

    private void AddDevFolder()
    {
        string picked = ManifestPicker.Pick(settings.LastPluginFolder);
        if (picked == null)
            return;

        // Remember the folder immediately so the next add starts here.
        try { settings.LastPluginFolder = Path.GetDirectoryName(Path.GetFullPath(picked)); } catch { }
        settings.Save(writer);

        try
        {
            string id = plugins.AddDevFolderFromManifest(picked);
            Refresh();
            Dialogs.Info("Dev folder registered",
                $"Registered dev folder '{id}' as a plugin source.\n\n" +
                "It's now selectable in the Hub Plugins list (shown with a\n" +
                "\"- dev folder\" suffix). Enable it there to load it on the\n" +
                "next server start.");
        }
        catch (Exception e)
        {
            Dialogs.Error("Add dev folder", e.Message);
        }
    }

    private void RemoveDevFolder()
    {
        int i = devList.SelectedItem;
        if (i < 0 || i >= devs.Count)
        {
            Dialogs.Info("Remove", "Select a dev-folder plugin first.");
            return;
        }
        DevFolderPlugin p = devs[i];
        if (!Dialogs.Confirm("Remove dev folder",
                $"Unregister dev folder '{p.Id}'?\n\n" +
                "This removes it from the plugin sources (and disables it in the\n" +
                "active profile if enabled). Your source files on disk are not touched."))
            return;

        try
        {
            plugins.RemoveDevFolder(p);
            Refresh();
        }
        catch (Exception e)
        {
            Dialogs.Error("Remove dev folder", e.Message);
        }
    }
}
