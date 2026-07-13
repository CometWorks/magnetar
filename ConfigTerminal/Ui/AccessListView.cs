using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>Editors for the Administrators / Banned / Reserved SteamID lists + GroupID.</summary>
internal sealed class AccessListView : Window, IAutoSaveContent
{
    private readonly DedicatedConfigDocument cfg;
    private readonly AtomicFile writer;
    private readonly Action onSaved;

    private readonly List<string> admins;
    private readonly List<string> banned;
    private readonly List<string> reserved;
    private readonly ListView adminList;
    private readonly ListView bannedList;
    private readonly ListView reservedList;
    private readonly TextField groupId;

    // Content snapshot for change-only saving, and the live-validity of GroupID
    // (the only free-typed field; the SteamID lists are numeric-enforced at add).
    // `touched` gates the flush so idle ticks skip the serialize-and-compare.
    private string snapshot;
    private bool touched;
    private bool groupIdInvalid;

    public AccessListView(DedicatedConfigDocument cfg, AtomicFile writer, Action onSaved) : base("Access Lists")
    {
        this.cfg = cfg;
        this.writer = writer;
        this.onSaved = onSaved;
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        admins = cfg.Administrators.ToList();
        banned = cfg.Banned.ToList();
        reserved = cfg.Reserved.ToList();

        adminList = MakeColumn("Administrators", 1, admins, out View adminFrame);
        bannedList = MakeColumn("Banned", 0, banned, out View bannedFrame);
        reservedList = MakeColumn("Reserved", 0, reserved, out View reservedFrame);
        bannedFrame.X = Pos.Right(adminFrame);
        reservedFrame.X = Pos.Right(bannedFrame);

        groupId = new TextField(cfg.GroupId) { X = 11, Y = Pos.AnchorEnd(3), Width = 20 };
        groupId.TextChanged += _ => ValidateGroupId();
        var groupLabel = new Label("GroupID:") { X = 1, Y = Pos.AnchorEnd(3) };

        var hint = new Label("Changes save automatically") { X = 1, Y = Pos.AnchorEnd(1) };

        Add(adminFrame, bannedFrame, reservedFrame, groupLabel, groupId, hint);

        // Normalize the baseline through the same write path used by the flush, so
        // merely viewing the panel (no edits) never looks "changed" on the first tick.
        CommitToDocument();
        snapshot = cfg.ToCanonicalString();
    }

    // GroupID must be a numeric group id; empty is allowed and means "0". Show it
    // red while invalid; the invalid value is not written (the original is kept).
    private void ValidateGroupId()
    {
        string t = groupId.Text.ToString().Trim();
        groupIdInvalid = t.Length > 0 && !ulong.TryParse(t, out _);
        groupId.ColorScheme = groupIdInvalid ? TurboVisionTheme.Error : TurboVisionTheme.Window;
        groupId.SetNeedsDisplay();
        touched = true;
    }

    private ListView MakeColumn(string title, int x, List<string> data, out View frame)
    {
        var f = new FrameView(title)
        {
            X = x == 1 ? 1 : 0,
            Y = 0,
            Width = Dim.Percent(33),
            Height = Dim.Fill(4),
            ColorScheme = TurboVisionTheme.Window,
        };
        var lv = new ListView(data) { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(1), ColorScheme = TurboVisionTheme.Window };
        var add = new Button("Add") { X = 0, Y = Pos.AnchorEnd(1) };
        add.Clicked += () =>
        {
            string id = Dialogs.Prompt($"Add to {title}", "SteamID (unsignedLong):", width: 44, validate: s =>
                ulong.TryParse(s.Trim(), out _) ? null : "Enter a numeric SteamID.");
            if (id != null) { data.Add(id.Trim()); lv.SetSource(data); touched = true; }
        };
        var del = new Button("Del") { X = Pos.Right(add) + 1, Y = Pos.AnchorEnd(1) };
        del.Clicked += () =>
        {
            if (lv.SelectedItem >= 0 && lv.SelectedItem < data.Count)
            {
                data.RemoveAt(lv.SelectedItem);
                lv.SetSource(data);
                touched = true;
            }
        };
        f.Add(lv, add, del);
        frame = f;
        return lv;
    }

    // Push the working buffers into the document. The lists are always valid
    // (numeric-enforced at add); GroupID is only applied when valid so an invalid
    // entry keeps the original value.
    private void CommitToDocument()
    {
        cfg.SetAdministrators(admins);
        cfg.SetBanned(banned);
        cfg.SetReserved(reserved);
        if (!groupIdInvalid)
        {
            string t = groupId.Text.ToString().Trim();
            cfg.GroupId = string.IsNullOrWhiteSpace(t) ? "0" : t;
        }
    }

    public void FlushPendingSave()
    {
        if (!touched)
            return; // nothing edited since the last flush — skip the serialize+compare

        CommitToDocument();

        if (cfg.ToCanonicalString() == snapshot)
        {
            touched = false;
            return; // edited back to the original — nothing to write
        }

        try
        {
            cfg.Save(writer);
            onSaved?.Invoke();
            snapshot = cfg.ToCanonicalString();
            touched = false;
        }
        catch
        {
            // Retry on the next tick; the auto-save path must never pop a dialog.
        }
    }

    public IReadOnlyList<string> InvalidFields =>
        groupIdInvalid ? new[] { "GroupID" } : Array.Empty<string>();
}
