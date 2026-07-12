using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>Ordered mod-list editor for a world's Sandbox_config.sbc.</summary>
internal sealed class ModListView : Window, IAutoSaveContent
{
    private readonly WorldConfigDocument doc;
    private readonly AtomicFile writer;
    private readonly ModList mods;
    private readonly ListView list;

    // Content snapshot for change-only saving, and the last validation problems
    // (surfaced as the leave-warning when the list can't be saved).
    private string snapshot;
    private IReadOnlyList<string> currentIssues = Array.Empty<string>();

    public ModListView(WorldInfo world, AtomicFile writer) : base($"Mods — {world.SessionName}")
    {
        this.writer = writer;
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        doc = WorldConfigDocument.Open(world.WorldConfigPath);
        mods = doc.ReadMods();
        // Normalize the baseline through WriteMods so an untouched list doesn't
        // look "changed" on the first flush.
        doc.WriteMods(mods);
        snapshot = doc.ToCanonicalString();

        list = new ListView(Array.Empty<string>())
        {
            X = 1, Y = 0, Width = Dim.Fill(1), Height = Dim.Fill(2),
            ColorScheme = TurboVisionTheme.Window,
        };

        var add = new Button("_Add") { X = 1, Y = Pos.AnchorEnd(2) };
        add.Clicked += AddMod;
        var del = new Button("_Del") { X = Pos.Right(add) + 1, Y = Pos.AnchorEnd(2) };
        del.Clicked += Remove;
        var up = new Button("_Up") { X = Pos.Right(del) + 1, Y = Pos.AnchorEnd(2) };
        up.Clicked += () => { mods.MoveUp(list.SelectedItem); Refresh(); };
        var down = new Button("Dow_n") { X = Pos.Right(up) + 1, Y = Pos.AnchorEnd(2) };
        down.Clicked += () => { mods.MoveDown(list.SelectedItem); Refresh(); };
        var dep = new Button("Toggle Dep_endency") { X = Pos.Right(down) + 1, Y = Pos.AnchorEnd(2) };
        dep.Clicked += ToggleDependency;
        var hint = new Label("Changes save automatically") { X = Pos.Right(dep) + 2, Y = Pos.AnchorEnd(2) };

        Add(list, add, del, up, down, dep, hint);
        Refresh();
    }

    private void Refresh()
    {
        list.SetSource(mods.Items.Select((m, i) =>
            $"{i + 1,3}. {m.PublishedFileId,-12} {(m.IsDependency ? "[dep] " : "      ")}{m.FriendlyName}").ToList());
    }

    private void AddMod()
    {
        string id = Prompt("Add mod", "Workshop id:");
        if (string.IsNullOrWhiteSpace(id)) return;
        if (!ulong.TryParse(id.Trim(), out ulong pid) || pid == 0)
        {
            Dialogs.Error("Add mod", "Enter a numeric Workshop id.");
            return;
        }
        string name = Prompt("Add mod", "Friendly name (optional):") ?? string.Empty;
        mods.Items.Add(new ModItem { PublishedFileId = pid, FriendlyName = name });
        Refresh();
    }

    private void Remove()
    {
        int i = list.SelectedItem;
        if (i >= 0 && i < mods.Items.Count)
        {
            mods.Items.RemoveAt(i);
            Refresh();
        }
    }

    private void ToggleDependency()
    {
        int i = list.SelectedItem;
        if (i >= 0 && i < mods.Items.Count)
        {
            mods.Items[i].IsDependency = !mods.Items[i].IsDependency;
            Refresh();
        }
    }

    public void FlushPendingSave()
    {
        currentIssues = mods.Validate();
        if (currentIssues.Count > 0)
            return; // don't write an invalid list; the leave-warning surfaces these

        doc.WriteMods(mods);
        if (doc.ToCanonicalString() == snapshot)
            return; // nothing changed

        try
        {
            doc.Save(writer);
            snapshot = doc.ToCanonicalString();
        }
        catch
        {
            // Retry on the next tick; the auto-save path must never pop a dialog.
        }
    }

    public IReadOnlyList<string> InvalidFields => currentIssues;

    private static string Prompt(string title, string label)
    {
        var dlg = new Dialog(title, 50, 8) { ColorScheme = TurboVisionTheme.Dialog };
        var field = new TextField("") { X = 1, Y = 2, Width = Dim.Fill(2) };
        var lbl = new Label(label) { X = 1, Y = 1 };
        string result = null;
        var ok = new Button("OK", true);
        ok.Clicked += () => { result = field.Text.ToString(); Application.RequestStop(dlg); };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => { result = null; Application.RequestStop(dlg); };
        dlg.Add(lbl, field);
        dlg.AddButton(ok);
        dlg.AddButton(cancel);
        field.SetFocus();
        Application.Run(dlg);
        return result;
    }
}
