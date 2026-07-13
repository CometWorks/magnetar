# ConfigTerminal/Logs/LogTailReader.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Logs` · **Kind:** sealed class · **Lines:** 150

## Summary
Memory-bounded tail reader over a single log file: it holds only the last window of bytes (default 256 KB) so it stays cheap even on multi-GB logs, and follows appended bytes `tail -f`-style. `Load` establishes the window at EOF; `Poll` reads only the bytes appended since the last read, or reloads the window when the file was truncated or rotated. Reads open the file with shared read/write access so the DS can keep writing, and IO errors never throw (they read as "no change").

## Types
### LogTailReader — sealed class, internal
Windowed line buffer for one log file, with incremental follow and a structural-change signal so a mirroring viewer knows when it must rebuild instead of append.

- **Fields:**
  - `DefaultWindowBytes` (256 KB) / `MaxLines` (20000) — window byte budget and a hard line cap so follow can never grow unbounded.
  - `path`, `windowBytes` (falls back to default when non-positive) — target file and window size.
  - `lines` (`List<string>`) — the parsed window, oldest first.
  - `position` (long) — byte offset of the next unread byte.
  - `pendingPartial` (string) — trailing line with no newline yet, carried across polls.
  - `structuralChange` (bool) — set when the last `Poll` reloaded or dropped front lines.
- **Properties:**
  - `Lines` (`IReadOnlyList<string>`) — the lines currently in the window, oldest first.
  - `StructuralChange` (bool) — true when the most recent `Poll` did not simply append (reloaded on truncation/rotation, or dropped lines off the front to stay under `MaxLines`); a follow viewer must do a full rebuild rather than an append-only update.
- **Methods:**
  - `Load()` — clears state and (re)reads the tail window: opens shared, seeks to `max(0, length - windowBytes)`, reads to EOF, and sets `position` to the length. If it started mid-file (`start > 0`) it discards the first, partial line. Swallows `IOException`/`UnauthorizedAccessException`.
  - `Poll()` — reads bytes appended since the last read; returns true when the window changed. If the length shrank below `position` it treats the file as truncated/replaced, calls `Load()` and sets `structuralChange`; if unchanged returns false; otherwise reads the new tail and appends. Resets `structuralChange` at entry and returns false (no throw) on IO errors.
  - `Open()` — opens the file `FileMode.Open`/`FileAccess.Read`/`FileShare.ReadWrite`.
  - `ReadFully(fs, buffer)` (static) — loops `Read` until the buffer is full or EOF, returning the byte count.
  - `Decode(buffer, count)` (static) — decodes as UTF-8 with a non-throwing, non-BOM `UTF8Encoding` so a split multibyte char at a window edge is replaced rather than thrown.
  - `AppendText(text)` — prepends `pendingPartial`, normalizes `\r\n`/`\r` to `\n`, splits into `lines`, keeps the trailing partial line, and when `lines` exceeds `MaxLines` trims the front and sets `structuralChange`.

## Cross-references
- **Uses:** `System.IO` (`FileStream`, `SeekOrigin`, `IOException`), `System.Text.UTF8Encoding`.
- **Used by:** [LogViewerView.cs](../Ui/LogViewerView.cs.md)
