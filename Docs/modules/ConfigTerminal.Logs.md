# Module: ConfigTerminal.Logs

**Project:** `ConfigTerminal` · **Files:** 3 · **Source lines:** 331

## Purpose

Discovers and tail-reads the log files behind the ConfigTerminal TUI log viewer. It enumerates the DS game logs (SpaceEngineersDedicated*.log) and Magnetar's info_*.log files for the bound instance, and provides a memory-bounded windowed reader that follows appended bytes tail -f-style while surviving truncation and rotation. All IO is failure-tolerant (missing or locked files degrade gracefully) and there is no Terminal.Gui dependency.

## Role in Magnetar

This is the file-discovery and streaming layer under the ConfigTerminal log-viewing UI. LogCatalog resolves paths from an InstanceBinding (ConfigTerminal.Model) and marks the active log of each group; LogTailReader feeds the viewer with a bounded, incrementally-updated window plus a structural-change signal so the UI knows when to rebuild versus append. ReadinessDetector scans for the 'Game ready' marker but is currently unused — a vestige of an earlier staged world-creation plan, retained with no callers.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `LogTailReader` | class | [`ConfigTerminal/Logs/LogTailReader.cs`](../descriptions/ConfigTerminal/Logs/LogTailReader.cs.md) | Memory-bounded (256 KB window) tail reader that follows appended bytes and reloads on truncation/rotation. |
| `LogCatalog` | class | [`ConfigTerminal/Logs/LogCatalog.cs`](../descriptions/ConfigTerminal/Logs/LogCatalog.cs.md) | Filesystem discovery of game and Magnetar log files for the bound instance, marking each group's active file. |
| `LogFileInfo` | class | [`ConfigTerminal/Logs/LogCatalog.cs`](../descriptions/ConfigTerminal/Logs/LogCatalog.cs.md) | One discovered log file with path, group, last-write time, size, active flag, and a compact timestamped display name. |
| `LogGroup` | enum | [`ConfigTerminal/Logs/LogCatalog.cs`](../descriptions/ConfigTerminal/Logs/LogCatalog.cs.md) | Which log stream a file belongs to: Game or Magnetar. |
| `ReadinessDetector` | static class | [`ConfigTerminal/Logs/ReadinessDetector.cs`](../descriptions/ConfigTerminal/Logs/ReadinessDetector.cs.md) | Tail-scans the game log for the 'Game ready' marker; currently unused vestige of a prior plan. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`ConfigTerminal/Logs/LogCatalog.cs`](../descriptions/ConfigTerminal/Logs/LogCatalog.cs.md) | 143 | Pure-filesystem discovery of the log files for the bound instance (§2.9): the DS game logs (`SpaceEngineersDedicated*.log`) in the DS data dir and Magnetar's `info_*.log` files in the config dir. |
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

## Dependencies

**Uses modules:** [ConfigTerminal.Model](ConfigTerminal.Model.md)  
**Used by modules:** [ConfigTerminal.Ui](ConfigTerminal.Ui.md), [ConfigTerminalTests](ConfigTerminalTests.md)  
**External systems:** System.IO; System.Text; System.Text.RegularExpressions

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
