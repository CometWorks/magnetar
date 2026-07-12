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
/// Enable/disable the Magnetar instance's plugins: local DLLs from the Local/
/// folder (Space toggles), and dev-folder plugins added Quasar-style by picking
/// a manifest XML (the folder + filename + folder-name id are derived, and the
/// last-visited folder is remembered for the next add).
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

        var localFrame = new FrameView("Local DLLs — Space toggles (from Local/ folder)")
        {
            X = 0, Y = 0, Width = Dim.Percent(50), Height = Dim.Fill(2), ColorScheme = TurboVisionTheme.Window,
        };
        localList = new ListView(Array.Empty<string>())
        { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = TurboVisionTheme.Window };
        localList.OpenSelectedItem += _ => ToggleLocal();
        localFrame.Add(localList);

        var devFrame = new FrameView("Dev-folder plugins (Enter/Add picks a manifest XML)")
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

    // Pad the id and manifest columns to a common width so the rows line up.
    private static List<string> FormatDevList(List<DevFolderPlugin> devs)
    {
        if (devs.Count == 0)
            return new List<string>();

        int idWidth = devs.Max(p => (p.Id ?? string.Empty).Length);
        int fileWidth = devs.Max(p => $"[{p.DataFile}]".Length);

        return devs.Select(p =>
        {
            string flag = p.SourceMissing ? "  ! source folder missing" : "";
            string folder = p.Folder ?? "(no source entry)";
            string id = (p.Id ?? string.Empty).PadRight(idWidth);
            string file = $"[{p.DataFile}]".PadRight(fileWidth);
            return $"{id}   {file}   {folder}{flag}";
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
            Dialogs.Info("Dev folder added",
                $"Enabled dev-folder plugin '{id}'.\n\n" +
                "Registered the folder as a plugin source and enabled it in the\n" +
                "current profile. It compiles and loads on the next server start.");
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
                $"Disable and unregister dev-folder plugin '{p.Id}'?\n\n" +
                "This removes it from the profile and the plugin sources.\n" +
                "Your source files on disk are not touched."))
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
