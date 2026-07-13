using System;
using System.IO;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Prompts for the folder pair (and launcher / DS install) that identifies the
/// instance. Pre-filled with resolved defaults; the DS data dir must exist.
/// Returns null on cancel.
/// </summary>
internal static class InstancePickerDialog
{
    public static InstanceBinding Show(InstanceBinding seed = null)
    {
        seed ??= InstanceLocator.ResolveDefaults(new InstanceBinding());

        // Span the full terminal width, leaving a 2-column margin on each side.
        var dlg = new Dialog("Open Instance", 74, 16)
        {
            ColorScheme = TurboVisionTheme.Dialog,
            X = 2, Width = Dim.Fill(2),
        };

        TextField data = Field(dlg, "DS data dir (-path):", 1, seed.DataDir,
            cur => FileDialogs.PickDirectory("DS data dir", "Select the DS data directory", cur));
        TextField config = Field(dlg, "Magnetar config (-config):", 3, seed.MagnetarConfigDir,
            cur => FileDialogs.PickDirectory("Magnetar config", "Select the Magnetar config directory", cur));
        TextField exe = Field(dlg, "Launcher (-magnetar):", 5, seed.MagnetarExePath,
            cur => FileDialogs.PickFile("Launcher", "Select the Magnetar launcher executable", cur));
        TextField ds64 = Field(dlg, "DS install (-ds64):", 7, seed.Ds64Dir,
            cur => FileDialogs.PickDirectory("DS install", "Select the DedicatedServer64 folder", cur));

        var note = new Label("The DS data dir must already exist.") { X = 1, Y = 9, Width = Dim.Fill(2) };
        dlg.Add(note);

        InstanceBinding result = null;
        var ok = new Button("Open", true);
        ok.Clicked += () =>
        {
            string d = data.Text.ToString().Trim();
            if (string.IsNullOrEmpty(d) || !Directory.Exists(d))
            {
                Dialogs.Error("Open Instance", "The DS data directory does not exist.");
                return;
            }
            result = new InstanceBinding
            {
                DataDir = d,
                MagnetarConfigDir = Empty(config),
                MagnetarExePath = Empty(exe),
                Ds64Dir = Empty(ds64),
            };
            InstanceLocator.ResolveDefaults(result);
            Application.RequestStop(dlg);
        };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop(dlg);
        dlg.AddButton(ok);
        dlg.AddButton(cancel);

        data.SetFocus();
        Application.Run(dlg);
        return result;
    }

    private static string Empty(TextField f)
    {
        string s = f.Text.ToString().Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }

    private static TextField Field(Dialog dlg, string label, int y, string value, Func<string, string> browse)
    {
        dlg.Add(new Label(label) { X = 1, Y = y });
        var tf = new TextField(value ?? "") { X = 28, Y = y, Width = Dim.Fill(13) };
        // The TextField constructor parks the cursor at the end of the seeded text, so a path
        // longer than the box opens scrolled all the way right with its start cropped off the
        // left edge. Reset the cursor to column 0 so the view is anchored at the start.
        tf.CursorPosition = 0;
        var browseButton = new Button("Browse") { X = Pos.Right(tf) + 1, Y = y };
        browseButton.Clicked += () =>
        {
            string picked = browse(tf.Text.ToString().Trim());
            if (!string.IsNullOrEmpty(picked))
            {
                tf.Text = NStack.ustring.Make(picked);
                tf.CursorPosition = 0;
            }
        };
        dlg.Add(tf, browseButton);
        return tf;
    }
}
