using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.State;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Manages Magnetar's mod sources (the <c>&lt;ModSources&gt;</c> list in
/// <c>sources.xml</c>, kept in lockstep with the enabled set in
/// <c>Profiles/Current.xml</c>). Mods are added by Workshop id or by pasting
/// Workshop/collection URLs; an optional Steam Web API integration fills in mod
/// names, expands collections and resolves transitive dependencies.
/// </summary>
internal sealed class ModSourcesView : Window
{
    private readonly MagnetarPlugins plugins;
    private readonly ToolSettings settings;
    private readonly AtomicFile writer;
    private readonly ListView list;
    private List<ModView> mods = new();

    public ModSourcesView(string magnetarConfigDir, AtomicFile writer, ToolSettings settings) : base("Mods")
    {
        this.writer = writer;
        this.settings = settings;
        plugins = new MagnetarPlugins(magnetarConfigDir, writer);
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        list = new ListView(Array.Empty<string>())
        { X = 1, Y = 0, Width = Dim.Fill(1), Height = Dim.Fill(3), ColorScheme = TurboVisionTheme.Window };
        list.OpenSelectedItem += _ => ToggleActive();

        var add = new Button("_Add…") { X = 1, Y = Pos.AnchorEnd(2) };
        add.Clicked += AddMods;
        var toggle = new Button("_Toggle") { X = Pos.Right(add) + 1, Y = Pos.AnchorEnd(2) };
        toggle.Clicked += ToggleActive;
        var rename = new Button("Re_name") { X = Pos.Right(toggle) + 1, Y = Pos.AnchorEnd(2) };
        rename.Clicked += Rename;
        var remove = new Button("_Remove") { X = Pos.Right(rename) + 1, Y = Pos.AnchorEnd(2) };
        remove.Clicked += Remove;

        var names = new Button("Resolve _Names") { X = 1, Y = Pos.AnchorEnd(1) };
        names.Clicked += ResolveNames;
        var deps = new Button("Resolve _Dependencies") { X = Pos.Right(names) + 1, Y = Pos.AnchorEnd(1) };
        deps.Clicked += ResolveDependencies;
        var key = new Button("Steam API _Key…") { X = Pos.Right(deps) + 1, Y = Pos.AnchorEnd(1) };
        key.Clicked += SetApiKey;

        Add(list, add, toggle, rename, remove, names, deps, key);
        Refresh();
    }

    public override bool ProcessKey(KeyEvent kb)
    {
        if (kb.Key == (Key)' ' && list.HasFocus)
        {
            ToggleActive();
            return true;
        }
        return base.ProcessKey(kb);
    }

    private void Refresh()
    {
        plugins.Reload();
        mods = plugins.Mods().ToList();
        int keep = list.SelectedItem;
        list.SetSource(mods.Select(Format).ToList());
        if (mods.Count > 0)
            list.SelectedItem = Math.Min(Math.Max(0, keep), mods.Count - 1);
    }

    private static string Format(ModView m)
    {
        string box = m.Active ? "[x]" : (m.SourceEnabled || m.InProfile ? "[~]" : "[ ]");
        return $"{box} {m.Id,-12} {m.Name}";
    }

    private ModView Selected()
    {
        int i = list.SelectedItem;
        return i >= 0 && i < mods.Count ? mods[i] : null;
    }

    private void AddMods()
    {
        string input = Dialogs.Prompt("Add mods",
            "Workshop id(s) or URL(s) — space/comma separated, collections OK:");
        if (string.IsNullOrWhiteSpace(input)) return;

        List<long> ids = WorkshopResolver.ExtractIds(input);
        if (ids.Count == 0)
        {
            Dialogs.Error("Add mods", "No Workshop ids found in that input.");
            return;
        }

        // Add immediately by id (name defaults to the id); the user can Resolve
        // Names afterwards to fill friendly names / expand collections online.
        foreach (long id in ids)
            plugins.AddMod(id, id.ToString(), active: true);
        Refresh();
        Dialogs.Info("Add mods",
            $"Added {ids.Count} mod(s). Use 'Resolve Names' to fetch friendly names\n" +
            "and expand any collections from the Steam Workshop.");
    }

    private void ToggleActive()
    {
        ModView m = Selected();
        if (m == null) return;
        plugins.SetModActive(m.Id, !m.Active);
        int i = list.SelectedItem;
        Refresh();
        list.SelectedItem = i;
    }

    private void Rename()
    {
        ModView m = Selected();
        if (m == null) return;
        string name = Dialogs.Prompt("Rename mod", $"Name for {m.Id}:", m.Name);
        if (name == null) return;
        plugins.SetModName(m.Id, name);
        Refresh();
    }

    private void Remove()
    {
        ModView m = Selected();
        if (m == null) return;
        if (!Dialogs.Confirm("Remove mod", $"Remove mod {m.Id} ({m.Name})?"))
            return;
        plugins.RemoveMod(m.Id);
        Refresh();
    }

    private void ResolveNames()
    {
        var ids = mods.Select(m => m.Id).ToList();
        if (ids.Count == 0)
        {
            Dialogs.Info("Resolve names", "No mods to resolve.");
            return;
        }

        var resolver = new WorkshopResolver();
        Dialogs.RunBackground(
            () =>
            {
                Dictionary<long, string> names = resolver.ResolveNames(ids, out List<string> warnings, out List<long> collections);
                return (names, warnings, collections);
            },
            r =>
            {
                foreach (var kv in r.names)
                {
                    if (mods.Any(m => m.Id == kv.Key))
                        plugins.SetModName(kv.Key, kv.Value);
                    else
                        // Newly-discovered collection members are added as active mods.
                        plugins.AddMod(kv.Key, kv.Value, active: true);
                }
                // A pasted collection is expanded into its members; drop the
                // collection entry itself so it does not linger as an inert mod.
                foreach (long collectionId in r.collections)
                    plugins.RemoveMod(collectionId);
                Refresh();
                string msg = $"Resolved {r.names.Count} name(s).";
                if (r.collections.Count > 0)
                    msg += $" Expanded {r.collections.Count} collection(s).";
                if (r.warnings.Count > 0)
                    msg += "\n\n" + string.Join("\n", r.warnings.Take(8));
                Dialogs.Info("Resolve names", msg);
            });
    }

    private void ResolveDependencies()
    {
        if (string.IsNullOrWhiteSpace(settings.SteamWebApiKey))
        {
            if (!Dialogs.Confirm("Steam API key required",
                    "Dependency resolution needs a Steam Web API key.\n" +
                    "Get one free at steamcommunity.com/dev/apikey.\n\nSet it now?"))
                return;
            SetApiKey();
            if (string.IsNullOrWhiteSpace(settings.SteamWebApiKey))
                return;
        }

        var input = mods.Select(m => (m.Id, m.Name)).ToList();
        if (input.Count == 0)
        {
            Dialogs.Info("Resolve dependencies", "No mods to resolve.");
            return;
        }

        var resolver = new WorkshopResolver();
        string apiKey = settings.SteamWebApiKey;
        Dialogs.RunBackground(
            () => resolver.ResolveDependencies(input, apiKey),
            r =>
            {
                foreach (var (id, name, isDep) in r.Mods)
                {
                    if (!mods.Any(m => m.Id == id))
                        plugins.AddMod(id, name, active: true);
                    else if (!string.IsNullOrWhiteSpace(name) && name != id.ToString())
                        plugins.SetModName(id, name);
                }
                Refresh();
                string msg = r.AddedDependencies == 0
                    ? "No new dependencies found."
                    : $"Added {r.AddedDependencies} dependency mod(s).";
                if (r.Warnings.Count > 0)
                    msg += "\n\n" + string.Join("\n", r.Warnings.Take(8));
                Dialogs.Info("Resolve dependencies", msg);
            });
    }

    private void SetApiKey()
    {
        string key = Dialogs.Prompt("Steam Web API key",
            "Key (from steamcommunity.com/dev/apikey), blank to clear:", settings.SteamWebApiKey ?? "");
        if (key == null) return;
        settings.SteamWebApiKey = key.Trim();
        settings.Save(writer);
    }
}
