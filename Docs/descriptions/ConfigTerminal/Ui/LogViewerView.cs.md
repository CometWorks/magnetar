# ConfigTerminal/Ui/LogViewerView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 243

## Summary
Read-only log viewer over the game and Magnetar log files, with a `tail -f` follow mode and optional word-wrap. A left file list and a right text pane are backed by a memory-bounded `LogTailReader`; follow updates append only newly-read lines (falling back to a full rebuild on a structural change) and a 700 ms `MainLoop` timeout drives the polling. The follow timer is stopped on `Dispose` so a closed panel does not keep polling.

## Types
### LogViewerView — sealed class, internal (`Window`)
The Logs panel.

- **Fields:**
  - `catalog` (`LogCatalog`), `fileList` (`ListView`), `text` (`TextView`), `statusLabel` (`Label`), `reader` (`LogTailReader`).
  - `following` (bool), `followToken` (object) — follow-mode state and the registered timeout handle.
  - `rendered` (`StringBuilder`), `renderedCount` (int) — mirror of what the text pane shows, so a follow poll appends only the new lines.
  - `wrapEnabled` (static bool) — word-wrap toggle remembered for the process lifetime (off by default).
- **Methods:**
  - `LogViewerView(InstanceBinding)` — scans the catalog, builds the file list (selection loads, Enter moves focus to the text pane), the read-only text pane, and the status line; handles viewer keys on the focused child's `KeyPress` (so End reaches it before the pane consumes it).
  - `Populate()` — fills the file list (active marker, group initial, display name) and defaults selection to the active game log.
  - `LoadSelected()` — opens a fresh `LogTailReader` on the selected file, loads it, and renders.
  - `Render()` — full rebuild of the text pane from the whole window; used on file switch and structural change.
  - `RenderFollow()` — appends only the lines read since the last render, falling back to `Render()` on `StructuralChange` or a shrunk window.
  - `ScrollToBottom()` / `ScrollToTop()` — set `CursorPosition` to scroll without moving horizontally (works around `TextView` topRow behavior).
  - `OnViewerKey(KeyEventEventArgs)` — End toggles follow, Home stops follow and jumps to top, R refreshes, W toggles wrap.
  - `ToggleWrap()`, `UpdateStatus()`, `ToggleFollow()`, `StopFollow()` — wrap toggle, status text, and follow start/stop (start re-reads and registers the 700 ms poll that calls `reader.Poll()` then `RenderFollow`).
  - `Dispose(bool)` — removes the follow timeout so the disposed panel stops polling.

## Cross-references
- **Uses:** Terminal.Gui `Window`/`ListView`/`TextView`/`Label`, `Application.MainLoop` timeouts; `LogCatalog`/`LogFileInfo`/`LogTailReader` (`ConfigTerminal/Logs/`); `InstanceBinding` (`ConfigTerminal/Model/`); `TurboVisionTheme` (this module); `System.Text.StringBuilder`.
- **Used by:** [AppShell.cs](AppShell.cs.md)
