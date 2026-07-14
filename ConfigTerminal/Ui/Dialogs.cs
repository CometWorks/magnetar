using System;
using System.Linq;
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

    // --- centered-question + left-aligned-details dialogs ---
    //
    // MessageBox centers every line, which mangles bulleted lists (each bullet
    // floats to its own centre). These helpers keep the question centered but
    // render the detail block — bullet lists, key tables — left-aligned. Never
    // center a bulleted list.

    /// <summary>Info dialog: centered <paramref name="question"/> over a left-aligned detail block.</summary>
    public static void InfoDetails(string title, string question, string details) =>
        QueryDetails(title, question, details, error: false, "OK");

    /// <summary>Error dialog: centered <paramref name="question"/> over a left-aligned detail block.</summary>
    public static void ErrorDetails(string title, string question, string details) =>
        QueryDetails(title, question, details, error: true, "OK");

    /// <summary>Yes/No confirmation with a left-aligned detail block; true on Yes (button index 0).</summary>
    public static bool ConfirmDetails(string title, string question, string details, string yes = "Yes", string no = "No") =>
        QueryDetails(title, question, details, error: false, yes, no) == 0;

    /// <summary>
    /// Destructive confirmation rendered in the error theme. The safe option is
    /// listed first so it is the default (Enter), the focused button, and the Esc
    /// fallback — only an explicit click on <paramref name="confirmLabel"/> returns
    /// true. Use for irreversible actions (deletes) so a stray Enter/Esc is safe.
    /// </summary>
    public static bool ConfirmDestructive(string title, string question, string details, string confirmLabel, string cancelLabel = "No") =>
        QueryDetailsCore(title, question, details, error: true, defaultButton: 0, escButton: 0,
            new[] { cancelLabel, confirmLabel }) == 1;

    /// <summary>
    /// Modal with a centered <paramref name="question"/> header sitting over a
    /// left-aligned <paramref name="details"/> block, then <paramref name="buttons"/>
    /// along the bottom (first is the default). Returns the clicked button index,
    /// or the last index on Esc. Pass a null/empty question or details to omit it.
    /// </summary>
    public static int QueryDetails(string title, string question, string details, bool error, params string[] buttons) =>
        QueryDetailsCore(title, question, details, error, defaultButton: 0, escButton: buttons.Length - 1, buttons);

    /// <summary>
    /// Core of <see cref="QueryDetails"/>. <paramref name="defaultButton"/> is the
    /// Enter/focused button; <paramref name="escButton"/> is the value returned when
    /// the dialog is dismissed without a click (Esc). They differ for a plain
    /// confirm (Enter=Yes, Esc=No) but coincide for a destructive confirm so both
    /// resolve to the safe option.
    /// </summary>
    private static int QueryDetailsCore(string title, string question, string details, bool error, int defaultButton, int escButton, string[] buttons)
    {
        int qLines = string.IsNullOrEmpty(question) ? 0 : question.Split('\n').Length;
        int dLines = string.IsNullOrEmpty(details) ? 0 : details.Split('\n').Length;
        int gap = qLines > 0 && dLines > 0 ? 1 : 0;

        int Longest(string s) => string.IsNullOrEmpty(s) ? 0 : s.Split('\n').Max(l => l.Length);
        int buttonsWidth = buttons.Sum(b => b.Length + 4) + Math.Max(0, buttons.Length - 1);
        int content = Math.Max(Math.Max(Longest(question), Longest(details)), Math.Max(buttonsWidth, title.Length));

        int width = Math.Clamp(content + 6, 44, Math.Max(44, Application.Driver.Cols - 6));
        int height = Math.Min(qLines + gap + dLines + 4, Math.Max(7, Application.Driver.Rows - 2));

        var dlg = new Dialog(title, width, height)
        {
            ColorScheme = error ? TurboVisionTheme.Error : TurboVisionTheme.Dialog,
        };

        if (qLines > 0)
            dlg.Add(new Label(question)
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = qLines,
                TextAlignment = TextAlignment.Centered,
                AutoSize = false,
            });

        if (dLines > 0)
            dlg.Add(new Label(details)
            {
                X = 1,
                Y = qLines + gap,
                Width = Dim.Fill(1),
                Height = dLines,
                TextAlignment = TextAlignment.Left,
                AutoSize = false,
            });

        int result = escButton;
        Button defaultBtn = null;
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            var button = new Button(buttons[i], is_default: i == defaultButton);
            button.Clicked += () => { result = idx; Application.RequestStop(dlg); };
            dlg.AddButton(button);
            if (i == defaultButton)
                defaultBtn = button;
        }
        defaultBtn?.SetFocus();

        Application.Run(dlg);
        return result;
    }

    /// <summary>Three-way pending-changes prompt: 0=Save, 1=Discard, 2=Cancel.</summary>
    public static int PendingChanges(string title) =>
        MessageBox.Query(title, "\nThis document has unsaved changes.\n", "Save", "Discard", "Cancel");

    /// <summary>
    /// Single-line text prompt; returns the entered text, or null on cancel.
    /// When <paramref name="validate"/> is given it is called on OK with the entered
    /// text; returning a non-null error message shows it and keeps the dialog open,
    /// so the caller only ever gets a value that passed validation (or null).
    /// </summary>
    public static string Prompt(string title, string label, string initial = "", int width = 60,
        Func<string, string> validate = null)
    {
        var dlg = new Dialog(title, width, 9) { ColorScheme = TurboVisionTheme.Dialog };
        var lbl = new Label(label) { X = 1, Y = 1 };
        var field = new TextField(initial ?? "") { X = 1, Y = 3, Width = Dim.Fill(2) };
        string result = null;
        var ok = new Button("OK", true);
        ok.Clicked += () =>
        {
            string text = field.Text.ToString();
            string error = validate?.Invoke(text);
            if (error != null) { Error("Invalid", error); return; }
            result = text;
            Application.RequestStop(dlg);
        };
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
