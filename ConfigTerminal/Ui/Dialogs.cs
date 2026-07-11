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
