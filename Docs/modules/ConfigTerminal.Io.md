# Module: ConfigTerminal.Io

**Project:** `ConfigTerminal` · **Files:** 4 · **Source lines:** 316

## Purpose

Low-level file and path plumbing for the MagnetarConfig TUI. Provides crash-safe atomic text writes with once-per-session .bak backups, shared XML writer settings that match what the DS and Quasar emit (UTF-8 no BOM, indented, LF newlines, declaration present), per-platform OS detection and filesystem-correct path comparison, and resolution of the default DS/Magnetar/launcher/DS-install locations. It has no Terminal.Gui dependency.

## Role in Magnetar

The foundational I/O layer beneath the ConfigTerminal editing and UI code. Model and higher layers use InstanceLocator to resolve where DS data, Magnetar config, launcher executables, and the DedicatedServer64 install live (honoring explicit CLI overrides), AtomicFile to persist config safely, XmlOut to serialize DS-format XML with clean cross-platform diffs, and PlatformPaths for OS branching and world-folder identity comparisons.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `InstanceLocator` | static class | [`ConfigTerminal/Io/InstanceLocator.cs`](../descriptions/ConfigTerminal/Io/InstanceLocator.cs.md) | Resolves default DS data dir, Magnetar config dir, launcher exe, and DS64 install per platform, filling only unset binding fields. |
| `MagnetarLauncher` | class | [`ConfigTerminal/Io/InstanceLocator.cs`](../descriptions/ConfigTerminal/Io/InstanceLocator.cs.md) | A Windows Magnetar launcher variant (name, label, config dir, exe path) the tool can configure. |
| `AtomicFile` | class | [`ConfigTerminal/Io/AtomicFile.cs`](../descriptions/ConfigTerminal/Io/AtomicFile.cs.md) | Crash-safe text writer: temp file + atomic rename, with a once-per-session .bak backup of the target. |
| `XmlOut` | static class | [`ConfigTerminal/Io/XmlOut.cs`](../descriptions/ConfigTerminal/Io/XmlOut.cs.md) | Shared XmlWriterSettings (UTF-8 no BOM, indented, LF) and XDocument-to-string serialization. |
| `PlatformPaths` | static class | [`ConfigTerminal/Io/PlatformPaths.cs`](../descriptions/ConfigTerminal/Io/PlatformPaths.cs.md) | OS detection (IsWindows/IsLinux) and filesystem-correct path comparer/comparison. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`ConfigTerminal/Io/AtomicFile.cs`](../descriptions/ConfigTerminal/Io/AtomicFile.cs.md) | 83 | Crash-safe text file writer: content is written to a temp file in the same directory, flushed to disk, then atomically renamed over the target, so the destination is never observed half-written or truncated. |
| [`ConfigTerminal/Io/InstanceLocator.cs`](../descriptions/ConfigTerminal/Io/InstanceLocator.cs.md) | 161 | Resolves the default DS data dir, Magnetar config dir, Magnetar launcher executable, and DS install (`DedicatedServer64`) locations using the same per-platform semantics Magnetar itself uses, so a non-standard deployment resolves end to end. |
| [`ConfigTerminal/Io/PlatformPaths.cs`](../descriptions/ConfigTerminal/Io/PlatformPaths.cs.md) | 29 | Platform helpers used throughout the Io module: OS detection (`IsWindows`/`IsLinux`) and filesystem-correct path comparison (case-insensitive on Windows, case-sensitive on Linux) for world-folder identity checks. |
| [`ConfigTerminal/Io/XmlOut.cs`](../descriptions/ConfigTerminal/Io/XmlOut.cs.md) | 43 | Shared XML output settings matching what the DS and Quasar write — UTF-8 without BOM, indented, LF (`\n`) newlines, XML declaration present — plus a helper that serializes an `XDocument` to a string with those settings. |

## Public API surface

- `InstanceLocator.DefaultDataDir()`
- `InstanceLocator.DefaultMagnetarConfigDir()`
- `InstanceLocator.DefaultMagnetarExe()`
- `InstanceLocator.PresentWindowsLaunchers()`
- `InstanceLocator.DetectDs64()`
- `InstanceLocator.ResolveDefaults(InstanceBinding)`
- `AtomicFile.WriteText(string path, string content)`
- `XmlOut.Settings()`
- `XmlOut.ToXmlString(XDocument document)`
- `PlatformPaths.IsWindows / IsLinux`
- `PlatformPaths.PathComparer / PathComparison`

## Dependencies

**Uses modules:** [ConfigTerminal.Model](ConfigTerminal.Model.md)  
**Used by modules:** [ConfigTerminal.App](ConfigTerminal.App.md), [ConfigTerminal.Model](ConfigTerminal.Model.md), [ConfigTerminal.Process](ConfigTerminal.Process.md), [ConfigTerminal.Ui](ConfigTerminal.Ui.md), [ConfigTerminalTests](ConfigTerminalTests.md)  
**External systems:** System.IO; System.Runtime.InteropServices; System.Text; System.Xml; System.Xml.Linq

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
