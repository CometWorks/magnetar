using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NStack;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Logs;
using Magnetar.ConfigTerminal.Model;
// Terminal.Gui v1's redraw hooks take the legacy System.Rune, not System.Text.Rune.
using Rune = System.Rune;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Read-only log viewer over the game and Magnetar log files, with a follow
/// mode (tail -f). Memory-bounded via <see cref="LogTailReader"/>.
/// </summary>
internal sealed class LogViewerView : Window
{
    private readonly LogCatalog catalog;
    private readonly ListView fileList;
    private readonly LogTextView text;
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

    // Last search term and its options, remembered across file switches so n/N keep
    // working and the Find dialog reopens with the previous choices.
    private string searchTerm = string.Empty;
    private bool searchMatchCase;
    private bool searchWholeWord;

    // True while the status line shows a transient message (a search result or a
    // highlight-navigation note) instead of the default key hints. Esc clears it.
    private bool transientStatus;

    // Set when a bottom-scroll was requested before the pane had a height (initial open);
    // applied on the first LayoutComplete. See ScrollToBottom / OnTextLayout.
    private bool bottomScrollPending;

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
        // Enter on a file moves into the text pane so it can be scrolled — the same
        // as the right arrow (which navigates focus to the view on the right).
        fileList.OpenSelectedItem += _ => text.SetFocus();

        text = new LogTextView
        {
            X = 22, Y = 1, Width = Dim.Fill(1), Height = Dim.Fill(2),
            ReadOnly = true, WordWrap = wrapEnabled, ColorScheme = TurboVisionTheme.Window,
        };

        // See OnViewerKey: the follow/refresh/wrap keys are handled on the focused child so
        // End reaches us before the pane consumes it (the list jumps to the last file, the
        // text pane to the end of the line). A ProcessKey override on this Window would not.
        fileList.KeyPress += OnViewerKey;
        text.KeyPress += OnViewerKey;
        // The first Render (from this ctor) scrolls to the tail before the pane is laid
        // out, so re-apply it once the pane has a real height (see ScrollToBottom).
        text.LayoutComplete += OnTextLayout;

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
        // The model was replaced, so any in-progress incremental find is anchored to
        // a stale position — reset it so the next search re-anchors to the cursor.
        text.FindTextChanged();
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
    //
    // Adjust() needs the pane's real height to place the last line at the bottom of the
    // viewport. The first Render runs from the constructor — before the view is added to
    // the tree and laid out — when that height is still 0, which would pin only the last
    // line to the top. Defer to the first LayoutComplete in that case (see OnTextLayout).
    private void ScrollToBottom()
    {
        if (text.Frame.Height <= 0)
        {
            bottomScrollPending = true;
            return;
        }
        text.CursorPosition = new Point(0, Math.Max(text.Lines - 1, 0));
    }

    // Flush a bottom-scroll deferred from before the pane had a height. Only the initial
    // (pending) scroll is honoured — later layouts (e.g. a terminal resize after the user
    // scrolled up) must not yank the view back to the tail.
    private void OnTextLayout(View.LayoutEventArgs _)
    {
        if (bottomScrollPending && text.Frame.Height > 0)
        {
            bottomScrollPending = false;
            ScrollToBottom();
        }
    }

    // Jump to the very top of the log (first line, first column).
    private void ScrollToTop()
    {
        bottomScrollPending = false; // an explicit Home cancels a not-yet-applied bottom scroll
        text.CursorPosition = Point.Empty;
    }

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
            case (Key)'/':
                PromptSearch();
                args.Handled = true;
                break;
            case (Key)'n':
            case Key.F3:
                FindNext(forward: true);
                args.Handled = true;
                break;
            case (Key)'N':
            case Key.F3 | Key.ShiftMask:
                FindNext(forward: false);
                args.Handled = true;
                break;
            case (Key)']':
                GoToHighlight(forward: true);
                args.Handled = true;
                break;
            case (Key)'[':
                GoToHighlight(forward: false);
                args.Handled = true;
                break;
            case Key.Esc:
                // Cancel an active search, or clear a transient status left by highlight
                // navigation ([ ]), so the default hint line returns. Only consume Esc
                // when there is something to clear, so it can still bubble otherwise.
                if (searchTerm.Length > 0)
                {
                    ClearSearch();
                    args.Handled = true;
                }
                else if (transientStatus)
                {
                    UpdateStatus();
                    args.Handled = true;
                }
                break;
        }
    }

    // Ask for a search term and its options, then jump to the first match. A blank
    // term clears the search. Following is stopped first so the next poll doesn't yank
    // the view off the match and back to the tail.
    private void PromptSearch()
    {
        StopFollow();

        var dlg = new Dialog("Find", 60, 11) { ColorScheme = TurboVisionTheme.Dialog };
        var field = new TextField(searchTerm) { X = 1, Y = 2, Width = Dim.Fill(2) };
        var caseBox = new CheckBox("Case sensitive") { X = 1, Y = 4, Checked = searchMatchCase };
        var wordBox = new CheckBox("Whole words only") { X = 1, Y = 5, Checked = searchWholeWord };

        bool accepted = false;
        var ok = new Button("OK", is_default: true);
        ok.Clicked += () => { accepted = true; Application.RequestStop(dlg); };
        var cancel = new Button("Cancel");
        cancel.Clicked += () => Application.RequestStop(dlg);

        dlg.Add(new Label("Search text:") { X = 1, Y = 1 }, field, caseBox, wordBox);
        dlg.AddButton(ok);
        dlg.AddButton(cancel);
        field.SetFocus();
        Application.Run(dlg);

        if (!accepted)
            return; // cancelled — keep the previous term and options

        searchMatchCase = caseBox.Checked;
        searchWholeWord = wordBox.Checked;
        searchTerm = field.Text.ToString();
        if (searchTerm.Length == 0)
        {
            ClearSearch();
            return;
        }
        // A new term searches the whole window from the top: anchor the cursor there
        // and reset the incremental-find state, so the first match is the topmost one
        // rather than only whatever happens to be below the tail we opened at.
        text.CursorPosition = Point.Empty;
        text.FindTextChanged();
        FindNext(forward: true);
    }

    // Cancel the active search: drop the match selection and restore the default hint
    // line so the base key reference is visible again.
    private void ClearSearch()
    {
        searchTerm = string.Empty;
        text.Selecting = false;
        text.SetNeedsDisplay();
        UpdateStatus();
    }

    // Move to the next (or previous) match of the current term, selecting and
    // scrolling to it. With no term yet, prompt for one first.
    private void FindNext(bool forward)
    {
        if (searchTerm.Length == 0)
        {
            PromptSearch();
            return;
        }

        StopFollow();
        text.SetFocus();

        ustring needle = ustring.Make(searchTerm);
        bool found = Find(needle, forward, out bool wrapped);
        string opts = SearchOptionSuffix();

        if (!found)
            statusLabel.Text = $"Not found: \"{searchTerm}\"{opts}";
        else if (wrapped)
            statusLabel.Text = $"Wrapped · \"{searchTerm}\"{opts} · n/N: next/prev · Esc: clear";
        else
            statusLabel.Text = $"Found \"{searchTerm}\"{opts} · n/N: next/prev · Esc: clear";
        transientStatus = true;
    }

    // " [case, word]" for whatever search options are on, else empty — appended to the
    // find status so the active matching mode is visible.
    private string SearchOptionSuffix()
    {
        var on = new List<string>();
        if (searchMatchCase) on.Add("case");
        if (searchWholeWord) on.Add("word");
        return on.Count > 0 ? $" [{string.Join(", ", on)}]" : string.Empty;
    }

    // Search once from the current position, then — on a miss — wrap by re-anchoring
    // to the far end and searching again. Terminal.Gui's TextView only wraps once its
    // find anchor has advanced past the start point, so a first search from the tail
    // never sees matches above it; the explicit retry gives reliable wrap-around from
    // anywhere. Returns whether a match was found, and whether it came from the wrap.
    private bool Find(ustring needle, bool forward, out bool wrapped)
    {
        bool found = forward
            ? text.FindNextText(needle, out _, searchMatchCase, searchWholeWord)
            : text.FindPreviousText(needle, out _, searchMatchCase, searchWholeWord);
        if (found)
        {
            wrapped = false;
            return true;
        }

        text.CursorPosition = forward
            ? Point.Empty
            : new Point(0, Math.Max(text.Lines - 1, 0));
        text.FindTextChanged();
        found = forward
            ? text.FindNextText(needle, out _, searchMatchCase, searchWholeWord)
            : text.FindPreviousText(needle, out _, searchMatchCase, searchWholeWord);
        wrapped = found;
        return found;
    }

    // Jump to the next/previous line the viewer highlights (a "Game ready" or
    // "Exception" line), wrapping around the loaded window; a short status names the
    // kind. Navigation is over the reader's logical lines, which map 1:1 to the text
    // pane's rows while word-wrap is off (the default); with wrap on the jump is
    // approximate. Independent of the text search, so it never disturbs the term.
    private void GoToHighlight(bool forward)
    {
        if (reader == null)
            return;

        var lines = reader.Lines;
        int count = lines.Count;
        int current = Math.Min(text.CurrentRow, count - 1);

        int target = -1;
        bool wrapped = false;
        if (forward)
        {
            target = NextHighlight(lines, current + 1, count);
            if (target < 0 && current >= 0)
            {
                target = NextHighlight(lines, 0, current + 1);
                wrapped = target >= 0;
            }
        }
        else
        {
            target = PrevHighlight(lines, current - 1);
            if (target < 0)
            {
                target = PrevHighlight(lines, count - 1);
                wrapped = target >= 0;
            }
        }

        if (target < 0)
        {
            statusLabel.Text = "No highlighted lines in view";
            transientStatus = true;
            return;
        }

        StopFollow();
        text.SetFocus();
        text.Selecting = false;
        text.CursorPosition = new Point(0, target);

        string what = LogHighlight.Classify(lines[target]) == LogHighlightKind.Ready
            ? "Game ready" : "Exception";
        statusLabel.Text = wrapped
            ? $"Wrapped to {what} line · [ ]: prev/next · Esc: clear"
            : $"{what} line · [ ]: prev/next · Esc: clear";
        transientStatus = true;
    }

    // First highlighted line in [from, to), or -1.
    private static int NextHighlight(IReadOnlyList<string> lines, int from, int to)
    {
        for (int i = Math.Max(from, 0); i < to; i++)
            if (LogHighlight.Classify(lines[i]) != LogHighlightKind.None)
                return i;
        return -1;
    }

    // Nearest highlighted line at or below index `from`, scanning upward, or -1.
    private static int PrevHighlight(IReadOnlyList<string> lines, int from)
    {
        for (int i = Math.Min(from, lines.Count - 1); i >= 0; i--)
            if (LogHighlight.Classify(lines[i]) != LogHighlightKind.None)
                return i;
        return -1;
    }

    private void ToggleWrap()
    {
        wrapEnabled = !wrapEnabled;
        text.WordWrap = wrapEnabled;
        text.SetNeedsDisplay();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        transientStatus = false;
        statusLabel.Text = following
            ? "FOLLOWING (End to stop) · Home: top · /: find · [ ]: highlight"
            : $"/: find · [ ]: highlight · End: follow · Home: top · R: refresh · W: wrap [{(wrapEnabled ? "on" : "off")}]";
    }

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

    /// <summary>
    /// Read-only <see cref="TextView"/> that tints whole lines matching the
    /// <see cref="LogHighlight"/> markers — "Game ready" (a world came up) and
    /// "Exception" (a fault) — so they stand out while scanning. The v1 redraw loop
    /// asks per rune which colour to use; because the pane is read-only that path is
    /// <see cref="SetReadOnlyColor"/> (search matches keep their selection colour,
    /// which the loop resolves before either of these). <see cref="SetNormalColor"/>
    /// is overridden too so highlighting survives if the pane ever becomes editable.
    /// </summary>
    private sealed class LogTextView : TextView
    {
        // Vivid, high-contrast within the CGA-16 palette so a matched line reads as a
        // deliberate marker rather than an accident of the theme.
        private static readonly Terminal.Gui.Attribute ReadyColor =
            Terminal.Gui.Attribute.Make(Color.Black, Color.Green);
        private static readonly Terminal.Gui.Attribute ExceptionColor =
            Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Red);

        // The redraw loop reuses one List<Rune> for every rune of a row, so classify
        // once per row and cache by reference — turning O(runes) work into O(rows).
        private List<Rune> cachedLine;
        private LogHighlightKind cachedKind;

        protected override void SetReadOnlyColor(List<Rune> line, int idx)
        {
            if (!TrySetHighlightColor(line))
                base.SetReadOnlyColor(line, idx);
        }

        protected override void SetNormalColor(List<Rune> line, int idx)
        {
            if (!TrySetHighlightColor(line))
                base.SetNormalColor(line, idx);
        }

        private bool TrySetHighlightColor(List<Rune> line)
        {
            switch (Classify(line))
            {
                case LogHighlightKind.Ready:
                    Application.Driver.SetAttribute(ReadyColor);
                    return true;
                case LogHighlightKind.Exception:
                    Application.Driver.SetAttribute(ExceptionColor);
                    return true;
                default:
                    return false;
            }
        }

        private LogHighlightKind Classify(List<Rune> line)
        {
            if (ReferenceEquals(line, cachedLine))
                return cachedKind;
            cachedLine = line;

            // Markers are ASCII, so a lossy rune->char fold is enough to match them.
            var sb = new StringBuilder(line.Count);
            foreach (Rune r in line)
                sb.Append((char)(uint)r);
            cachedKind = LogHighlight.Classify(sb.ToString());
            return cachedKind;
        }
    }
}
