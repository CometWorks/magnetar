using System;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>Set or clear the server password. The plaintext is never stored — only the PBKDF2 hash+salt.</summary>
internal static class PasswordDialog
{
    public static void Show(DedicatedConfigDocument cfg, AtomicFile writer, Action onSaved)
    {
        var dlg = new Dialog("Server Password", 56, 12) { ColorScheme = TurboVisionTheme.Dialog };

        var state = new Label(cfg.HasPassword ? "A password is currently set." : "No password is set.")
        { X = 1, Y = 0, Width = Dim.Fill(2) };

        var p1 = new TextField("") { X = 14, Y = 2, Width = Dim.Fill(2), Secret = true };
        var p2 = new TextField("") { X = 14, Y = 4, Width = Dim.Fill(2), Secret = true };

        dlg.Add(state,
            new Label("New:") { X = 1, Y = 2 }, p1,
            new Label("Confirm:") { X = 1, Y = 4 }, p2,
            new Label("Leave both empty and press Set to clear the password.") { X = 1, Y = 6, Width = Dim.Fill(2) });

        var set = new Button("Set", true);
        set.Clicked += () =>
        {
            string a = p1.Text.ToString();
            string b = p2.Text.ToString();
            if (a != b)
            {
                Dialogs.Error("Password", "The two entries do not match.");
                return;
            }
            try
            {
                cfg.SetPassword(string.IsNullOrEmpty(a) ? null : a);
                cfg.Save(writer);
                onSaved?.Invoke();
                Application.RequestStop(dlg);
                Dialogs.Info("Password", string.IsNullOrEmpty(a) ? "Password cleared." : "Password set.");
            }
            catch (Exception e)
            {
                Dialogs.Error("Save failed", e.Message);
            }
        };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop(dlg);
        dlg.AddButton(set);
        dlg.AddButton(cancel);

        p1.SetFocus();
        Application.Run(dlg);
    }
}
