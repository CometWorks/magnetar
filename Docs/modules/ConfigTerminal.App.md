# Module: ConfigTerminal.App

**Project:** `ConfigTerminal` · **Files:** 4 · **Source lines:** 380

## Purpose

Application entry and process-wide plumbing for the MagnetarConfig TUI. Parses the command line into an InstanceBinding, selects the Terminal.Gui driver, runs the launcher and instance pickers, and hosts the top-level AppShell with unified error handling. Also provides the headless `-diag` state report (no Terminal.Gui) and persists the tool's own per-instance settings as `ConfigTerminal.xml`.

## Role in Magnetar

This is the outermost layer of the MagnetarConfig TUI: `Program.Main` is the process entry point that bootstraps everything below it and guarantees clean Terminal.Gui shutdown. It bridges the raw args to the Model layer's `InstanceBinding`/`InstanceLocator`, dispatches to the Ui layer (`AppShell`, pickers, dialogs, theme) for the interactive path, and to the read-only Diagnostics path for scripts/CI. ToolSettings gives the UI a small persisted store that lives alongside Magnetar's own config so per-instance preferences travel with the instance.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `Program` | static class | [`ConfigTerminal/Program.cs`](../descriptions/ConfigTerminal/Program.cs.md) | Process entry point: parses args, picks driver/launcher/instance, hosts AppShell with top-level error handling. |
| `Cli` | class | [`ConfigTerminal/Cli.cs`](../descriptions/ConfigTerminal/Cli.cs.md) | Parses the command line into typed options and converts them to a default-resolved InstanceBinding. |
| `Diagnostics` | static class | [`ConfigTerminal/Diagnostics.cs`](../descriptions/ConfigTerminal/Diagnostics.cs.md) | Headless, read-only `-diag` report of an instance's resolved config, worlds, plugins, profiles, and server status. |
| `ToolSettings` | class | [`ConfigTerminal/State/ToolSettings.cs`](../descriptions/ConfigTerminal/State/ToolSettings.cs.md) | Fault-tolerant per-instance tool settings persisted as ConfigTerminal.xml next to Magnetar's config.xml. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`ConfigTerminal/Cli.cs`](../descriptions/ConfigTerminal/Cli.cs.md) | 92 | Parses the MagnetarConfig command line into a strongly-typed options object and converts it into an `InstanceBinding` with defaults filled in. |
| [`ConfigTerminal/Diagnostics.cs`](../descriptions/ConfigTerminal/Diagnostics.cs.md) | 106 | Produces the headless, read-only `-diag` report of an instance's resolved state without starting Terminal.Gui, exercising the same model/process layers the UI uses. |
| [`ConfigTerminal/Program.cs`](../descriptions/ConfigTerminal/Program.cs.md) | 123 | Application entry point for the MagnetarConfig TUI: parses the command line, dispatches the special headless (`-diag`) and help modes, selects the Terminal.Gui driver, runs the launcher/instance pickers, and hosts the top-level `AppShell` under a try/catch/finally that guarantees `Application.Shutdown()`. |
| [`ConfigTerminal/State/ToolSettings.cs`](../descriptions/ConfigTerminal/State/ToolSettings.cs.md) | 59 | The TUI tool's own per-instance settings, persisted as a small `ConfigTerminal.xml` next to Magnetar's `config.xml` in the selected config dir so per-instance state travels with the instance. |

## Public API surface

- `Program.Main(string[] args)`
- `Cli.Parse(string[] args)`
- `Cli.ToBinding()`
- `Cli.HasInstance`
- `Cli.PrintHelp()`
- `Diagnostics.Run(InstanceBinding binding)`
- `ToolSettings.Load(string magnetarConfigDir)`
- `ToolSettings.Save(AtomicFile writer)`
- `ToolSettings.LastPluginFolder`
- `CLI flags: -path, -config, -magnetar, -ds64, -netdriver, -diag, -help/-h/--help`

## Dependencies

**Uses modules:** [ConfigTerminal.Io](ConfigTerminal.Io.md), [ConfigTerminal.Model](ConfigTerminal.Model.md), [ConfigTerminal.Process](ConfigTerminal.Process.md), [ConfigTerminal.Ui](ConfigTerminal.Ui.md)  
**Used by modules:** [ConfigTerminal.Ui](ConfigTerminal.Ui.md)  
**External systems:** System.Xml.Linq; Terminal.Gui

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
