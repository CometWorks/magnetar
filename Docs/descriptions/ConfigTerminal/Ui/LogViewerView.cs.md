# ConfigTerminal/Ui/LogViewerView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 588

## Summary
Read-only log viewer over the game and Magnetar log files, with a `tail -f` follow mode, optional word-wrap, incremental text search, and keyword highlighting. A left file list and a right text pane are backed by a memory-bounded `LogTailReader`; follow updates append only newly-read lines (falling back to a full rebuild on a structural change) and a 700 ms `MainLoop` timeout drives the polling. Lines matching the `LogHighlight` markers ("Game ready", "Exception") are colour-tinted, `/` searches (with case / whole-word options), `[` / `]` step between highlighted lines, and `Esc` clears either. The follow timer is stopped on `Dispose` so a closed panel does not keep polling.

## Types
### LogViewerView — sealed class, internal (`Window`)
The Logs panel.

- **Fields:**
  - `catalog` (`LogCatalog`), `fileList` (`ListView`), `text` (`LogTextView`), `statusLabel` (`Label`), `reader` (`LogTailReader`).
  - `following` (bool), `followToken` (object) — follow-mode state and the registered timeout handle.
  - `rendered` (`StringBuilder`), `renderedCount` (int) — mirror of what the text pane shows, so a follow poll appends only the new lines.
  - `wrapEnabled` (static bool) — word-wrap toggle remembered for the process lifetime (off by default).
  - `searchTerm` (string), `searchMatchCase` / `searchWholeWord` (bool) — last search term and its options, remembered across file switches and Find-dialog invocations.
  - `bottomScrollPending` (bool) — set when a scroll-to-tail was requested before the pane had a height; applied on the first `LayoutComplete`.
  - `transientStatus` (bool) — true while the status line shows a search/highlight message rather than the default key hints (so `Esc` knows to restore them).
- **Methods:**
  - `LogViewerView(InstanceBinding)` — scans the catalog, builds the file list (selection loads, Enter moves focus to the text pane), the read-only `LogTextView`, and the status line; handles viewer keys on the focused child's `KeyPress` (so End reaches it before the pane consumes it) and subscribes `OnTextLayout` to `LayoutComplete`.
  - `Populate()` / `LoadSelected()` / `Render()` / `RenderFollow()` — fill the file list (defaulting to the active game log), open a fresh `LogTailReader` on the selected file, and rebuild/append the text pane; `Render` resets the incremental-find anchor after replacing the model.
  - `ScrollToBottom()` / `OnTextLayout(View.LayoutEventArgs)` / `ScrollToTop()` — scroll to the tail (deferred to `LayoutComplete` when the pane has no height yet, so the whole last screen shows rather than only the last line) or jump to the top.
  - `OnViewerKey(KeyEventEventArgs)` — End follow, Home top, R refresh, W wrap, `/` find, `n`/`N` (and F3/Shift-F3) next/previous match, `[`/`]` previous/next highlighted line, Esc clear.
  - `PromptSearch()` — modal Find dialog: a term field plus **Case sensitive** and **Whole words only** checkboxes; a blank term clears the search, otherwise anchors at the top and searches.
  - `ClearSearch()` — drops the match selection, forgets the term, and restores the default hints.
  - `FindNext(bool)` / `SearchOptionSuffix()` / `Find(ustring, bool, out bool)` — run a match forward/backward via `TextView.FindNextText`/`FindPreviousText` (passing the case/whole-word options), retrying from the far end so wrap-around is reliable, and report the result (with active options) in the status.
  - `GoToHighlight(bool)` / `NextHighlight` / `PrevHighlight` — jump to the next/previous line `LogHighlight.Classify` marks, wrapping around; operates on the reader's logical lines (1:1 with pane rows while word-wrap is off) and is independent of the text search.
  - `ToggleWrap()`, `UpdateStatus()`, `ToggleFollow()`, `StopFollow()`, `Dispose(bool)` — wrap toggle, status text (clears `transientStatus`), follow start/stop (700 ms poll), and follow-timer teardown.

### LogTextView — sealed class, private, nested (`TextView`)
Read-only text pane that tints whole lines matching the `LogHighlight` markers. Overrides the per-rune redraw hooks `SetReadOnlyColor` / `SetNormalColor` (the read-only pane uses the former; both delegate to `TrySetHighlightColor`) to paint "Game ready" lines black-on-green and "Exception" lines yellow-on-red. `Classify` builds the line string once and caches it by reference, since the redraw loop reuses one `List<Rune>` per row. Because the redraw resolves the selection (search match) colour before these hooks, a match stays visible on a highlighted line.

## Cross-references
- **Uses:** Terminal.Gui `Window`/`ListView`/`TextView`/`Label`/`Dialog`/`CheckBox`/`Button`/`Attribute`, `Application.MainLoop` timeouts, `NStack.ustring`; `LogCatalog`/`LogFileInfo`/`LogTailReader`/`LogHighlight` (`ConfigTerminal/Logs/`); `InstanceBinding` (`ConfigTerminal/Model/`); `TurboVisionTheme`, `Dialogs` (this module); `System.Text.StringBuilder`.
- **Used by:** [AppShell.cs](AppShell.cs.md), [UiSmokeTests.cs](../../ConfigTerminalTests/UiSmokeTests.cs.md)
