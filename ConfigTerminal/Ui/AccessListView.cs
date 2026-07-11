using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>Editors for the Administrators / Banned / Reserved SteamID lists + GroupID.</summary>
internal sealed class AccessListView : Window
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
        var groupLabel = new Label("GroupID:") { X = 1, Y = Pos.AnchorEnd(3) };

        var save = new Button("_Save (F2)") { X = 1, Y = Pos.AnchorEnd(1) };
        save.Clicked += Save;

        Add(adminFrame, bannedFrame, reservedFrame, groupLabel, groupId, save);
    }

    public override bool ProcessKey(KeyEvent kb)
    {
        if (kb.Key == Key.F2) { Save(); return true; }
        return base.ProcessKey(kb);
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
            string id = PromptId(title);
            if (id != null) { data.Add(id); lv.SetSource(data); }
        };
        var del = new Button("Del") { X = Pos.Right(add) + 1, Y = Pos.AnchorEnd(1) };
        del.Clicked += () =>
        {
            if (lv.SelectedItem >= 0 && lv.SelectedItem < data.Count)
            {
                data.RemoveAt(lv.SelectedItem);
                lv.SetSource(data);
            }
        };
        f.Add(lv, add, del);
        frame = f;
        return lv;
    }

    private static string PromptId(string title)
    {
        var dlg = new Dialog($"Add to {title}", 44, 8) { ColorScheme = TurboVisionTheme.Dialog };
        var field = new TextField("") { X = 1, Y = 2, Width = Dim.Fill(2) };
        string result = null;
        var ok = new Button("OK", true);
        ok.Clicked += () =>
        {
            string t = field.Text.ToString().Trim();
            if (ulong.TryParse(t, out _)) { result = t; Application.RequestStop(dlg); }
            else Dialogs.Error("Invalid", "Enter a numeric SteamID.");
        };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop(dlg);
        dlg.Add(new Label("SteamID (unsignedLong):") { X = 1, Y = 1 }, field);
        dlg.AddButton(ok);
        dlg.AddButton(cancel);
        field.SetFocus();
        Application.Run(dlg);
        return result;
    }

    private void Save()
    {
        try
        {
            cfg.SetAdministrators(admins);
            cfg.SetBanned(banned);
            cfg.SetReserved(reserved);
            cfg.GroupId = string.IsNullOrWhiteSpace(groupId.Text.ToString()) ? "0" : groupId.Text.ToString().Trim();
            cfg.Save(writer);
            onSaved?.Invoke();
            Dialogs.Info("Saved", "Access lists saved.");
        }
        catch (Exception e)
        {
            Dialogs.Error("Save failed", e.Message);
        }
    }
}
