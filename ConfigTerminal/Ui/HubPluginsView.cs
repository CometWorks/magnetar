using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Browses the plugins offered by the instance's configured hub/remote sources
/// (read offline from Magnetar's own cached catalogs under <c>Sources/Hubs</c> and
/// <c>Sources/Plugins</c>) plus the registered dev folders (from sources.xml, shown
/// with a "- dev folder" suffix), and enables/disables them in the active profile.
/// Space or Enter toggles; enabling a hub plugin also pulls in its declared
/// dependencies. A details pane shows the focused plugin's author, tagline and
/// description. Dev folders are added/removed under Plugins, not here.
/// </summary>
internal sealed class HubPluginsView : Window
{
    private const string CachedEmptyMessage =
        "No hub plugins cached yet.\n\n" +
        "Magnetar downloads the plugin catalog on server start from the\n" +
        "configured hub sources. Add a source under Plugins ▸ Plugin Sources,\n" +
        "then start the server once so it fetches the catalog.";

    private readonly MagnetarPlugins plugins;
    private readonly TextField filter;
    private readonly ListView list;
    private readonly TextView details;
    private List<HubPluginView> allItems = new();
    private List<HubPluginView> items = new();
    private string defaultHubLabel;

    public HubPluginsView(string magnetarConfigDir, AtomicFile writer) : base("Hub Plugins")
    {
        plugins = new MagnetarPlugins(magnetarConfigDir, writer);
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        var listFrame = new FrameView("Available plugins — Space/Enter toggles enabled")
        {
            X = 0, Y = 0, Width = Dim.Percent(34), Height = Dim.Fill(2), ColorScheme = TurboVisionTheme.Window,
        };
        var filterLabel = new Label("Filter: ") { X = 0, Y = 0 };
        filter = new TextField(string.Empty)
        { X = Pos.Right(filterLabel), Y = 0, Width = Dim.Fill(), ColorScheme = TurboVisionTheme.Window };
        filter.TextChanged += _ => ApplyFilter();
        list = new ListView(Array.Empty<string>())
        { X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = TurboVisionTheme.Window };
        list.SelectedItemChanged += _ => ShowDetails();
        list.OpenSelectedItem += _ => Toggle();
        listFrame.Add(filterLabel, filter, list);

        var detailFrame = new FrameView("Details")
        {
            X = Pos.Percent(34), Y = 0, Width = Dim.Fill(), Height = Dim.Fill(2), ColorScheme = TurboVisionTheme.Window,
        };
        details = new TextView
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ReadOnly = true, WordWrap = true,
            ColorScheme = TurboVisionTheme.Window,
        };
        detailFrame.Add(details);

        var toggle = new Button("_Toggle") { X = 0, Y = Pos.AnchorEnd(1) };
        toggle.Clicked += Toggle;
        var refresh = new Button("Re_fresh") { X = Pos.Right(toggle) + 1, Y = Pos.AnchorEnd(1) };
        refresh.Clicked += Refresh;
        var srcHint = new Label("Manage sources & dev folders under Plugins") { X = Pos.Right(refresh) + 2, Y = Pos.AnchorEnd(1) };

        Add(listFrame, detailFrame, toggle, refresh, srcHint);
        Refresh();
    }

    public override bool ProcessKey(KeyEvent kb)
    {
        if (kb.Key == (Key)' ' && list.HasFocus)
        {
            Toggle();
            return true;
        }
        return base.ProcessKey(kb);
    }

    private void Refresh()
    {
        plugins.Reload();
        defaultHubLabel = plugins.DefaultHubLabel;
        // Hub/remote plugins plus the registered dev folders, interleaved by name.
        allItems = plugins.HubCatalogPlugins()
            .Concat(plugins.DevFolderCatalogViews())
            .OrderBy(v => v.Info.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        ApplyFilter();
    }

    // Narrow the catalog to rows matching the filter box (name/id/author/tagline).
    private void ApplyFilter()
    {
        string q = (filter.Text?.ToString() ?? string.Empty).Trim();
        items = string.IsNullOrEmpty(q)
            ? allItems.ToList()
            : allItems.Where(v => Matches(v, q)).ToList();

        int keep = list.SelectedItem;
        list.SetSource(items.Select(Format).ToList());
        if (items.Count > 0)
        {
            list.SelectedItem = Math.Min(Math.Max(0, keep), items.Count - 1);
            ShowDetails();
        }
        else
        {
            details.Text = allItems.Count == 0 ? CachedEmptyMessage : $"No plugins match “{q}”.";
        }
    }

    private static bool Matches(HubPluginView v, string q)
    {
        HubPluginInfo p = v.Info;
        return Contains(p.FriendlyName, q) || Contains(p.Id, q)
            || Contains(p.Author, q) || Contains(p.Tooltip, q);
    }

    private static bool Contains(string s, string q) =>
        s != null && s.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;

    private string Format(HubPluginView v)
    {
        string box = v.Enabled ? "[x]" : "[ ]";
        // Dev folders are registered locally, not fetched from a hub — mark them so.
        if (v.IsDevFolder)
            return $"{box} {v.Info.FriendlyName} - dev folder";
        string kind = v.Info.Kind == HubPluginKind.Mod ? " (mod)" : "";
        // Only label the source when it isn't the default hub — that suffix is implied.
        bool fromDefault = string.IsNullOrEmpty(v.Info.SourceLabel)
            || string.Equals(v.Info.SourceLabel, defaultHubLabel, StringComparison.OrdinalIgnoreCase);
        string src = fromDefault ? "" : $"  — {v.Info.SourceLabel}";
        return $"{box} {v.Info.FriendlyName}{kind}{src}";
    }

    private void ShowDetails()
    {
        int i = list.SelectedItem;
        if (i < 0 || i >= items.Count)
        {
            if (items.Count > 0)
                details.Text = "";
            return;
        }
        HubPluginInfo p = items[i].Info;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(p.FriendlyName);
        if (!string.IsNullOrEmpty(p.Author)) sb.AppendLine("by " + p.Author);
        if (!string.IsNullOrEmpty(p.RepoId)) sb.AppendLine(p.RepoId);
        sb.AppendLine();
        sb.AppendLine("Id: " + p.Id);
        if (p.DependencyIds != null && p.DependencyIds.Length > 0)
            sb.AppendLine("Depends on: " + string.Join(", ", p.DependencyIds));
        sb.AppendLine();
        if (!string.IsNullOrEmpty(p.Tooltip)) { sb.AppendLine(p.Tooltip); sb.AppendLine(); }
        if (!string.IsNullOrEmpty(p.Description)) sb.AppendLine(p.Description);
        details.Text = sb.ToString();
    }

    private void Toggle()
    {
        int i = list.SelectedItem;
        if (i < 0 || i >= items.Count)
            return;
        HubPluginView v = items[i];
        try
        {
            if (v.IsDevFolder)
            {
                plugins.SetDevFolderEnabled(v.Id, v.DataFile, !v.Enabled);
                Refresh();
                list.SelectedItem = i;
                return;
            }

            IReadOnlyList<string> touched = plugins.SetHubPluginEnabled(v.Id, !v.Enabled);
            Refresh();
            list.SelectedItem = i;
            if (!v.Enabled && touched.Count > 1)
                Dialogs.Info("Enabled with dependencies",
                    $"Enabled '{v.Info.FriendlyName}' and {touched.Count - 1} dependency plugin(s).\n\n" +
                    "Takes effect on the next server start.");
        }
        catch (Exception e)
        {
            Dialogs.Error("Toggle plugin", e.Message);
        }
    }
}
