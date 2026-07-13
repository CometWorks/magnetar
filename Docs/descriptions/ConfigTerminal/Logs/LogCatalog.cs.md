# ConfigTerminal/Logs/LogCatalog.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Logs` · **Kind:** enum, sealed class · **Lines:** 143

## Summary
Pure-filesystem discovery of the log files for the bound instance (§2.9): the DS game logs (`SpaceEngineersDedicated*.log`) in the DS data dir and Magnetar's `info_*.log` files in the config dir. It builds a list of `LogFileInfo` records sorted newest-first and marks the active file of each group — the newest game log, and the Magnetar file named by the `info.current` marker. No Terminal.Gui dependency; all IO is wrapped so missing/locked files or directories degrade to empty results.

## Types
### LogGroup — enum, internal
Which log stream a file belongs to: `Game` or `Magnetar`.

### LogFileInfo — sealed class, internal
One discovered log file with the metadata the viewer sorts and marks by.

- **Fields:** `Path`, `Group` (`LogGroup`), `LastWrite` (`DateTime`, UTC), `Size` (long), `IsActive` (bool — `info.current` match, or newest game log).
- **Properties:**
  - `Name` — bare file name (`Path.GetFileName`).
  - `DisplayName` — compact selector label: if the name embeds a `yyyyMMdd_HHmmssfff` timestamp (matched by regex `(\d{8}_\d{6})\d*`), shows only `yyyyMMdd_HHmmss` (prefix, extension, and milliseconds dropped); otherwise the bare name.

### LogCatalog — sealed class, internal
Discovers and marks the game and Magnetar log files for the bound `InstanceBinding`; `Scan` is safe to call repeatedly for follow/refresh.

- **Fields:** `binding` (`InstanceBinding`), `files` (`List<LogFileInfo>`).
- **Properties:**
  - `Files` (`IReadOnlyList<LogFileInfo>`) — all discovered files, newest first.
  - `ActiveGameLog` — the active game log (where "Game ready" appears), i.e. the game file flagged `IsActive`, falling back to the first game file, or null if none exist.
- **Methods:**
  - `Scan()` — clears `files`, runs `ScanGameLogs` and `ScanMagnetarLogs`, then sorts by `LastWrite` descending.
  - `ScanGameLogs()` — discovers `SpaceEngineersDedicated*.log` under `binding?.DataDir`; since the DS overwrites its log per start, marks the newest by `LastWrite` as active.
  - `ScanMagnetarLogs()` — discovers `info_*.log` under `binding?.MagnetarConfigDir` and marks the one whose name case-insensitively equals the `info.current` content as active.
  - `Discover(dir, pattern, group)` (static) — enumerates matching files, reading `LastWriteTimeUtc`/`Length` per file into `LogFileInfo`; returns empty and swallows `IOException`/`UnauthorizedAccessException` for a missing/inaccessible dir or file.
  - `ReadInfoCurrent(dir)` (static) — reads and trims the `info.current` marker file in `dir`, or null if absent/unreadable.

## Cross-references
- **Uses:** `InstanceBinding` (`ConfigTerminal/Model/`, for `DataDir`/`MagnetarConfigDir`); `System.IO` (`Directory`, `FileInfo`, `File`, `Path`); `System.Text.RegularExpressions.Regex`; `System.Linq`.
- **Used by:** [LogViewerView.cs](../Ui/LogViewerView.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md)
