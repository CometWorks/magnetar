# Module: ConfigTerminal.Ui

**Project:** `ConfigTerminal` · **Files:** 21 · **Source lines:** 3452

## Purpose

The Terminal.Gui v1 (classic Turbo Vision) presentation layer of the MagnetarConfig TUI. It provides the application shell (menu bar, F-key status bar, Turbo Vision desktop, panel navigation, and process-status/auto-save timers), a generic registry-driven settings form, and dedicated editors for worlds, mods, access lists, passwords, plugins, hub plugins, plugin sources, and profiles, plus the new-world wizard, log viewer, instance picker, shared dialogs, and the color theme. Editable panels implement IAutoSaveContent so edits persist automatically on a ~1s tick, on panel switch, and on quit with no explicit Save step.

## Role in Magnetar

This is the human-facing front end of MagnetarConfig: it renders and drives every screen and delegates all data and process work to the other ConfigTerminal modules. Views read and mutate configuration through ConfigTerminal.Model (documents, edit sessions, option registry, plugin/profile catalogs, world/template info), write atomically via ConfigTerminal.Io (AtomicFile, PlatformPaths), operate the server through ConfigTerminal.Process (MagnetarProcess, ProcessMonitor, ServerStatus), tail logs via ConfigTerminal.Logs, and persist tool preferences via ConfigTerminal.State/App. AppShell is the composition root that wires these collaborators together and hosts one content panel at a time.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `AppShell` | class | [`ConfigTerminal/Ui/AppShell.cs`](../descriptions/ConfigTerminal/Ui/AppShell.cs.md) | Toplevel app shell: menu/status bar, panel host, status-poll and auto-save timers, and all process actions. |
| `OptionFormView` | class | [`ConfigTerminal/Ui/OptionFormView.cs`](../descriptions/ConfigTerminal/Ui/OptionFormView.cs.md) | Generic registry-driven settings form with live validation and auto-save; used for DS config, new-world defaults, and per-world settings. |
| `WorldsView` | class | [`ConfigTerminal/Ui/WorldsView.cs`](../descriptions/ConfigTerminal/Ui/WorldsView.cs.md) | Lists worlds under Saves/ with settings/mods editing, activation, creation, and deletion. |
| `ModListView` | class | [`ConfigTerminal/Ui/ModListView.cs`](../descriptions/ConfigTerminal/Ui/ModListView.cs.md) | Ordered mod-list editor for a world's Sandbox_config.sbc with background Workshop name resolution. |
| `LogViewerView` | class | [`ConfigTerminal/Ui/LogViewerView.cs`](../descriptions/ConfigTerminal/Ui/LogViewerView.cs.md) | Read-only log viewer with tail -f follow mode over a memory-bounded reader. |
| `PluginsView` | class | [`ConfigTerminal/Ui/PluginsView.cs`](../descriptions/ConfigTerminal/Ui/PluginsView.cs.md) | Manages local DLLs and registered dev-folder plugin sources. |
| `HubPluginsView` | class | [`ConfigTerminal/Ui/HubPluginsView.cs`](../descriptions/ConfigTerminal/Ui/HubPluginsView.cs.md) | Browses cached hub/remote plugin catalogs and dev folders, toggling them in the active profile. |
| `PluginSourcesView` | class | [`ConfigTerminal/Ui/PluginSourcesView.cs`](../descriptions/ConfigTerminal/Ui/PluginSourcesView.cs.md) | Edits the remote-hub, remote-plugin, and local-hub catalog sources in sources.xml. |
| `ProfilesView` | class | [`ConfigTerminal/Ui/ProfilesView.cs`](../descriptions/ConfigTerminal/Ui/ProfilesView.cs.md) | Manages named plugin profiles (load/save/update/rename/delete) against the active Current.xml set. |
| `AccessListView` | class | [`ConfigTerminal/Ui/AccessListView.cs`](../descriptions/ConfigTerminal/Ui/AccessListView.cs.md) | Editors for the Administrators/Banned/Reserved SteamID lists and GroupID. |
| `DashboardView` | class | [`ConfigTerminal/Ui/DashboardView.cs`](../descriptions/ConfigTerminal/Ui/DashboardView.cs.md) | Home window with live status, an instance summary, and process-control buttons. |
| `NewWorldWizard` | static class | [`ConfigTerminal/Ui/NewWorldWizard.cs`](../descriptions/ConfigTerminal/Ui/NewWorldWizard.cs.md) | Creates a world by copying a DS template and activating it, no server start required. |
| `Dialogs` | static class | [`ConfigTerminal/Ui/Dialogs.cs`](../descriptions/ConfigTerminal/Ui/Dialogs.cs.md) | Shared modal helpers (info/error/confirm/details/prompt) and a UI-thread-safe background runner. |
| `FileDialogs` | static class | [`ConfigTerminal/Ui/FileDialogs.cs`](../descriptions/ConfigTerminal/Ui/FileDialogs.cs.md) | Directory/file browse dialogs that quiet Terminal.Gui's noisy folder watcher via reflection. |
| `InstancePickerDialog` | static class | [`ConfigTerminal/Ui/InstancePickerDialog.cs`](../descriptions/ConfigTerminal/Ui/InstancePickerDialog.cs.md) | Modal prompt for the data/config/launcher/DS-install paths identifying an instance. |
| `PasswordDialog` | static class | [`ConfigTerminal/Ui/PasswordDialog.cs`](../descriptions/ConfigTerminal/Ui/PasswordDialog.cs.md) | Sets or clears the server password, storing only the PBKDF2 hash+salt. |
| `TurboVisionTheme` | static class | [`ConfigTerminal/Ui/TurboVisionTheme.cs`](../descriptions/ConfigTerminal/Ui/TurboVisionTheme.cs.md) | Classic 16-color Turbo Vision palette as Terminal.Gui ColorSchemes, installed as global defaults. |
| `IAutoSaveContent` | interface | [`ConfigTerminal/Ui/IAutoSaveContent.cs`](../descriptions/ConfigTerminal/Ui/IAutoSaveContent.cs.md) | Contract for auto-saving panels: FlushPendingSave plus InvalidFields. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`ConfigTerminal/Ui/AccessListView.cs`](../descriptions/ConfigTerminal/Ui/AccessListView.cs.md) | 151 | Editors for the dedicated config's Administrators / Banned / Reserved SteamID lists plus the GroupID field, laid out as three add/delete columns and one text field. |
| [`ConfigTerminal/Ui/AppShell.cs`](../descriptions/ConfigTerminal/Ui/AppShell.cs.md) | 487 | The application shell for MagnetarConfig: a Terminal.Gui v1 `Toplevel` hosting a Turbo Vision desktop with a menu bar, an F-key status bar carrying the live server state, and a single swappable content panel. |
| [`ConfigTerminal/Ui/DashboardView.cs`](../descriptions/ConfigTerminal/Ui/DashboardView.cs.md) | 110 | The home window: a live server-status line, a read-only text summary of the instance (paths, server name/ports/network/password, active world, world/template counts, and any warnings or problems), and the Start/Stop/Restart/Reload/Worlds/New-World controls that delegate to the shell. |
| [`ConfigTerminal/Ui/DesktopBackground.cs`](../descriptions/ConfigTerminal/Ui/DesktopBackground.cs.md) | 31 | The classic Turbo Vision blue desktop backdrop: a non-focusable `View` that fills its bounds with the `▒` shade glyph and sits behind all content windows. |
| [`ConfigTerminal/Ui/Dialogs.cs`](../descriptions/ConfigTerminal/Ui/Dialogs.cs.md) | 181 | Shared modal dialog helpers in the Turbo Vision look used across every view: info/error/confirm boxes, "details" dialogs that keep a centered question over a left-aligned detail block (so bullet lists aren't mangled by `MessageBox`'s per-line centering), a destructive confirm defaulting to the safe option, a text prompt with optional validation, and a background-work runner that keeps the UI live. |
| [`ConfigTerminal/Ui/FileDialogs.cs`](../descriptions/ConfigTerminal/Ui/FileDialogs.cs.md) | 166 | Filesystem browse dialogs shared by the instance picker (path fields) and the dev-folder manifest picker. |
| [`ConfigTerminal/Ui/HelpDialog.cs`](../descriptions/ConfigTerminal/Ui/HelpDialog.cs.md) | 25 | The About/help modal. |
| [`ConfigTerminal/Ui/HubPluginsView.cs`](../descriptions/ConfigTerminal/Ui/HubPluginsView.cs.md) | 196 | Browses the plugins offered by the instance's configured hub/remote sources — read offline from Magnetar's cached catalogs under `Sources/Hubs` and `Sources/Plugins` — plus the registered dev folders (shown with a "- dev folder" suffix), and enables/disables them in the active profile. |
| [`ConfigTerminal/Ui/IAutoSaveContent.cs`](../descriptions/ConfigTerminal/Ui/IAutoSaveContent.cs.md) | 23 | The contract for a hosted panel that persists its edits automatically. |
| [`ConfigTerminal/Ui/InstancePickerDialog.cs`](../descriptions/ConfigTerminal/Ui/InstancePickerDialog.cs.md) | 96 | Modal dialog that prompts for the folder pair (and launcher / DS install) identifying an instance — the DS data dir, Magnetar config dir, launcher executable, and DS install — each with a Browse button. |
| [`ConfigTerminal/Ui/LogViewerView.cs`](../descriptions/ConfigTerminal/Ui/LogViewerView.cs.md) | 243 | Read-only log viewer over the game and Magnetar log files, with a `tail -f` follow mode and optional word-wrap. |
| [`ConfigTerminal/Ui/ManifestPicker.cs`](../descriptions/ConfigTerminal/Ui/ManifestPicker.cs.md) | 17 | Quasar-style dev-folder picker: browse the filesystem and select a plugin's `.xml` manifest file, opening at the last-visited folder so adding several plugins in a row is frictionless. |
| [`ConfigTerminal/Ui/ModListView.cs`](../descriptions/ConfigTerminal/Ui/ModListView.cs.md) | 219 | Ordered mod-list editor for a world's `Sandbox_config.sbc`: add (by Workshop id or URL, with background friendly-name resolution), delete, reorder, and toggle a mod's dependency flag. |
| [`ConfigTerminal/Ui/NewWorldWizard.cs`](../descriptions/ConfigTerminal/Ui/NewWorldWizard.cs.md) | 158 | New-world creation by folder copy: pick a template, name the world, then copy the template into `Saves/<name>` and stamp the name into its `Sandbox_config.sbc` via `WorldCreator`. |
| [`ConfigTerminal/Ui/OptionFormView.cs`](../descriptions/ConfigTerminal/Ui/OptionFormView.cs.md) | 439 | The generic, registry-driven settings form used for the DS global config, the new-world defaults, and each world's settings. |
| [`ConfigTerminal/Ui/PasswordDialog.cs`](../descriptions/ConfigTerminal/Ui/PasswordDialog.cs.md) | 57 | Modal dialog to set or clear the server password. |
| [`ConfigTerminal/Ui/PluginSourcesView.cs`](../descriptions/ConfigTerminal/Ui/PluginSourcesView.cs.md) | 158 | Manages the instance's plugin catalog *sources* — remote GitHub hubs (e.g. |
| [`ConfigTerminal/Ui/PluginsView.cs`](../descriptions/ConfigTerminal/Ui/PluginsView.cs.md) | 203 | Manages the Magnetar instance's local plugin sources: a left pane of local DLLs from the `Local/` folder (Space toggles enabled) and a right pane of registered dev folders added Quasar-style by picking a manifest XML. |
| [`ConfigTerminal/Ui/ProfilesView.cs`](../descriptions/ConfigTerminal/Ui/ProfilesView.cs.md) | 173 | Manages plugin *profiles* — named presets of the enabled-plugin set stored as `Profiles/<Key>.xml`, with `Current.xml` the active set the server loads. |
| [`ConfigTerminal/Ui/TurboVisionTheme.cs`](../descriptions/ConfigTerminal/Ui/TurboVisionTheme.cs.md) | 73 | The classic 16-color Turbo Vision / Turbo Pascal 7 IDE palette expressed as Terminal.Gui v1 `ColorScheme`s. |
| [`ConfigTerminal/Ui/WorldsView.cs`](../descriptions/ConfigTerminal/Ui/WorldsView.cs.md) | 246 | Lists the worlds found under `Saves/` and offers per-world settings and mod editing, activation, creation, and deletion. |

## Public API surface

- `AppShell(InstanceBinding)`
- `AppShell.StartServer(bool confirm) / StopServer() / RestartServer() / ReloadServer()`
- `AppShell.ShowServerSettings() / ShowWorlds() / ShowLogs() / ShowHubPlugins() / ShowProfiles() / ShowPlugins()`
- `OptionFormView(title, options, document, session, writer, onSaved, banner, editMods)`
- `IAutoSaveContent.FlushPendingSave() / InvalidFields`
- `NewWorldWizard.Run(AppShell)`
- `InstancePickerDialog.Show(...)`
- `PasswordDialog.Show(...)`
- `Dialogs.Confirm/ConfirmDetails/ConfirmDestructive/Prompt/RunBackground(...)`
- `TurboVisionTheme.Apply()`

## Dependencies

**Uses modules:** [ConfigTerminal.App](ConfigTerminal.App.md), [ConfigTerminal.Io](ConfigTerminal.Io.md), [ConfigTerminal.Logs](ConfigTerminal.Logs.md), [ConfigTerminal.Model](ConfigTerminal.Model.md), [ConfigTerminal.Process](ConfigTerminal.Process.md)  
**Used by modules:** [ConfigTerminal.App](ConfigTerminal.App.md), [ConfigTerminalTests](ConfigTerminalTests.md)  
**External systems:** NStack; System.Reflection; Terminal.Gui

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
