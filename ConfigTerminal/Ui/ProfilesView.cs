using System;
using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Manages plugin <em>profiles</em> — named presets of the enabled-plugin set,
/// stored as <c>Profiles/&lt;Key&gt;.xml</c>, with <c>Current.xml</c> the active set
/// the server loads. Mirrors Magnetar's in-game profile UI (load, save-new,
/// update, rename, delete) from the terminal: <b>Load</b> copies a preset into the
/// active set; <b>Save As</b>/<b>Update</b> snapshot the active set into a preset.
/// The preset whose enabled set matches the active one is marked.
/// </summary>
internal sealed class ProfilesView : Window
{
    private readonly ProfileCatalog catalog;
    private readonly Action onActiveChanged;
    private readonly ListView list;
    private readonly Label activeLabel;
    private List<ProfileInfo> profiles = new();

    public ProfilesView(string magnetarConfigDir, AtomicFile writer, Action onActiveChanged = null) : base("Plugin Profiles")
    {
        catalog = new ProfileCatalog(magnetarConfigDir, writer);
        this.onActiveChanged = onActiveChanged;
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        var frame = new FrameView("Saved profiles — Enter/Load applies to the active set")
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(3), ColorScheme = TurboVisionTheme.Window,
        };
        list = new ListView(Array.Empty<string>())
        { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = TurboVisionTheme.Window };
        list.OpenSelectedItem += _ => Load();
        frame.Add(list);

        activeLabel = new Label("") { X = 0, Y = Pos.AnchorEnd(2), Width = Dim.Fill(), Height = 1 };

        var load = new Button("_Load") { X = 0, Y = Pos.AnchorEnd(1) };
        load.Clicked += Load;
        var saveAs = new Button("_Save As New…") { X = Pos.Right(load) + 1, Y = Pos.AnchorEnd(1) };
        saveAs.Clicked += SaveAsNew;
        var update = new Button("_Update") { X = Pos.Right(saveAs) + 1, Y = Pos.AnchorEnd(1) };
        update.Clicked += Update;
        var rename = new Button("Re_name…") { X = Pos.Right(update) + 1, Y = Pos.AnchorEnd(1) };
        rename.Clicked += Rename;
        var delete = new Button("_Delete") { X = Pos.Right(rename) + 1, Y = Pos.AnchorEnd(1) };
        delete.Clicked += Delete;

        Add(frame, activeLabel, load, saveAs, update, rename, delete);
        Refresh();
    }

    private void Refresh()
    {
        profiles = catalog.NamedProfiles().ToList();
        int keep = list.SelectedItem;
        list.SetSource(profiles.Select(Format).ToList());
        if (profiles.Count > 0)
            list.SelectedItem = Math.Min(Math.Max(0, keep), profiles.Count - 1);

        string match = catalog.ActiveMatchKey();
        activeLabel.Text = match == null
            ? " Active set (Current.xml): unsaved — not matching any profile"
            : $" Active set (Current.xml): matches profile '{profiles.First(p => p.Key == match).Name}'";
    }

    private static string Format(ProfileInfo p) =>
        $"{(p.MatchesActive ? "→ " : "  ")}{p.Name}";

    private ProfileInfo Selected()
    {
        int i = list.SelectedItem;
        return i >= 0 && i < profiles.Count ? profiles[i] : null;
    }

    private void Load()
    {
        ProfileInfo p = Selected();
        if (p == null) return;
        if (!Dialogs.Confirm("Load profile",
                $"Make '{p.Name}' the active plugin set?\n\n" +
                "This overwrites Current.xml with this profile's enabled plugins.\n" +
                "It takes effect on the next server start."))
            return;
        try
        {
            catalog.Load(p.Key);
            Refresh();
            onActiveChanged?.Invoke();
        }
        catch (Exception e) { Dialogs.Error("Load profile", e.Message); }
    }

    private void SaveAsNew()
    {
        string name = Dialogs.Prompt("Save profile", "New profile name:");
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            if (!catalog.SaveCurrentAs(name))
            {
                if (Dialogs.Confirm("Profile exists",
                        $"A profile named '{name.Trim()}' already exists.\nOverwrite it with the active set?"))
                {
                    catalog.Update(PluginProfileDocument.CleanKey(name.Trim()));
                }
                else return;
            }
            Refresh();
        }
        catch (Exception e) { Dialogs.Error("Save profile", e.Message); }
    }

    private void Update()
    {
        ProfileInfo p = Selected();
        if (p == null)
        {
            Dialogs.Info("Update", "Select a profile to overwrite, or use 'Save As New…'.");
            return;
        }
        if (!Dialogs.Confirm("Update profile",
                $"Overwrite profile '{p.Name}' with the current active plugin set?"))
            return;
        try
        {
            catalog.Update(p.Key);
            Refresh();
        }
        catch (Exception e) { Dialogs.Error("Update profile", e.Message); }
    }

    private void Rename()
    {
        ProfileInfo p = Selected();
        if (p == null) return;
        string newName = Dialogs.Prompt("Rename profile", "New name:", p.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName.Trim() == p.Name) return;
        try
        {
            string newKey = PluginProfileDocument.CleanKey(newName.Trim());
            if (!string.Equals(newKey, p.Key, StringComparison.Ordinal) && catalog.Exists(newKey))
            {
                Dialogs.Error("Rename profile", $"A profile named '{newName.Trim()}' already exists.");
                return;
            }
            catalog.Rename(p.Key, newName.Trim());
            Refresh();
        }
        catch (Exception e) { Dialogs.Error("Rename profile", e.Message); }
    }

    private void Delete()
    {
        ProfileInfo p = Selected();
        if (p == null) return;
        if (!Dialogs.Confirm("Delete profile",
                $"Delete profile '{p.Name}'?\n\nThe active set (Current.xml) is not affected."))
            return;
        try
        {
            catalog.Delete(p.Key);
            Refresh();
        }
        catch (Exception e) { Dialogs.Error("Delete profile", e.Message); }
    }
}
