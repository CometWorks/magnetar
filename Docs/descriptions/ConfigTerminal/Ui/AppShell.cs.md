# ConfigTerminal/Ui/AppShell.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 487

## Summary
The application shell for MagnetarConfig: a Terminal.Gui v1 `Toplevel` hosting a Turbo Vision desktop with a menu bar, an F-key status bar carrying the live server state, and a single swappable content panel. It owns the instance/process/monitor/settings objects, drives navigation to every view, runs the two `MainLoop` timers (a 2 s process-status poll and a 1 s auto-save tick that flushes the current `IAutoSaveContent` panel), and coordinates all process actions (start/stop/restart/reload/force-kill) with confirmation and background execution.

## Types
### AppShell — sealed class, internal
Root window and controller. Constructed from an `InstanceBinding`; opens the `DsInstance`, wires the status/auto-save timers, and exposes the shared services to views.

- **Fields:**
  - `binding` (`InstanceBinding`), `writer` (`AtomicFile`), `process` (`MagnetarProcess`), `monitor` (`ProcessMonitor`), `settings` (`ToolSettings`) — the shared collaborators.
  - `instance` (`DsInstance`) — the currently open instance (re-opened by `ReopenInstance`).
  - `content` (`View`) — the hosted panel; `dashboard` (`DashboardView`) — non-null only while the dashboard is shown, so status ticks can update it.
  - `statusLabel` (`Label`) — the live-state line above the status bar.
  - `starting` (bool) — UI-only latch bridging the gap between launch and the pid file appearing, so the status shows STARTING.
  - `warnedNoSafeStop` (bool) — Windows-only; the "no safe stop" notice is shown at most once per session.
- **Properties:** `Binding`, `Instance`, `Process`, `Monitor`, `Writer` — exposed so views can drive operations through the shell.
- **Methods:**
  - `AppShell(InstanceBinding)` — opens the instance, applies `TurboVisionTheme.Desktop`, adds the `DesktopBackground`, menu, status bar and status label, shows the Worlds view, subscribes to `monitor.Changed`, and registers the 2 s poll + 1 s auto-save-flush timeouts.
  - `BuildMenu()` — builds the `MenuBar` (File / Server / Worlds / Plugins / Tools / Help) wiring each item to a Show*/action method.
  - `BuildStatusBar()` — builds the `StatusBar` (F1/F3/F4/F5/F7/F8/F10) and the anchored `statusLabel`.
  - `RefreshStatus()` — recomputes the state (folding in the UI-driven STARTING), picks a glyph, and updates the status line with world + UDP port; also forwards to `dashboard.UpdateStatus`.
  - `SetContent(View)` — the content-host switch: flushes the outgoing `IAutoSaveContent`, and if it has invalid fields confirms before leaving (aborting the switch and disposing the incoming view on Stay); disposes the old panel, positions the new one inside the border, adds and focuses it.
  - `ShowDashboard()`, `ShowServerSettings()`, `ShowNewWorldDefaults()`, `ShowAccessLists()`, `ShowPasswordDialog()`, `ShowWorlds()`, `ShowWorldContent(View)`, `ShowLogs()`, `ShowPlugins()`, `ShowHubPlugins()`, `ShowPluginSources()`, `ShowProfiles()`, `ShowNewWorldWizard()` — navigation; the two option forms are built from `OptionRegistry.DedicatedOptions`/`SessionOptions` over `instance.Config` with an `EditSession`.
  - `OnConfigSaved()` / `ReloadInstance()` — reload the instance and refresh status after a save.
  - `ToggleServer()`, `StartServer(bool confirm)`, `ConfirmStart()`, `CollectPlugins()`, `CollectMods()`, `AppendList(...)` — start path: `ConfirmStart` shows the world, UDP port, and a best-effort list of plugins (dev folders + local DLLs + enabled hub plugins resolved to friendly names) and mods; `StartServer` sets `starting`, runs `process.Start` in the background, and clears the latch on completion.
  - `StopServer()`, `OfferForceKill(string)`, `ForceKillServer()`, `ForceKill()` — stop path: Linux sends SIGTERM (save+quit) with a 2 min timeout, offering a force-kill on timeout; Windows warns once then force-kills (no safe stop).
  - `RestartServer()`, `ReloadServer()` — restart (stop then start) and live config reload.
  - `ReopenInstance()` — opens `InstancePickerDialog`, copies the chosen binding fields, re-opens the instance, and shows the dashboard.
  - `ShowAbout()` — shows `HelpDialog`.
  - `RequestQuit()` — flushes the current panel, folds any invalid-field note into a single Quit confirm, and calls `Application.RequestStop()`.

## Cross-references
- **Uses:** Terminal.Gui `Toplevel`/`MenuBar`/`StatusBar`/`Label`/`View`; `InstanceBinding`/`DsInstance`/`WorldInfo`/`ModList`/`ModItem`/`WorldConfigDocument`/`PluginProfileDocument`/`MagnetarPlugins`/`OptionRegistry` (`ConfigTerminal/Model/`); `MagnetarProcess`/`ProcessMonitor`/`ServerStatus`/`ServerState`/`LaunchSpec`/`OpResult` (`ConfigTerminal/Process/`); `AtomicFile`/`PlatformPaths` (`ConfigTerminal/Io/`); `ToolSettings` (`ConfigTerminal/State/`); sibling views (`DashboardView`, `OptionFormView`, `WorldsView`, `LogViewerView`, `PluginsView`, `HubPluginsView`, `PluginSourcesView`, `ProfilesView`, `AccessListView`, `NewWorldWizard`, `InstancePickerDialog`, `PasswordDialog`, `HelpDialog`, `Dialogs`, `TurboVisionTheme`, `DesktopBackground`, `IAutoSaveContent`).
- **Used by:** [Program.cs](../Program.cs.md), [DashboardView.cs](DashboardView.cs.md), [NewWorldWizard.cs](NewWorldWizard.cs.md), [WorldsView.cs](WorldsView.cs.md), [UiSmokeTests.cs](../../ConfigTerminalTests/UiSmokeTests.cs.md)
