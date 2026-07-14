# ConfigTerminal/Model/DsInstance.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 121

## Summary
The aggregate root that binds a DS instance's cfg, worlds, templates and last-session together for the session. Opening never throws for content problems — they are recorded in `InstanceProblems` so the UI can open anything and show what is wrong (a config tool must be able to repair a broken instance).

## Types
### InstanceBinding — sealed class, internal
The identity of the one instance the tool is bound to.
- **Fields:** `DataDir` (`-path`: cfg + Saves), `MagnetarConfigDir` (`-config`: state/logs/pid), `MagnetarExePath` (launcher), `Ds64Dir` (DS install, auto-detected).
- **Properties (derived paths):** `ConfigPath` (`SpaceEngineers-Dedicated.cfg`), `SavesPath` (`Saves`), `PidFilePath` (`magnetar.pid`).
### InstanceProblems — sealed class, internal
Non-fatal problems found while opening; `Messages`, `Any`, `Add(m)`.
### DsInstance — sealed class, internal
- **Properties:** `Binding`, `Config` (`DedicatedConfigDocument`), `Worlds` (`WorldCatalog`), `Templates` (`WorldTemplateCatalog`), `LastSession` (`LastSessionFile`), `Problems`, `ActiveWorld` (the world flagged active).
- **Methods:**
  - `Open(InstanceBinding binding)` (static) — constructs and `Reload`s.
  - `Reload()` — opens the cfg (recording read errors), scans worlds and templates, reads LastSession, and resolves the active world; records problems for a missing Saves folder or absent templates.
  - `ResolveActiveWorld()` (private) — flags the world the DS would load next per the LastSession precedence rules (§2.3): honors `IgnoreLastSession`, matches LastSession by `RelativePath` then `Path`, else falls back to cfg `LoadWorld`.
  - `MatchLastSession()` (private) — locates the world named by the LastSession relative/absolute path.

## Cross-references
- **Uses:** `DedicatedConfigDocument`/`WorldCatalog`/`WorldTemplateCatalog`/`LastSessionFile`/`WorldInfo` (this module); `System.IO`, `System.Linq`.
- **Used by:** [Cli.cs](../Cli.cs.md), [Diagnostics.cs](../Diagnostics.cs.md), [InstanceLocator.cs](../Io/InstanceLocator.cs.md), [LogCatalog.cs](../Logs/LogCatalog.cs.md), [LaunchSpec.cs](../Process/LaunchSpec.cs.md), [MagnetarProcess.cs](../Process/MagnetarProcess.cs.md), [Program.cs](../Program.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [DashboardView.cs](../Ui/DashboardView.cs.md), [InstancePickerDialog.cs](../Ui/InstancePickerDialog.cs.md), [LogViewerView.cs](../Ui/LogViewerView.cs.md), [NewWorldWizard.cs](../Ui/NewWorldWizard.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md), [UiSmokeTests.cs](../../ConfigTerminalTests/UiSmokeTests.cs.md)
