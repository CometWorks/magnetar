# Module: ConfigTerminal.Logs

**Project:** `ConfigTerminal` · **Files:** 4 · **Source lines:** 374

## Purpose

Discovers and tail-reads the log files behind the ConfigTerminal TUI log viewer, and classifies lines for highlighting. It enumerates the DS game logs (SpaceEngineersDedicated*.log) and Magnetar's info_*.log files for the bound instance, provides a memory-bounded windowed reader that follows appended bytes tail -f-style while surviving truncation and rotation, and flags 'Game ready' / 'Exception' lines for the viewer to colour and navigate. All IO is failure-tolerant (missing or locked files degrade gracefully) and there is no Terminal.Gui dependency.

## Role in Magnetar

This is the file-discovery, streaming, and line-classification layer under the ConfigTerminal log-viewing UI. LogCatalog resolves paths from an InstanceBinding (ConfigTerminal.Model) and marks the active log of each group; LogTailReader feeds the viewer with a bounded, incrementally-updated window plus a structural-change signal so the UI knows when to rebuild versus append; LogHighlight classifies a line as a 'Game ready' readiness marker or an 'Exception' fault, driving both the colour highlighting and the [ / ] highlighted-line navigation in the viewer. ReadinessDetector scans for the same 'Game ready' marker for staged world-creation but is currently unused, retained with no callers.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `LogTailReader` | class | [`ConfigTerminal/Logs/LogTailReader.cs`](../descriptions/ConfigTerminal/Logs/LogTailReader.cs.md) | Memory-bounded (256 KB window) tail reader that follows appended bytes and reloads on truncation/rotation. |
| `LogCatalog` | class | [`ConfigTerminal/Logs/LogCatalog.cs`](../descriptions/ConfigTerminal/Logs/LogCatalog.cs.md) | Filesystem discovery of game and Magnetar log files for the bound instance, marking each group's active file. |
| `LogFileInfo` | class | [`ConfigTerminal/Logs/LogCatalog.cs`](../descriptions/ConfigTerminal/Logs/LogCatalog.cs.md) | One discovered log file with path, group, last-write time, size, active flag, and a compact timestamped display name. |
| `LogGroup` | enum | [`ConfigTerminal/Logs/LogCatalog.cs`](../descriptions/ConfigTerminal/Logs/LogCatalog.cs.md) | Which log stream a file belongs to: Game or Magnetar. |
| `ReadinessDetector` | static class | [`ConfigTerminal/Logs/ReadinessDetector.cs`](../descriptions/ConfigTerminal/Logs/ReadinessDetector.cs.md) | Tail-scans the game log for the 'Game ready' marker; currently unused vestige of a prior plan. |
| `LogHighlight` | static class | [`ConfigTerminal/Logs/LogHighlight.cs`](../descriptions/ConfigTerminal/Logs/LogHighlight.cs.md) | Case-sensitive classifier that flags a log line as a 'Game ready' readiness marker or an 'Exception' fault (Exception wins); used for highlighting and [ / ] navigation. |
| `LogHighlightKind` | enum | [`ConfigTerminal/Logs/LogHighlight.cs`](../descriptions/ConfigTerminal/Logs/LogHighlight.cs.md) | How a line should be highlighted: None, Ready, or Exception. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`ConfigTerminal/Logs/LogCatalog.cs`](../descriptions/ConfigTerminal/Logs/LogCatalog.cs.md) | 143 | Pure-filesystem discovery of the log files for the bound instance (§2.9): the DS game logs (`SpaceEngineersDedicated*.log`) in the DS data dir and Magnetar's `info_*.log` files in the config dir. |
| [`ConfigTerminal/Logs/LogHighlight.cs`](../descriptions/ConfigTerminal/Logs/LogHighlight.cs.md) | 43 | Classifies a single log line for colour highlighting in the log viewer. |
| [`ConfigTerminal/Logs/LogTailReader.cs`](../descriptions/ConfigTerminal/Logs/LogTailReader.cs.md) | 150 | Memory-bounded tail reader over a single log file: it holds only the last window of bytes (default 256 KB) so it stays cheap even on multi-GB logs, and follows appended bytes `tail -f`-style. |
| [`ConfigTerminal/Logs/ReadinessDetector.cs`](../descriptions/ConfigTerminal/Logs/ReadinessDetector.cs.md) | 38 | Detects that the DS has finished loading a world by scanning the game log's tail for the "Game ready" readiness marker (§2.9). |

## Public API surface

- `LogTailReader.Load()`
- `LogTailReader.Poll()`
- `LogTailReader.Lines`
- `LogTailReader.StructuralChange`
- `LogCatalog(InstanceBinding binding)`
- `LogCatalog.Scan()`
- `LogCatalog.Files`
- `LogCatalog.ActiveGameLog`
- `LogFileInfo.DisplayName`
- `ReadinessDetector.IsReady(string path)`
- `LogHighlight.Classify(string line)`
- `LogHighlight.ReadyMarker`
- `LogHighlight.ExceptionMarker`

## Dependencies

**Uses modules:** [ConfigTerminal.Model](ConfigTerminal.Model.md)  
**Used by modules:** [ConfigTerminal.Ui](ConfigTerminal.Ui.md), [ConfigTerminalTests](ConfigTerminalTests.md)  
**External systems:** System.IO; System.Text; System.Text.RegularExpressions

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
