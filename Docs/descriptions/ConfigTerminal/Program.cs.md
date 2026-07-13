# ConfigTerminal/Program.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal` · **Kind:** static class · **Lines:** 123

## Summary
Application entry point for the MagnetarConfig TUI: parses the command line, dispatches the special headless (`-diag`) and help modes, selects the Terminal.Gui driver, runs the launcher/instance pickers, and hosts the top-level `AppShell` under a try/catch/finally that guarantees `Application.Shutdown()`. On Windows it forces UTF-8 console output so box-drawing/shade glyphs render on legacy consoles, and offers a launcher picker when both the Legacy (.NET Framework 4.8) and Interim (.NET 10) Magnetar launchers are installed.

## Types
### Program — static class, internal
Process-wide startup and error-handling shell around Terminal.Gui.

- **Methods:**
  - `Main(string[] args)` — parses via `Cli.Parse`; prints help and returns 0 when `cli.Help`; writes `cli.Error` to stderr and returns 1 on a parse error. For `-diag` it builds an `InstanceBinding`, errors (exit 1) if the data dir does not exist, and returns `Diagnostics.Run(binding)` without ever touching Terminal.Gui. Otherwise sets `Console.OutputEncoding = UTF8` on Windows (guarded), enables `Application.UseSystemConsole` when `-netdriver` is given, then `Application.Init()` + `TurboVisionTheme.Apply()`. Resolves the binding; on Windows, when neither `-magnetar` nor `-config` was pinned, queries `InstanceLocator.PresentWindowsLaunchers()` and auto-selects the single launcher or prompts via `ChooseLauncher` (returning 0 if cancelled), copying the chosen launcher's config dir/exe path into the binding. When no instance was specified and the default data dir is missing, opens `InstancePickerDialog.Show` (returns 0 if cancelled). Finally constructs `AppShell(binding)` and `Application.Run`s it. Any exception is caught: shuts down, prints `Fatal: <exception>` to stderr, returns 1; the `finally` calls `Application.Shutdown()` unconditionally.
  - `ChooseLauncher(IReadOnlyList<MagnetarLauncher> launchers)` — builds a button list from each launcher's `Label` plus a trailing "Cancel", shows it through `Dialogs.QueryDetails` ("Select Magnetar"), and returns the chosen `MagnetarLauncher` or `null` when Cancel was picked.

## Cross-references
- **Uses:** `Cli` (this module), `Diagnostics` (this module), `InstanceBinding`/`MagnetarLauncher`/`InstanceLocator` (`ConfigTerminal/Model/`), `PlatformPaths` (`ConfigTerminal/Io/`), `AppShell`/`InstancePickerDialog`/`Dialogs`/`TurboVisionTheme` (`ConfigTerminal/Ui/`), Terminal.Gui `Application`, `System.Text.Encoding`, `System.IO.Directory`.
- **Used by:** _none within the repository_
