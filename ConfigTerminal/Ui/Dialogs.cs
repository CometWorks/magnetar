using System;
using Terminal.Gui;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>Shared modal dialog helpers with the Turbo Vision look.</summary>
internal static class Dialogs
{
    public static void Info(string title, string message) =>
        MessageBox.Query(title, "\n" + message + "\n", "OK");

    public static void Error(string title, string message) =>
        MessageBox.ErrorQuery(title, "\n" + message + "\n", "OK");

    /// <summary>Yes/No confirmation; returns true on Yes (button index 0).</summary>
    public static bool Confirm(string title, string message, string yes = "Yes", string no = "No") =>
        MessageBox.Query(title, "\n" + message + "\n", yes, no) == 0;

    /// <summary>Three-way pending-changes prompt: 0=Save, 1=Discard, 2=Cancel.</summary>
    public static int PendingChanges(string title) =>
        MessageBox.Query(title, "\nThis document has unsaved changes.\n", "Save", "Discard", "Cancel");

    /// <summary>Single-line text prompt; returns the entered text, or null on cancel.</summary>
    public static string Prompt(string title, string label, string initial = "", int width = 60)
    {
        var dlg = new Dialog(title, width, 9) { ColorScheme = TurboVisionTheme.Dialog };
        var lbl = new Label(label) { X = 1, Y = 1 };
        var field = new TextField(initial ?? "") { X = 1, Y = 3, Width = Dim.Fill(2) };
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

    /// <summary>
    /// Runs a blocking operation off the UI thread so the status bar and process
    /// monitor stay live, then invokes <paramref name="onDone"/> back on the UI
    /// thread with the result. Terminal.Gui is single-threaded, so all UI must
    /// happen in the callback.
    /// </summary>
    public static void RunBackground<T>(Func<T> work, Action<T> onDone)
    {
        System.Threading.Tasks.Task.Run(() =>
        {
            T result;
            try
            {
                result = work();
            }
            catch (Exception e)
            {
                Application.MainLoop.Invoke(() => Error("Operation failed", e.Message));
                return;
            }
            Application.MainLoop.Invoke(() => onDone(result));
        });
    }
}
