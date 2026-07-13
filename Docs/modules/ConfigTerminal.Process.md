# Module: ConfigTerminal.Process

**Project:** `ConfigTerminal` · **Files:** 5 · **Source lines:** 524

## Purpose

Controls the single managed Magnetar DS instance for the config terminal: it reads and verifies the launcher's magnetar.pid file (distinguishing running, stale, and foreign states), builds the daemon launch command line from the instance binding, spawns the launcher detached with discarded output, delivers lifecycle signals, and exposes a change-detecting status poller for the UI. It classifies process state into a ServerStatus snapshot rather than ever trusting the pid file's presence alone.

## Role in Magnetar

This is the process-control layer of the ConfigTerminal (MagnetarConfig) tool, sitting between the Model/Io layers and the Terminal.Gui views. It carries no Terminal.Gui dependency: MagnetarProcess and PidFileReader do the imperative and read-only work, while ProcessMonitor is driven by the view's main-loop timer so all callbacks stay on the single UI thread. It consumes InstanceBinding (ConfigTerminal.Model) and PlatformPaths (ConfigTerminal.Io) and drives the launcher process whose in-process ServerControl handles the actual save/quit inside Magnetar.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `MagnetarProcess` | class | [`ConfigTerminal/Process/MagnetarProcess.cs`](../descriptions/ConfigTerminal/Process/MagnetarProcess.cs.md) | Starts/stops/reloads/force-kills the one instance and queries its status; graceful Stop (SIGTERM) and Reload (SIGHUP) are Linux-only, Windows offers only force-kill. |
| `PidFileReader` | class | [`ConfigTerminal/Process/PidFileReader.cs`](../descriptions/ConfigTerminal/Process/PidFileReader.cs.md) | Reads and verifies magnetar.pid, classifying state as NotRunning/Running/StalePidFile/Foreign via live-pid and layered identity checks. |
| `LaunchSpec` | class | [`ConfigTerminal/Process/LaunchSpec.cs`](../descriptions/ConfigTerminal/Process/LaunchSpec.cs.md) | Builds the daemon launch argument list from the binding and rejects user extra args that collide with tool-managed switches. |
| `ServerStatus` | class | [`ConfigTerminal/Process/ServerStatus.cs`](../descriptions/ConfigTerminal/Process/ServerStatus.cs.md) | Snapshot of process state (pid, start time, detail) with uptime and status-line formatting. |
| `ServerState` | enum | [`ConfigTerminal/Process/ServerStatus.cs`](../descriptions/ConfigTerminal/Process/ServerStatus.cs.md) | NotRunning, Starting, Running, Stopping, StalePidFile, Foreign. |
| `ProcessMonitor` | class | [`ConfigTerminal/Process/ProcessMonitor.cs`](../descriptions/ConfigTerminal/Process/ProcessMonitor.cs.md) | Polls status and raises Changed on a state/pid transition, driven by the UI main-loop timer. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`ConfigTerminal/Process/LaunchSpec.cs`](../descriptions/ConfigTerminal/Process/LaunchSpec.cs.md) | 71 | Builds the Magnetar daemon launch command line from an `InstanceBinding` and validates user-supplied extra arguments. |
| [`ConfigTerminal/Process/MagnetarProcess.cs`](../descriptions/ConfigTerminal/Process/MagnetarProcess.cs.md) | 218 | Controls the single managed Magnetar DS instance: starts it daemonized, gracefully stops it (SIGTERM → save+quit), reloads live config (SIGHUP), force-kills it, and queries its status via the pid file. |
| [`ConfigTerminal/Process/PidFileReader.cs`](../descriptions/ConfigTerminal/Process/PidFileReader.cs.md) | 152 | Reads and verifies the `magnetar.pid` file written by the launcher (spec §2.8), producing a `ServerStatus` snapshot. |
| [`ConfigTerminal/Process/ProcessMonitor.cs`](../descriptions/ConfigTerminal/Process/ProcessMonitor.cs.md) | 39 | Polls the managed instance's status and raises `Changed` when it moves, so the UI can react to start/stop/foreign transitions. |
| [`ConfigTerminal/Process/ServerStatus.cs`](../descriptions/ConfigTerminal/Process/ServerStatus.cs.md) | 44 | Defines the `ServerState` enum and the `ServerStatus` snapshot that carries the managed instance's process state across the module. |

## Public API surface

- `MagnetarProcess(InstanceBinding)`
- `MagnetarProcess.Query()`
- `MagnetarProcess.Start(LaunchSpec, TimeSpan?)`
- `MagnetarProcess.Stop(TimeSpan) [Linux-only, SIGTERM]`
- `MagnetarProcess.Reload() [Linux-only, SIGHUP]`
- `MagnetarProcess.ForceKill(TimeSpan)`
- `PidFileReader.Query()`
- `LaunchSpec.RejectionReason()`
- `LaunchSpec.BuildArgs()`
- `ProcessMonitor.Poll()`
- `ProcessMonitor.Changed (event)`

## Dependencies

**Uses modules:** [ConfigTerminal.Io](ConfigTerminal.Io.md), [ConfigTerminal.Model](ConfigTerminal.Model.md)  
**Used by modules:** [ConfigTerminal.App](ConfigTerminal.App.md), [ConfigTerminal.Ui](ConfigTerminal.Ui.md), [ConfigTerminalTests](ConfigTerminalTests.md)  
**External systems:** Linux /proc/<pid>/cmdline; System.Diagnostics.Process; libc kill (P/Invoke, Linux signals)

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
