# ConfigTerminal/Ui/DashboardView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 110

## Summary
The home window: a live server-status line, a read-only text summary of the instance (paths, server name/ports/network/password, active world, world/template counts, and any warnings or problems), and the Start/Stop/Restart/Reload/Worlds/New-World controls that delegate to the shell.

## Types
### DashboardView — sealed class, internal (`Window`)
The Dashboard panel.

- **Fields:** `shell` (`AppShell`), `statusLine` (`Label`), `summary` (`TextView`).
- **Methods:**
  - `DashboardView(AppShell)` — builds the status line, the read-only summary text view, and the action buttons (each wired to a shell method); builds the summary once.
  - `UpdateStatus(ServerStatus)` — refreshes the status line (with any detail); called by the shell on each status tick.
  - `BuildSummary()` — private; composes the instance summary from the binding paths and the `DedicatedConfigDocument`, reading fields by id via `Val`, flagging `IgnoreLastSession`/`PremadeCheckpointPath`/`LoadWorld`, and listing any `instance.Problems`.
  - `Val(cfg, id)` — private; resolves an `OptionDefinition` by id from `OptionRegistry` and reads its value, or empty when unknown.

## Cross-references
- **Uses:** Terminal.Gui `Window`/`Label`/`TextView`/`Button`; `DsInstance`/`DedicatedConfigDocument`/`WorldInfo`/`OptionDefinition`/`OptionRegistry` (`ConfigTerminal/Model/`); `ServerStatus` (`ConfigTerminal/Process/`); `AppShell`, `TurboVisionTheme` (this module); `System.Text.StringBuilder`.
- **Used by:** [AppShell.cs](AppShell.cs.md)
