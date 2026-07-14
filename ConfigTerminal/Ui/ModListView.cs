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
    // (surfaced as the leave-warning when the list can't be saved). `touched`
    // gates the flush so idle ticks skip the serialize-and-compare entirely.
    private string snapshot;
    private bool touched;
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
        up.Clicked += () => MoveSelected(-1);
        var down = new Button("Dow_n") { X = Pos.Right(up) + 1, Y = Pos.AnchorEnd(2) };
        down.Clicked += () => MoveSelected(+1);
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

    // Reorder the selected mod one slot, keeping the selection on that mod so it
    // can be moved again without re-selecting it.
    private void MoveSelected(int delta)
    {
        int i = list.SelectedItem;
        int target = i + delta;
        if (i < 0 || target < 0 || target >= mods.Items.Count)
            return;

        // A move only swaps two rows, so the scroll position should stay put.
        // Refresh reloads the source, which resets TopItem to 0, so capture and
        // restore it. EnsureSelectedItemVisible then nudges the scroll by exactly
        // one row only if the moved item ended up just outside the visible window.
        int top = list.TopItem;
        if (delta < 0)
            mods.MoveUp(i);
        else
            mods.MoveDown(i);
        touched = true;
        Refresh();
        list.TopItem = top;
        list.SelectedItem = target;
        list.EnsureSelectedItemVisible();
    }

    private void AddMod()
    {
        // Accept either a numeric Workshop id or a Steam Workshop URL (or several,
        // pasted together) — ExtractIds pulls the ids out of whatever is entered.
        string input = Dialogs.Prompt("Add mod", "Workshop id or URL:", width: 74, validate: s =>
            WorkshopResolver.ExtractIds(s).Count > 0
                ? null
                : "Enter a numeric Workshop id or a Steam Workshop URL.");
        if (input == null) return;

        var existing = new HashSet<ulong>(mods.Items.Select(m => m.PublishedFileId));
        var wanted = WorkshopResolver.ExtractIds(input)
            .Where(id => !existing.Contains((ulong)id))
            .ToList();
        if (wanted.Count == 0)
        {
            Dialogs.Info("Add mod", "That mod is already in the list.");
            return;
        }

        // Look the names up off the UI thread so the resolve doesn't freeze the
        // window; ResolveOnline swallows transport errors into an offline result.
        Dialogs.RunBackground(() => ResolveOnline(wanted), applied => ApplyResolved(wanted, applied));
    }

    // The friendly-name lookup, run on a background thread. Returns null when the
    // Workshop can't be reached so ApplyResolved falls back to adding bare ids.
    private static WorkshopResolver.ResolveResult ResolveOnline(List<long> wanted)
    {
        try
        {
            return new WorkshopResolver().Resolve(wanted);
        }
        catch
        {
            return null; // offline / API failure — add ids without names
        }
    }

    // Back on the UI thread: append the resolved mods (skipping any that snuck in
    // while the lookup ran) and surface warnings. `wanted` is the pre-filtered id
    // list, used as the fallback when the lookup failed.
    private void ApplyResolved(List<long> wanted, WorkshopResolver.ResolveResult resolved)
    {
        var existing = new HashSet<ulong>(mods.Items.Select(m => m.PublishedFileId));

        if (resolved == null)
        {
            int offlineAdded = 0;
            foreach (long id in wanted)
                if (existing.Add((ulong)id))
                {
                    mods.Items.Add(new ModItem { PublishedFileId = (ulong)id });
                    offlineAdded++;
                }
            if (offlineAdded > 0) { touched = true; Refresh(); }
            Dialogs.ErrorDetails("Add mod", "Couldn't reach the Steam Workshop.",
                $"Added {offlineAdded} mod(s) by id without a friendly name.\n" +
                "Edit the name later once you're back online.");
            return;
        }

        int added = 0;
        foreach ((long id, string name) in resolved.Mods)
            if (existing.Add((ulong)id))
            {
                mods.Items.Add(new ModItem { PublishedFileId = (ulong)id, FriendlyName = name ?? string.Empty });
                added++;
            }
        if (added > 0) { touched = true; Refresh(); }

        if (resolved.Warnings.Count > 0)
            Dialogs.InfoDetails("Add mod", $"Added {added} mod(s).", string.Join("\n", resolved.Warnings));
        else if (added == 0)
            Dialogs.Info("Add mod", "Nothing to add.");
    }

    private void Remove()
    {
        int i = list.SelectedItem;
        if (i >= 0 && i < mods.Items.Count)
        {
            mods.Items.RemoveAt(i);
            touched = true;
            Refresh();
        }
    }

    private void ToggleDependency()
    {
        int i = list.SelectedItem;
        if (i >= 0 && i < mods.Items.Count)
        {
            mods.Items[i].IsDependency = !mods.Items[i].IsDependency;
            touched = true;
            Refresh();
        }
    }

    public void FlushPendingSave()
    {
        if (!touched)
            return; // nothing edited since the last flush — skip the serialize+compare

        currentIssues = mods.Validate();
        if (currentIssues.Count > 0)
            return; // don't write an invalid list; the leave-warning surfaces these

        doc.WriteMods(mods);
        if (doc.ToCanonicalString() == snapshot)
        {
            touched = false;
            return; // edited back to the original — nothing to write
        }

        try
        {
            doc.Save(writer);
            snapshot = doc.ToCanonicalString();
            touched = false;
        }
        catch
        {
            // Retry on the next tick; the auto-save path must never pop a dialog.
        }
    }

    public IReadOnlyList<string> InvalidFields => currentIssues;
}
