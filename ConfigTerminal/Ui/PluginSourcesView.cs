using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Manages the instance's plugin catalog <em>sources</em> — the remote GitHub hubs
/// (e.g. MagnetarHub), single remote plugin repos, and local hub folders that
/// Magnetar scans for available plugins. Edits <c>Sources/sources.xml</c> in place
/// via the same upsert approach as the rest of the tool, preserving every field
/// Magnetar manages itself (LastCheck, Hash). Press SPACE to toggle a source.
/// </summary>
internal sealed class PluginSourcesView : Window
{
    private readonly MagnetarPlugins plugins;
    private readonly ListView list;

    // Flattened rows across the three source kinds, for a single list view.
    private enum Kind { RemoteHub, RemotePlugin, LocalHub }
    private sealed class Row { public Kind Kind; public string Key; public string Text; public bool Enabled; }
    private List<Row> rows = new();

    public PluginSourcesView(string magnetarConfigDir, AtomicFile writer) : base("Plugin Sources")
    {
        plugins = new MagnetarPlugins(magnetarConfigDir, writer);
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        list = new ListView(Array.Empty<string>())
        { X = 1, Y = 0, Width = Dim.Fill(1), Height = Dim.Fill(2), ColorScheme = TurboVisionTheme.Window };
        list.OpenSelectedItem += _ => ToggleEnabled();

        var addHub = new Button("Add _Hub…") { X = 1, Y = Pos.AnchorEnd(1) };
        addHub.Clicked += AddRemoteHub;
        var addPlugin = new Button("Add _Plugin…") { X = Pos.Right(addHub) + 1, Y = Pos.AnchorEnd(1) };
        addPlugin.Clicked += AddRemotePlugin;
        var addLocal = new Button("Add _Local…") { X = Pos.Right(addPlugin) + 1, Y = Pos.AnchorEnd(1) };
        addLocal.Clicked += AddLocalHub;
        var toggle = new Button("_Toggle") { X = Pos.Right(addLocal) + 1, Y = Pos.AnchorEnd(1) };
        toggle.Clicked += ToggleEnabled;
        var remove = new Button("_Remove") { X = Pos.Right(toggle) + 1, Y = Pos.AnchorEnd(1) };
        remove.Clicked += Remove;

        Add(list, addHub, addPlugin, addLocal, toggle, remove);
        Refresh();
    }

    public override bool ProcessKey(KeyEvent kb)
    {
        if (kb.Key == (Key)' ' && list.HasFocus)
        {
            ToggleEnabled();
            return true;
        }
        return base.ProcessKey(kb);
    }

    private void Refresh()
    {
        plugins.Reload();
        rows = new List<Row>();
        foreach (RemoteHubSource h in plugins.RemoteHubs())
            rows.Add(new Row { Kind = Kind.RemoteHub, Key = h.Repo, Enabled = h.Enabled,
                Text = $"HUB     {h.Name,-16} {h.Repo} ({h.Branch})" });
        foreach (RemotePluginSource p in plugins.RemotePlugins())
            rows.Add(new Row { Kind = Kind.RemotePlugin, Key = p.Repo, Enabled = p.Enabled,
                Text = $"PLUGIN  {p.Name,-16} {p.Repo} [{p.File}]" });
        foreach (LocalHubSource l in plugins.LocalHubs())
            rows.Add(new Row { Kind = Kind.LocalHub, Key = l.Folder, Enabled = l.Enabled,
                Text = $"LOCAL   {l.Name,-16} {l.Folder}" });

        int keep = list.SelectedItem;
        list.SetSource(rows.Select(r => $"{(r.Enabled ? "[x]" : "[ ]")} {r.Text}").ToList());
        if (rows.Count > 0)
            list.SelectedItem = Math.Min(Math.Max(0, keep), rows.Count - 1);
    }

    private Row Selected()
    {
        int i = list.SelectedItem;
        return i >= 0 && i < rows.Count ? rows[i] : null;
    }

    private void AddRemoteHub()
    {
        string repo = Dialogs.Prompt("Add hub source", "GitHub repo (owner/name), e.g. CometWorks/magnetar-hub:");
        if (string.IsNullOrWhiteSpace(repo)) return;
        string name = Dialogs.Prompt("Add hub source", "Display name:", repo.Split('/').Last());
        if (name == null) return;
        string branch = Dialogs.Prompt("Add hub source", "Branch:", "main");
        if (branch == null) return;
        if (!plugins.AddRemoteHub(name, repo.Trim(), branch.Trim()))
            Dialogs.Info("Add hub", "A hub source with that repo already exists.");
        Refresh();
    }

    private void AddRemotePlugin()
    {
        string repo = Dialogs.Prompt("Add plugin source", "GitHub repo (owner/name):");
        if (string.IsNullOrWhiteSpace(repo)) return;
        string name = Dialogs.Prompt("Add plugin source", "Display name:", repo.Split('/').Last());
        if (name == null) return;
        string branch = Dialogs.Prompt("Add plugin source", "Branch:", "main");
        if (branch == null) return;
        string file = Dialogs.Prompt("Add plugin source", "Manifest file path in repo (e.g. Plugin.xml):");
        if (file == null) return;
        if (!plugins.AddRemotePlugin(name, repo.Trim(), branch.Trim(), file.Trim()))
            Dialogs.Info("Add plugin", "A plugin source with that repo already exists.");
        Refresh();
    }

    private void AddLocalHub()
    {
        string folder = Dialogs.Prompt("Add local hub", "Folder containing plugin manifest .xml files:");
        if (string.IsNullOrWhiteSpace(folder)) return;
        string name = Dialogs.Prompt("Add local hub", "Display name:",
            System.IO.Path.GetFileName(folder.TrimEnd('/', '\\')));
        if (name == null) return;
        if (!plugins.AddLocalHub(name, folder.Trim()))
            Dialogs.Info("Add local hub", "A local hub with that folder already exists.");
        Refresh();
    }

    private void ToggleEnabled()
    {
        Row r = Selected();
        if (r == null) return;
        switch (r.Kind)
        {
            case Kind.RemoteHub: plugins.SetRemoteHubEnabled(r.Key, !r.Enabled); break;
            case Kind.RemotePlugin: plugins.SetRemotePluginEnabled(r.Key, !r.Enabled); break;
            case Kind.LocalHub: plugins.SetLocalHubEnabled(r.Key, !r.Enabled); break;
        }
        int i = list.SelectedItem;
        Refresh();
        list.SelectedItem = i;
    }

    private void Remove()
    {
        Row r = Selected();
        if (r == null) return;
        if (!Dialogs.Confirm("Remove source", $"Remove this source?\n\n{r.Text}\n\n" +
                "Plugins it provided stay listed in the profile but will no longer\nresolve until re-added."))
            return;
        switch (r.Kind)
        {
            case Kind.RemoteHub: plugins.RemoveRemoteHub(r.Key); break;
            case Kind.RemotePlugin: plugins.RemoveRemotePlugin(r.Key); break;
            case Kind.LocalHub: plugins.RemoveLocalHub(r.Key); break;
        }
        Refresh();
    }
}
