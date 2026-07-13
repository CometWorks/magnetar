using System;
using System.Linq;
using System.Text;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Logs;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Read-only log viewer over the game and Magnetar log files, with a follow
/// mode (tail -f). Memory-bounded via <see cref="LogTailReader"/>.
/// </summary>
internal sealed class LogViewerView : Window
{
    private readonly LogCatalog catalog;
    private readonly ListView fileList;
    private readonly TextView text;
    private readonly Label statusLabel;
    private LogTailReader reader;
    private bool following;
    private object followToken;

    // Mirror of what the text pane currently shows, so a follow poll appends only
    // the newly-read lines instead of re-joining and re-parsing the whole window.
    private readonly StringBuilder rendered = new();
    private int renderedCount;

    // Remembered for the lifetime of the configurator process (off by default).
    private static bool wrapEnabled;

    public LogViewerView(InstanceBinding binding) : base("Logs")
    {
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        catalog = new LogCatalog(binding);
        catalog.Scan();

        fileList = new ListView(Array.Empty<string>())
        {
            X = 1, Y = 1, Width = 20, Height = Dim.Fill(2), ColorScheme = TurboVisionTheme.Window,
        };
        fileList.SelectedItemChanged += _ => LoadSelected();

        text = new TextView
        {
            X = 22, Y = 1, Width = Dim.Fill(1), Height = Dim.Fill(2),
            ReadOnly = true, WordWrap = wrapEnabled, ColorScheme = TurboVisionTheme.Window,
        };

        // See OnViewerKey: the follow/refresh/wrap keys are handled on the focused child so
        // End reaches us before the pane consumes it (the list jumps to the last file, the
        // text pane to the end of the line). A ProcessKey override on this Window would not.
        fileList.KeyPress += OnViewerKey;
        text.KeyPress += OnViewerKey;

        statusLabel = new Label
        { X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = TurboVisionTheme.Window };

        Add(new Label("Log files:") { X = 1, Y = 0 }, fileList, text, statusLabel);
        UpdateStatus();
        Populate();
    }

    private void Populate()
    {
        var items = catalog.Files
            .Select(f => $"{(f.IsActive ? "*" : " ")}[{f.Group.ToString()[0]}] {f.DisplayName}")
            .ToList();
        fileList.SetSource(items);
        if (catalog.Files.Count > 0)
        {
            // Default to the active game log (where "Game ready" appears).
            LogFileInfo active = catalog.ActiveGameLog ?? catalog.Files[0];
            int idx = catalog.Files.ToList().FindIndex(f => f.Path == active.Path);
            fileList.SelectedItem = Math.Max(0, idx);
            LoadSelected();
        }
    }

    private void LoadSelected()
    {
        int i = fileList.SelectedItem;
        if (i < 0 || i >= catalog.Files.Count)
            return;
        reader = new LogTailReader(catalog.Files[i].Path);
        reader.Load();
        Render();
    }

    // Full rebuild of the text pane from the whole window. Used on file switch and
    // whenever the window changed structurally (reload / front-trim) so an append
    // would be wrong.
    private void Render()
    {
        rendered.Clear();
        var lines = reader.Lines;
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0) rendered.Append('\n');
            rendered.Append(lines[i]);
        }
        renderedCount = lines.Count;
        text.Text = rendered.ToString();
        ScrollToBottom();
    }

    // Follow-tick update: append only the lines read since the last render, unless
    // the reader signals a structural change (then fall back to a full rebuild).
    // Avoids re-joining the entire (up to 20k-line) window on every 700 ms poll.
    private void RenderFollow()
    {
        var lines = reader.Lines;
        if (reader.StructuralChange || lines.Count < renderedCount)
        {
            Render();
            return;
        }
        if (lines.Count == renderedCount)
            return;
        for (int i = renderedCount; i < lines.Count; i++)
        {
            if (rendered.Length > 0) rendered.Append('\n');
            rendered.Append(lines[i]);
        }
        renderedCount = lines.Count;
        text.Text = rendered.ToString();
        ScrollToBottom();
    }

    // Scroll to the bottom without moving horizontally. TextView.MoveEnd() only moves the
    // cursor, not the scroll offset (topRow) — and the Text setter above just reset topRow
    // to 0, so on a follow poll the view would stay pinned to the top. It would also jump to
    // the end of the last line, scrolling right. Assigning CursorPosition at column 0 of the
    // last row runs TextView.Adjust(), which scrolls topRow down and leftColumn back to 0.
    private void ScrollToBottom() =>
        text.CursorPosition = new Point(0, Math.Max(text.Lines - 1, 0));

    // Jump to the very top of the log (first line, first column).
    private void ScrollToTop() =>
        text.CursorPosition = Point.Empty;

    // Terminal.Gui dispatches a key to the focused child (via ProcessHotKey) before the
    // containing Window's ProcessKey runs, so an End handler there never fires — the ListView
    // consumes End (jump to last file) and the TextView consumes it (end of line). Handling the
    // child's KeyPress event runs first, so End reliably drives follow whichever pane has focus.
    private void OnViewerKey(KeyEventEventArgs args)
    {
        switch (args.KeyEvent.Key)
        {
            case Key.End:
                ToggleFollow();
                args.Handled = true;
                break;
            case Key.Home:
                // Jump to the top of the log. Stop following first, otherwise the
                // next poll would snap the view straight back to the bottom.
                StopFollow();
                ScrollToTop();
                args.Handled = true;
                break;
            case (Key)'r':
            case (Key)'R':
                LoadSelected();
                args.Handled = true;
                break;
            case (Key)'w':
            case (Key)'W':
                ToggleWrap();
                args.Handled = true;
                break;
        }
    }

    private void ToggleWrap()
    {
        wrapEnabled = !wrapEnabled;
        text.WordWrap = wrapEnabled;
        text.SetNeedsDisplay();
        UpdateStatus();
    }

    private void UpdateStatus() =>
        statusLabel.Text = following
            ? "FOLLOWING (End to stop) · Home: top"
            : $"End: follow · Home: top · R: refresh · W: wrap [{(wrapEnabled ? "on" : "off")}]";

    private void ToggleFollow()
    {
        if (following)
        {
            StopFollow();
            return;
        }

        following = true;
        UpdateStatus();
        // Refresh from disk once (like pressing R) so following re-reads the
        // current window and snaps to the very end immediately — picking up
        // anything written since the file was last read — then keep polling.
        LoadSelected();
        followToken = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(700), _ =>
        {
            if (!following || reader == null)
                return false;
            if (reader.Poll())
                RenderFollow();
            return true;
        });
    }

    private void StopFollow()
    {
        if (!following)
            return;
        following = false;
        UpdateStatus();
        if (followToken != null)
        {
            Application.MainLoop.RemoveTimeout(followToken);
            followToken = null;
        }
    }

    // The follow timer holds this view (and its LogTailReader) alive and keeps
    // polling the file. AppShell disposes the panel on every switch, so stop the
    // timer here or it would leak — polling disk and rendering into a dead view for
    // the rest of the session, once per Logs visit left in follow mode.
    protected override void Dispose(bool disposing)
    {
        if (disposing && followToken != null)
        {
            following = false;
            Application.MainLoop.RemoveTimeout(followToken);
            followToken = null;
        }
        base.Dispose(disposing);
    }
}
