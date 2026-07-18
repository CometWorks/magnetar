# Magnetar — Full File Index

Every documented source file, grouped by module. 210 files across 25 modules.

[◀ Back to TOC](TOC.md)

## Compiler  ·  [module doc](modules/Compiler.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Compiler/LogFile.cs`](descriptions/Compiler/LogFile.cs.md) | 122 | 2 | Minimal NLog-backed file logger used by the Compiler module to record Roslyn reference loading, publicizing, and compilation diagnostics to the active Magnetar `info_*.log` file. |
| [`Compiler/PublicizedAssemblies.cs`](descriptions/Compiler/PublicizedAssemblies.cs.md) | 77 | 2 | Bridges Roslyn source analysis with assembly publicizing. |
| [`Compiler/Publicizer.cs`](descriptions/Compiler/Publicizer.cs.md) | 151 | 1 | Performs the actual IL-level publicizing of an SE DS assembly using Mono.Cecil: it reads the assembly from disk, forces every non-public type, field, method, and property to public, and re-emits it to an in-memory `MetadataReference` for Roslyn. |
| [`Compiler/RoslynCompiler.cs`](descriptions/Compiler/RoslynCompiler.cs.md) | 171 | 1 | The core in-process C# compiler used to build local/Workshop plugins from source at server startup. |
| [`Compiler/RoslynReferences.cs`](descriptions/Compiler/RoslynReferences.cs.md) | 84 | 2 | Builds and caches the global set of Roslyn `MetadataReference`s that plugins are compiled against — essentially the SE Dedicated Server / VRage / framework assembly closure. |

## ConfigTerminal.App  ·  [module doc](modules/ConfigTerminal.App.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`ConfigTerminal/Cli.cs`](descriptions/ConfigTerminal/Cli.cs.md) | 92 | 2 | Parses the MagnetarConfig command line into a strongly-typed options object and converts it into an `InstanceBinding` with defaults filled in. |
| [`ConfigTerminal/Diagnostics.cs`](descriptions/ConfigTerminal/Diagnostics.cs.md) | 106 | 2 | Produces the headless, read-only `-diag` report of an instance's resolved state without starting Terminal.Gui, exercising the same model/process layers the UI uses. |
| [`ConfigTerminal/Program.cs`](descriptions/ConfigTerminal/Program.cs.md) | 123 | 2 | Application entry point for the MagnetarConfig TUI: parses the command line, dispatches the special headless (`-diag`) and help modes, selects the Terminal.Gui driver, runs the launcher/instance pickers, and hosts the top-level `AppShell` under a try/catch/finally that guarantees `Application.Shutdown()`. |
| [`ConfigTerminal/State/ToolSettings.cs`](descriptions/ConfigTerminal/State/ToolSettings.cs.md) | 59 | 2 | The TUI tool's own per-instance settings, persisted as a small `ConfigTerminal.xml` next to Magnetar's `config.xml` in the selected config dir so per-instance state travels with the instance. |

## ConfigTerminal.Io  ·  [module doc](modules/ConfigTerminal.Io.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`ConfigTerminal/Io/AtomicFile.cs`](descriptions/ConfigTerminal/Io/AtomicFile.cs.md) | 83 | 2 | Crash-safe text file writer: content is written to a temp file in the same directory, flushed to disk, then atomically renamed over the target, so the destination is never observed half-written or truncated. |
| [`ConfigTerminal/Io/InstanceLocator.cs`](descriptions/ConfigTerminal/Io/InstanceLocator.cs.md) | 161 | 2 | Resolves the default DS data dir, Magnetar config dir, Magnetar launcher executable, and DS install (`DedicatedServer64`) locations using the same per-platform semantics Magnetar itself uses, so a non-standard deployment resolves end to end. |
| [`ConfigTerminal/Io/PlatformPaths.cs`](descriptions/ConfigTerminal/Io/PlatformPaths.cs.md) | 29 | 2 | Platform helpers used throughout the Io module: OS detection (`IsWindows`/`IsLinux`) and filesystem-correct path comparison (case-insensitive on Windows, case-sensitive on Linux) for world-folder identity checks. |
| [`ConfigTerminal/Io/XmlOut.cs`](descriptions/ConfigTerminal/Io/XmlOut.cs.md) | 43 | 2 | Shared XML output settings matching what the DS and Quasar write — UTF-8 without BOM, indented, LF (`\n`) newlines, XML declaration present — plus a helper that serializes an `XDocument` to a string with those settings. |

## ConfigTerminal.Logs  ·  [module doc](modules/ConfigTerminal.Logs.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`ConfigTerminal/Logs/LogCatalog.cs`](descriptions/ConfigTerminal/Logs/LogCatalog.cs.md) | 143 | 2 | Pure-filesystem discovery of the log files for the bound instance (§2.9): the DS game logs (`SpaceEngineersDedicated*.log`) in the DS data dir and Magnetar's `info_*.log` files in the config dir. |
| [`ConfigTerminal/Logs/LogHighlight.cs`](descriptions/ConfigTerminal/Logs/LogHighlight.cs.md) | 43 | 2 | Classifies a single log line for colour highlighting in the log viewer. |
| [`ConfigTerminal/Logs/LogTailReader.cs`](descriptions/ConfigTerminal/Logs/LogTailReader.cs.md) | 150 | 2 | Memory-bounded tail reader over a single log file: it holds only the last window of bytes (default 256 KB) so it stays cheap even on multi-GB logs, and follows appended bytes `tail -f`-style. |
| [`ConfigTerminal/Logs/ReadinessDetector.cs`](descriptions/ConfigTerminal/Logs/ReadinessDetector.cs.md) | 38 | 2 | Detects that the DS has finished loading a world by scanning the game log's tail for the "Game ready" readiness marker (§2.9). |

## ConfigTerminal.Model  ·  [module doc](modules/ConfigTerminal.Model.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`ConfigTerminal/Model/CheckpointReader.cs`](descriptions/ConfigTerminal/Model/CheckpointReader.cs.md) | 76 | 2 | Reads only the handful of header fields needed for display from a `Sandbox.sbc` checkpoint, which may be GZip-compressed. |
| [`ConfigTerminal/Model/ConfigDocumentBase.cs`](descriptions/ConfigTerminal/Model/ConfigDocumentBase.cs.md) | 99 | 2 | Base for the `XDocument`-backed DS config wrappers, implementing per-element upsert editing: unknown elements, comments and the ordering of untouched elements are preserved so the tool coexists with hand edits, the DS's own saves and newer game versions. |
| [`ConfigTerminal/Model/DedicatedConfigDocument.cs`](descriptions/ConfigTerminal/Model/DedicatedConfigDocument.cs.md) | 176 | 2 | `XDocument` wrapper for `SpaceEngineers-Dedicated.cfg` (root `MyConfigDedicated`). |
| [`ConfigTerminal/Model/DefaultHttpFetcher.cs`](descriptions/ConfigTerminal/Model/DefaultHttpFetcher.cs.md) | 40 | 2 | The live HTTP transport for `WorkshopResolver`: a plain `HttpClient` with a short (15s) timeout and a friendly user agent. |
| [`ConfigTerminal/Model/DsInstance.cs`](descriptions/ConfigTerminal/Model/DsInstance.cs.md) | 121 | 2 | The aggregate root that binds a DS instance's cfg, worlds, templates and last-session together for the session. |
| [`ConfigTerminal/Model/EditSession.cs`](descriptions/ConfigTerminal/Model/EditSession.cs.md) | 190 | 2 | Dirty-tracking plus validation for one open config document. |
| [`ConfigTerminal/Model/HubCatalog.cs`](descriptions/ConfigTerminal/Model/HubCatalog.cs.md) | 179 | 2 | Reads Magnetar's cached plugin catalogs — the protobuf-net blobs Magnetar downloads into `Sources/Hubs/*.bin` (a `PluginData[]`) and `Sources/Plugins/*.bin` (a single-element `PluginData[]`). |
| [`ConfigTerminal/Model/Json/MiniJson.cs`](descriptions/ConfigTerminal/Model/Json/MiniJson.cs.md) | 223 | 1 | A tiny, self-contained JSON reader — enough to parse the Steam Web API responses the Workshop resolver consumes, with zero third-party dependencies. |
| [`ConfigTerminal/Model/LastSessionFile.cs`](descriptions/ConfigTerminal/Model/LastSessionFile.cs.md) | 111 | 2 | Read/write model for `Saves/LastSession.sbl` (`MyObjectBuilder_LastSession`), which selects the world the DS loads next. |
| [`ConfigTerminal/Model/MagnetarPlugins.cs`](descriptions/ConfigTerminal/Model/MagnetarPlugins.cs.md) | 399 | 1 | Facade over Magnetar's plugin config for one instance: the active profile (the enabled set) and the dev-folder sources, joined into UI-ready view rows. |
| [`ConfigTerminal/Model/ModList.cs`](descriptions/ConfigTerminal/Model/ModList.cs.md) | 50 | 2 | The per-world mod list model: a `ModItem` value type for one workshop mod and an ordered `ModList` with reorder and validation. |
| [`ConfigTerminal/Model/OptionModel.cs`](descriptions/ConfigTerminal/Model/OptionModel.cs.md) | 82 | 2 | Declares the small value types that describe one editable config option: the scope/kind/liveness enums, an `EnumChoice` record for one enum member, and the `OptionDefinition` record that is the declarative metadata driving the editor UI, serialization, validation and liveness hints. |
| [`ConfigTerminal/Model/OptionRegistry.cs`](descriptions/ConfigTerminal/Model/OptionRegistry.cs.md) | 431 | 1 | The single declarative source of truth for every editable DS config option, hand-transcribed from the decompiled `MyConfigDedicatedData` and `MyObjectBuilder_SessionSettings` (build 1.209.024) and cross-checked against Quasar's metadata. |
| [`ConfigTerminal/Model/PasswordHasher.cs`](descriptions/ConfigTerminal/Model/PasswordHasher.cs.md) | 50 | 2 | Reproduces the DS server-password hashing exactly, so a password set by this tool actually admits players: PBKDF2 (SHA1), 16-byte random salt, 10000 iterations, 20-byte derived key, both stored base64 as `ServerPasswordHash` / `ServerPasswordSalt`. |
| [`ConfigTerminal/Model/PluginManifest.cs`](descriptions/ConfigTerminal/Model/PluginManifest.cs.md) | 92 | 2 | Reads the display metadata a dev-folder plugin declares in its manifest XML — a `GitHubPlugin` serialized as `PluginData` (namespace `Pulsar.Shared.Data`). |
| [`ConfigTerminal/Model/PluginProfileDocument.cs`](descriptions/ConfigTerminal/Model/PluginProfileDocument.cs.md) | 249 | 1 | `XDocument` wrapper for a Magnetar plugin profile (`Profiles/<key>.xml`, root `Profile`), with `Current.xml` the active set the server loads. |
| [`ConfigTerminal/Model/PluginSourcesDocument.cs`](descriptions/ConfigTerminal/Model/PluginSourcesDocument.cs.md) | 299 | 1 | `XDocument` wrapper for `Sources/sources.xml` (root `SourcesConfig`), the registry of plugin catalog sources. |
| [`ConfigTerminal/Model/ProfileCatalog.cs`](descriptions/ConfigTerminal/Model/ProfileCatalog.cs.md) | 147 | 2 | Manages the instance's plugin *profiles* — named presets of enabled plugins stored as `Profiles/<Key>.xml`, with `Current.xml` the active set the server loads. |
| [`ConfigTerminal/Model/ProtoReader.cs`](descriptions/ConfigTerminal/Model/ProtoReader.cs.md) | 117 | 2 | A tiny, forward-only reader for the Protocol Buffers wire format — just enough to walk Magnetar's protobuf-net hub-catalog cache (`Sources/Hubs/*.bin`, `Sources/Plugins/*.bin`) by field number, without referencing `Shared`/protobuf-net or loading any Magnetar type. |
| [`ConfigTerminal/Model/WorkshopResolver.cs`](descriptions/ConfigTerminal/Model/WorkshopResolver.cs.md) | 259 | 1 | Looks up Steam Workshop mod metadata (friendly names, collection members) so the per-world mod-list editor can accept a Workshop URL or id and fill the name in automatically. |
| [`ConfigTerminal/Model/WorldCatalog.cs`](descriptions/ConfigTerminal/Model/WorldCatalog.cs.md) | 104 | 2 | Enumerates the worlds under a `Saves/` directory, building `WorldInfo` display metadata for each folder that holds a checkpoint and/or world config, sorted by last-save time descending. |
| [`ConfigTerminal/Model/WorldConfigDocument.cs`](descriptions/ConfigTerminal/Model/WorldConfigDocument.cs.md) | 166 | 2 | `XDocument` wrapper for a world's `Sandbox_config.sbc` (`MyObjectBuilder_WorldConfiguration`). |
| [`ConfigTerminal/Model/WorldCreator.cs`](descriptions/ConfigTerminal/Model/WorldCreator.cs.md) | 88 | 2 | Creates a new world by copying a DS world template (`Content/CustomWorlds/…`) into `Saves/` and stamping the chosen name into its `Sandbox_config.sbc` — no server start required. |
| [`ConfigTerminal/Model/WorldTemplateCatalog.cs`](descriptions/ConfigTerminal/Model/WorldTemplateCatalog.cs.md) | 114 | 2 | Enumerates the world templates the DS ships under `<ContentPath>/CustomWorlds/`, where ContentPath is the `Content/` folder sibling to `DedicatedServer64/`. |

## ConfigTerminal.Process  ·  [module doc](modules/ConfigTerminal.Process.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`ConfigTerminal/Process/LaunchSpec.cs`](descriptions/ConfigTerminal/Process/LaunchSpec.cs.md) | 71 | 2 | Builds the Magnetar daemon launch command line from an `InstanceBinding` and validates user-supplied extra arguments. |
| [`ConfigTerminal/Process/MagnetarProcess.cs`](descriptions/ConfigTerminal/Process/MagnetarProcess.cs.md) | 218 | 1 | Controls the single managed Magnetar DS instance: starts it daemonized, gracefully stops it (SIGTERM → save+quit), reloads live config (SIGHUP), force-kills it, and queries its status via the pid file. |
| [`ConfigTerminal/Process/PidFileReader.cs`](descriptions/ConfigTerminal/Process/PidFileReader.cs.md) | 152 | 2 | Reads and verifies the `magnetar.pid` file written by the launcher (spec §2.8), producing a `ServerStatus` snapshot. |
| [`ConfigTerminal/Process/ProcessMonitor.cs`](descriptions/ConfigTerminal/Process/ProcessMonitor.cs.md) | 39 | 2 | Polls the managed instance's status and raises `Changed` when it moves, so the UI can react to start/stop/foreign transitions. |
| [`ConfigTerminal/Process/ServerStatus.cs`](descriptions/ConfigTerminal/Process/ServerStatus.cs.md) | 44 | 2 | Defines the `ServerState` enum and the `ServerStatus` snapshot that carries the managed instance's process state across the module. |

## ConfigTerminal.Ui  ·  [module doc](modules/ConfigTerminal.Ui.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`ConfigTerminal/Ui/AccessListView.cs`](descriptions/ConfigTerminal/Ui/AccessListView.cs.md) | 151 | 2 | Editors for the dedicated config's Administrators / Banned / Reserved SteamID lists plus the GroupID field, laid out as three add/delete columns and one text field. |
| [`ConfigTerminal/Ui/AppShell.cs`](descriptions/ConfigTerminal/Ui/AppShell.cs.md) | 487 | 1 | The application shell for MagnetarConfig: a Terminal.Gui v1 `Toplevel` hosting a Turbo Vision desktop with a menu bar, an F-key status bar carrying the live server state, and a single swappable content panel. |
| [`ConfigTerminal/Ui/DashboardView.cs`](descriptions/ConfigTerminal/Ui/DashboardView.cs.md) | 110 | 2 | The home window: a live server-status line, a read-only text summary of the instance (paths, server name/ports/network/password, active world, world/template counts, and any warnings or problems), and the Start/Stop/Restart/Reload/Worlds/New-World controls that delegate to the shell. |
| [`ConfigTerminal/Ui/DesktopBackground.cs`](descriptions/ConfigTerminal/Ui/DesktopBackground.cs.md) | 31 | 2 | The classic Turbo Vision blue desktop backdrop: a non-focusable `View` that fills its bounds with the `▒` shade glyph and sits behind all content windows. |
| [`ConfigTerminal/Ui/Dialogs.cs`](descriptions/ConfigTerminal/Ui/Dialogs.cs.md) | 181 | 2 | Shared modal dialog helpers in the Turbo Vision look used across every view: info/error/confirm boxes, "details" dialogs that keep a centered question over a left-aligned detail block (so bullet lists aren't mangled by `MessageBox`'s per-line centering), a destructive confirm defaulting to the safe option, a text prompt with optional validation, and a background-work runner that keeps the UI live. |
| [`ConfigTerminal/Ui/FileDialogs.cs`](descriptions/ConfigTerminal/Ui/FileDialogs.cs.md) | 166 | 2 | Filesystem browse dialogs shared by the instance picker (path fields) and the dev-folder manifest picker. |
| [`ConfigTerminal/Ui/HelpDialog.cs`](descriptions/ConfigTerminal/Ui/HelpDialog.cs.md) | 25 | 3 | The About/help modal. |
| [`ConfigTerminal/Ui/HubPluginsView.cs`](descriptions/ConfigTerminal/Ui/HubPluginsView.cs.md) | 196 | 2 | Browses the plugins offered by the instance's configured hub/remote sources — read offline from Magnetar's cached catalogs under `Sources/Hubs` and `Sources/Plugins` — plus the registered dev folders (shown with a "- dev folder" suffix), and enables/disables them in the active profile. |
| [`ConfigTerminal/Ui/IAutoSaveContent.cs`](descriptions/ConfigTerminal/Ui/IAutoSaveContent.cs.md) | 23 | 3 | The contract for a hosted panel that persists its edits automatically. |
| [`ConfigTerminal/Ui/InstancePickerDialog.cs`](descriptions/ConfigTerminal/Ui/InstancePickerDialog.cs.md) | 96 | 2 | Modal dialog that prompts for the folder pair (and launcher / DS install) identifying an instance — the DS data dir, Magnetar config dir, launcher executable, and DS install — each with a Browse button. |
| [`ConfigTerminal/Ui/LogViewerView.cs`](descriptions/ConfigTerminal/Ui/LogViewerView.cs.md) | 588 | 1 | Read-only log viewer over the game and Magnetar log files, with a `tail -f` follow mode, optional word-wrap, incremental text search, and keyword highlighting. |
| [`ConfigTerminal/Ui/ManifestPicker.cs`](descriptions/ConfigTerminal/Ui/ManifestPicker.cs.md) | 17 | 3 | Quasar-style dev-folder picker: browse the filesystem and select a plugin's `.xml` manifest file, opening at the last-visited folder so adding several plugins in a row is frictionless. |
| [`ConfigTerminal/Ui/ModListView.cs`](descriptions/ConfigTerminal/Ui/ModListView.cs.md) | 219 | 1 | Ordered mod-list editor for a world's `Sandbox_config.sbc`: add (by Workshop id or URL, with background friendly-name resolution), delete, reorder, and toggle a mod's dependency flag. |
| [`ConfigTerminal/Ui/NewWorldWizard.cs`](descriptions/ConfigTerminal/Ui/NewWorldWizard.cs.md) | 158 | 2 | New-world creation by folder copy: pick a template, name the world, then copy the template into `Saves/<name>` and stamp the name into its `Sandbox_config.sbc` via `WorldCreator`. |
| [`ConfigTerminal/Ui/OptionFormView.cs`](descriptions/ConfigTerminal/Ui/OptionFormView.cs.md) | 439 | 1 | The generic, registry-driven settings form used for the DS global config, the new-world defaults, and each world's settings. |
| [`ConfigTerminal/Ui/PasswordDialog.cs`](descriptions/ConfigTerminal/Ui/PasswordDialog.cs.md) | 57 | 2 | Modal dialog to set or clear the server password. |
| [`ConfigTerminal/Ui/PluginSourcesView.cs`](descriptions/ConfigTerminal/Ui/PluginSourcesView.cs.md) | 158 | 2 | Manages the instance's plugin catalog *sources* — remote GitHub hubs (e.g. |
| [`ConfigTerminal/Ui/PluginsView.cs`](descriptions/ConfigTerminal/Ui/PluginsView.cs.md) | 203 | 1 | Manages the Magnetar instance's local plugin sources: a left pane of local DLLs from the `Local/` folder (Space toggles enabled) and a right pane of registered dev folders added Quasar-style by picking a manifest XML. |
| [`ConfigTerminal/Ui/ProfilesView.cs`](descriptions/ConfigTerminal/Ui/ProfilesView.cs.md) | 173 | 2 | Manages plugin *profiles* — named presets of the enabled-plugin set stored as `Profiles/<Key>.xml`, with `Current.xml` the active set the server loads. |
| [`ConfigTerminal/Ui/TurboVisionTheme.cs`](descriptions/ConfigTerminal/Ui/TurboVisionTheme.cs.md) | 73 | 2 | The classic 16-color Turbo Vision / Turbo Pascal 7 IDE palette expressed as Terminal.Gui v1 `ColorScheme`s. |
| [`ConfigTerminal/Ui/WorldsView.cs`](descriptions/ConfigTerminal/Ui/WorldsView.cs.md) | 246 | 1 | Lists the worlds found under `Saves/` and offers per-world settings and mod editing, activation, creation, and deletion. |

## ConfigTerminalTests  ·  [module doc](modules/ConfigTerminalTests.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`ConfigTerminalTests/DocumentTests.cs`](descriptions/ConfigTerminalTests/DocumentTests.cs.md) | 123 | 2 | xUnit tests for the config-document round-trip layer — `DedicatedConfigDocument` and `WorldConfigDocument` — proving edits are surgical and format-faithful. |
| [`ConfigTerminalTests/HubCatalogTests.cs`](descriptions/ConfigTerminalTests/HubCatalogTests.cs.md) | 46 | 2 | xUnit tests for `HubCatalog`, the protobuf-net reader that decodes a MagnetarHub catalog cache (`PluginData[]`) into browsable `HubPluginInfo` rows. |
| [`ConfigTerminalTests/LiveEndToEndTests.cs`](descriptions/ConfigTerminalTests/LiveEndToEndTests.cs.md) | 156 | 2 | Live end-to-end test of the real create → start → "Game ready" → stop flow against an installed dedicated server plus a patched Magnetar launcher, exercising the exact model/process code paths the New-World wizard drives. |
| [`ConfigTerminalTests/LogHighlightTests.cs`](descriptions/ConfigTerminalTests/LogHighlightTests.cs.md) | 34 | 2 | Unit tests for `LogHighlight.Classify`, the log-viewer line classifier. |
| [`ConfigTerminalTests/PluginConfigTests.cs`](descriptions/ConfigTerminalTests/PluginConfigTests.cs.md) | 254 | 1 | Comprehensive xUnit suite for the plugin/profile/sources model and the `MagnetarPlugins` facade, proving that enabling/disabling plugins is a surgical upsert that never clobbers unmanaged siblings. |
| [`ConfigTerminalTests/PluginInteropTests.cs`](descriptions/ConfigTerminalTests/PluginInteropTests.cs.md) | 237 | 1 | Interop tests proving that profile/sources XML written by this tool is accepted by Magnetar's own serializers. |
| [`ConfigTerminalTests/ProcessAndFileTests.cs`](descriptions/ConfigTerminalTests/ProcessAndFileTests.cs.md) | 187 | 2 | xUnit tests for the process/pid/atomic-file and world-creation layer. |
| [`ConfigTerminalTests/ProfileCatalogTests.cs`](descriptions/ConfigTerminalTests/ProfileCatalogTests.cs.md) | 129 | 2 | xUnit tests for `ProfileCatalog`, which manages named plugin profiles derived from the active `Current` set. |
| [`ConfigTerminalTests/RegistryTests.cs`](descriptions/ConfigTerminalTests/RegistryTests.cs.md) | 66 | 2 | xUnit tests asserting the structural invariants of `OptionRegistry` — the static table of dedicated/session config options the TUI edits. |
| [`ConfigTerminalTests/UiSmokeTests.cs`](descriptions/ConfigTerminalTests/UiSmokeTests.cs.md) | 356 | 1 | Headless UI tests that build the `AppShell` view tree against Terminal.Gui's `FakeDriver` and pump a few main-loop iterations, catching constructor/layout exceptions without a real terminal — plus focused coverage of the log viewer's behaviour. |
| [`ConfigTerminalTests/WorkshopResolverTests.cs`](descriptions/ConfigTerminalTests/WorkshopResolverTests.cs.md) | 178 | 2 | xUnit tests for `WorkshopResolver`, which turns Steam Workshop ids/URLs into mod names via the Steam Web API. |

## Legacy.Commands  ·  [module doc](modules/Legacy.Commands.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Commands/CommandService.cs`](descriptions/Legacy/Commands/CommandService.cs.md) | 114 | 2 | `CommandService` is the host-side owner of the chat-command pipeline for the Legacy (.NET Framework 4.8 / Windows) build of Magnetar. |
| [`Legacy/Commands/MagnetarCommands.cs`](descriptions/Legacy/Commands/MagnetarCommands.cs.md) | 92 | 2 | Declares four built-in chat-command modules — `!save`, `!restart`, `!quit`, and `!stop` — that Magnetar registers with `CommandService` before any plugin loads. |
| [`Legacy/Commands/ServerCommandResponder.cs`](descriptions/Legacy/Commands/ServerCommandResponder.cs.md) | 37 | 2 | `ServerCommandResponder` is the `ICommandResponder` implementation that delivers command replies into the SE DS chat system. |

## Legacy.Integration  ·  [module doc](modules/Legacy.Integration.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Compiler/Interim.cs`](descriptions/Legacy/Compiler/Interim.cs.md) | 147 | 2 | Active only under `#if NETCOREAPP` (the Interim/.NET 10 build). |
| [`Legacy/Compiler/Legacy.cs`](descriptions/Legacy/Compiler/Legacy.cs.md) | 86 | 2 | Active only under `#if NETFRAMEWORK` (the .NET Framework 4.8 / Windows build). |
| [`Legacy/Compiler/References.cs`](descriptions/Legacy/Compiler/References.cs.md) | 36 | 2 | Provides the list of assembly references that the Roslyn compiler must know about when compiling SE scripts and plugins. |
| [`Legacy/Extensions/ModPlugin.cs`](descriptions/Legacy/Extensions/ModPlugin.cs.md) | 31 | 2 | Extends `ModPlugin` (the Magnetar data type representing a Steam Workshop mod) with the SE DS API objects needed to register a mod with the game engine at runtime. |
| [`Legacy/Integration/MissionScreenSender.cs`](descriptions/Legacy/Integration/MissionScreenSender.cs.md) | 142 | 2 | Host-side sender that delivers plugin-declared mission-screen popups to clients over Space Engineers' multiplayer messaging API. |
| [`Legacy/Paths/PathResolverBinder.cs`](descriptions/Legacy/Paths/PathResolverBinder.cs.md) | 77 | 2 | Wires the `PluginSdk.Paths.PathResolver` facade to the LinuxCompat plugin's case-insensitive path cache at startup. |
| [`Legacy/Paths/ReflectionPathResolver.cs`](descriptions/Legacy/Paths/ReflectionPathResolver.cs.md) | 94 | 2 | An `IPathResolver` backend that forwards path operations to the LinuxCompat plugin's `PathHelpers` and `PathCache` static methods via pre-bound delegates. |

## Legacy.Launcher  ·  [module doc](modules/Legacy.Launcher.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Launcher/Daemon.cs`](descriptions/Legacy/Launcher/Daemon.cs.md) | 164 | 2 | Detaches the running process from its parent (typically Quasar) when the `-daemon` flag is set, so the parent terminating does not take the dedicated server down with it. |
| [`Legacy/Launcher/Folder.cs`](descriptions/Legacy/Launcher/Folder.cs.md) | 161 | 2 | Locates the Space Engineers Dedicated Server `DedicatedServer64` installation directory so the launcher knows which game binaries to load and patch. |
| [`Legacy/Launcher/Game.cs`](descriptions/Legacy/Launcher/Game.cs.md) | 141 | 2 | Thin bridge between Magnetar's launcher and the Space Engineers DS engine internals (`Sandbox`, `VRage`). |
| [`Legacy/Launcher/PidFile.cs`](descriptions/Legacy/Launcher/PidFile.cs.md) | 79 | 2 | Writes and removes `magnetar.pid` in the Magnetar config directory so an external tool (MagnetarConfig) can discover this dedicated-server instance and verify the running process belongs to it. |
| [`Legacy/Launcher/ServerControl.cs`](descriptions/Legacy/Launcher/ServerControl.cs.md) | 529 | 1 | Single source of truth for the dedicated server's lifecycle operations — save world, reload dedicated config, quit, and restart — with and without saving. |
| [`Legacy/Program.cs`](descriptions/Legacy/Program.cs.md) | 486 | 1 | Entry point for the Magnetar launcher. |

## Legacy.Loader  ·  [module doc](modules/Legacy.Loader.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Loader/LoaderTools.cs`](descriptions/Legacy/Loader/LoaderTools.cs.md) | 137 | 2 | Process-level utilities for the loader: restarting the dedicated server process with adjusted command-line arguments, and force-precompiling (JIT-preparing) plugin assemblies so member-access errors surface immediately instead of mid-game. |
| [`Legacy/Loader/MagnetarClientMod.cs`](descriptions/Legacy/Loader/MagnetarClientMod.cs.md) | 102 | 2 | Manages the bundled **MagnetarMod** client companion world mod (Steam workshop id `3750200326`), the script-side counterpart clients must load so that plugin-driven mission-screen popups have receiving code. |
| [`Legacy/Loader/NativeLibraryPreloader.cs`](descriptions/Legacy/Loader/NativeLibraryPreloader.cs.md) | 154 | 1 | Linux-only native-library bootstrap that runs once at the very top of `Main()`. |
| [`Legacy/Loader/PluginInstance.cs`](descriptions/Legacy/Loader/PluginInstance.cs.md) | 336 | 1 | Runtime wrapper around a single loaded plugin: it locates the plugin's `IPlugin` implementation type in the compiled assembly, instantiates it, performs reflection-based dependency injection of loader services into well-known static fields/methods, and drives the SE plugin lifecycle (`Init` / `Update` / `HandleInput` / `Dispose`). |
| [`Legacy/Loader/PluginLoader.cs`](descriptions/Legacy/Loader/PluginLoader.cs.md) | 229 | 1 | The top-level plugin host: a singleton `IHandleInputPlugin` that SE itself drives (`Init`/`Update`/`HandleInput`/`Dispose`). |
| [`Legacy/Loader/SteamMods.cs`](descriptions/Legacy/Loader/SteamMods.cs.md) | 120 | 2 | Downloads/updates Steam Workshop items (mod-plugins referenced by the active profile) by reproducing SE's own blocking workshop-download path. |

## Legacy.Patch  ·  [module doc](modules/Legacy.Patch.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Patch/Patch_Compile.cs`](descriptions/Legacy/Patch/Patch_Compile.cs.md) | 65 | 2 | Postfix-patches `MyScriptCompiler.AnalyzeDiagnostics` to intercept Roslyn compilation failures before they reach SE's own error pipeline. |
| [`Legacy/Patch/Patch_ComponentRegistered.cs`](descriptions/Legacy/Patch/Patch_ComponentRegistered.cs.md) | 20 | 3 | Prefix-patches `MySession.RegisterComponentsFromAssembly` to inject plugin-provided session components at exactly the right moment in the SE session lifecycle. |
| [`Legacy/Patch/Patch_DedicatedServerRun.cs`](descriptions/Legacy/Patch/Patch_DedicatedServerRun.cs.md) | 78 | 2 | Transpiler-patches `VRage.Dedicated.DedicatedServer.Run` to remove the Telerik/WinForms configuration UI and the Windows Service branch, replacing the entire method body with a minimal headless startup sequence. |
| [`Legacy/Patch/Patch_ExitThreadSafe.cs`](descriptions/Legacy/Patch/Patch_ExitThreadSafe.cs.md) | 20 | 3 | Prefix-patches `MySandboxGame.ExitThreadSafe` to redirect in-game and admin-triggered exit requests through Magnetar's graceful shutdown path. |
| [`Legacy/Patch/Patch_LoadScripts.cs`](descriptions/Legacy/Patch/Patch_LoadScripts.cs.md) | 17 | 3 | Postfix-patches `MyScriptManager.LoadScripts` to trigger plugin entity-component registration at the correct point in session startup. |
| [`Legacy/Patch/Patch_MyDefinitionErrors.cs`](descriptions/Legacy/Patch/Patch_MyDefinitionErrors.cs.md) | 40 | 2 | Prefix-patches `MyDefinitionErrors.Add` to intercept Roslyn compilation-failure error messages and redirect them to Magnetar's own log, replacing SE's raw, path-cluttered error string with a cleaner structured output that pairs the mod name with the per-diagnostic messages already collected by `Patch_Compile`. |
| [`Legacy/Patch/Patch_MyDefinitionManager.cs`](descriptions/Legacy/Patch/Patch_MyDefinitionManager.cs.md) | 45 | 2 | Prefix-patches `MyDefinitionManager.LoadData` to augment SE's mod list before definitions are loaded. |
| [`Legacy/Patch/Patch_MyScriptManager.cs`](descriptions/Legacy/Patch/Patch_MyScriptManager.cs.md) | 78 | 2 | Postfix-patches `MyScriptManager.LoadData` to compile and load scripts for client-side `ModPlugin` entries after SE has processed all normal session mods. |
| [`Legacy/Patch/Patch_MySessionLoader.cs`](descriptions/Legacy/Patch/Patch_MySessionLoader.cs.md) | 38 | 2 | Contains two Harmony Prefix patches on `MySessionLoader.LoadMultiplayerScenarioWorld` and `MySessionLoader.LoadMultiplayerSession`. |
| [`Legacy/Patch/Patch_MyWorkshop.cs`](descriptions/Legacy/Patch/Patch_MyWorkshop.cs.md) | 27 | 2 | `Patch_MyWorkshop` intercepts SE's `MyWorkshop.DownloadWorldModsBlocking` path. |
| [`Legacy/Patch/Patch_PrepareCrashReport.cs`](descriptions/Legacy/Patch/Patch_PrepareCrashReport.cs.md) | 44 | 2 | Prefix-patches `VRage.Platform.Windows.MyCrashReporting.PrepareCrashAnalyticsReporting` to redirect the SE crash reporter to run the correct `SpaceEngineers.exe` binary, which in Magnetar's in-process hosting model is not necessarily the process that crashed. |
| [`Legacy/Patch/Patch_ServerChat.cs`](descriptions/Legacy/Patch/Patch_ServerChat.cs.md) | 56 | 2 | Prefix-patches `MyMultiplayerBase.OnChatMessageReceived_Server` to intercept player-typed chat before SE relays it. |

## MagnetarMod  ·  [module doc](modules/MagnetarMod.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`MagnetarMod/src/Data/Scripts/MagnetarMod/MagnetarModSession.cs`](descriptions/MagnetarMod/src/Data/Scripts/MagnetarMod/MagnetarModSession.cs.md) | 114 | 2 | Client-side Space Engineers world-mod session component that receives server-pushed mission-screen popups and renders them through the SE ModAPI. |

## PluginSdk.Commands  ·  [module doc](modules/PluginSdk.Commands.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdk/Commands/ArgumentBinder.cs`](descriptions/PluginSdk/Commands/ArgumentBinder.cs.md) | 155 | 2 | `ArgumentBinder` converts the ordered list of string tokens produced by `CommandLine.Tokenize` into the typed `object[]` array expected by a handler's `MethodInfo.Invoke` call. |
| [`PluginSdk/Commands/CommandAttribute.cs`](descriptions/PluginSdk/Commands/CommandAttribute.cs.md) | 54 | 2 | `CommandAttribute` is the marker that turns a public instance method of a `CommandModule` subclass into a chat command handler. |
| [`PluginSdk/Commands/CommandCaller.cs`](descriptions/PluginSdk/Commands/CommandCaller.cs.md) | 37 | 2 | `CommandCaller` is an immutable snapshot of the identity and permission level of the player (or server console) who issued a chat command. |
| [`PluginSdk/Commands/CommandContext.cs`](descriptions/PluginSdk/Commands/CommandContext.cs.md) | 55 | 2 | `CommandContext` is the per-invocation environment that a command handler accesses through `CommandModule.Context`. |
| [`PluginSdk/Commands/CommandDispatcher.cs`](descriptions/PluginSdk/Commands/CommandDispatcher.cs.md) | 308 | 1 | `CommandDispatcher` is the main entry point for chat message processing. |
| [`PluginSdk/Commands/CommandLine.cs`](descriptions/PluginSdk/Commands/CommandLine.cs.md) | 69 | 2 | `CommandLine` provides a single `Tokenize` method that splits a raw chat string (with the leading `!` already stripped) into an ordered `List<string>` of tokens. |
| [`PluginSdk/Commands/CommandModule.cs`](descriptions/PluginSdk/Commands/CommandModule.cs.md) | 21 | 3 | `CommandModule` is the plugin-facing base class for a group of chat commands. |
| [`PluginSdk/Commands/CommandRegistrationException.cs`](descriptions/PluginSdk/Commands/CommandRegistrationException.cs.md) | 15 | 3 | `CommandRegistrationException` is the specific exception thrown by `CommandRegistry` when a module fails to register — for example when the `[CommandRoot]` prefix is already claimed by a different plugin, the prefix is the reserved word `"help"`, a command path starts with the reserved word `"help"`, or the prefix string is empty or contains spaces. |
| [`PluginSdk/Commands/CommandRegistry.cs`](descriptions/PluginSdk/Commands/CommandRegistry.cs.md) | 124 | 2 | `CommandRegistry` is the authoritative store of all registered chat commands, keyed by root prefix. |
| [`PluginSdk/Commands/CommandReply.cs`](descriptions/PluginSdk/Commands/CommandReply.cs.md) | 70 | 2 | `CommandReply` is the value type that carries a fully-specified chat message from a command handler back to the host's `ICommandResponder`. |
| [`PluginSdk/Commands/CommandRoot.cs`](descriptions/PluginSdk/Commands/CommandRoot.cs.md) | 133 | 2 | `CommandRoot` owns the trie-like tree of commands registered under one `!prefix` namespace. |
| [`PluginSdk/Commands/CommandRootAttribute.cs`](descriptions/PluginSdk/Commands/CommandRootAttribute.cs.md) | 49 | 2 | `CommandRootAttribute` declares the `!prefix` namespace that a `CommandModule` subclass contributes to. |
| [`PluginSdk/Commands/ICommandRegistrar.cs`](descriptions/PluginSdk/Commands/ICommandRegistrar.cs.md) | 26 | 2 | `ICommandRegistrar` is the host-implemented sink through which plugins register their command modules. |
| [`PluginSdk/Commands/ICommandResponder.cs`](descriptions/PluginSdk/Commands/ICommandResponder.cs.md) | 18 | 3 | `ICommandResponder` is the abstraction between the command dispatch pipeline and the actual SE DS chat API. |
| [`PluginSdk/Commands/PermissionAttribute.cs`](descriptions/PluginSdk/Commands/PermissionAttribute.cs.md) | 28 | 2 | `PermissionAttribute` sets the minimum `MyPromoteLevel` a player must hold to invoke the decorated command. |
| [`PluginSdk/Commands/RegisteredCommand.cs`](descriptions/PluginSdk/Commands/RegisteredCommand.cs.md) | 78 | 2 | `RegisteredCommand` is the internal representation of a single chat command as resolved from a `[Command]`-decorated method. |
| [`PluginSdk/Commands/ServerCommands.cs`](descriptions/PluginSdk/Commands/ServerCommands.cs.md) | 49 | 2 | `ServerCommands` is the plugin-facing static facade for command registration, analogous to how `Harmony.PatchAll(Assembly)` is the entry point for Harmony patches. |

## PluginSdk.Config  ·  [module doc](modules/PluginSdk.Config.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdk/Config/ConfigAttributes.cs`](descriptions/PluginSdk/Config/ConfigAttributes.cs.md) | 412 | 1 | Declares the full attribute vocabulary a plugin uses to annotate a `PluginConfig`-derived class so Magnetar can discover, validate, remotely manage and lay out each configuration option in an external Web UI (rendered by the manager app, e.g. |
| [`PluginSdk/Config/ConfigSchema.cs`](descriptions/PluginSdk/Config/ConfigSchema.cs.md) | 550 | 1 | Reflection-based schema extractor that turns a `PluginConfig`-derived type into a `ConfigSchemaData` document describing its layout tree, options, nested struct definitions and enum definitions. |
| [`PluginSdk/Config/ConfigStorage.cs`](descriptions/PluginSdk/Config/ConfigStorage.cs.md) | 158 | 2 | Save/load facade for `PluginConfig`-derived instances in two formats. **XML** is the local on-disk format: written atomically via a temp file + rename, emitting only non-default values (the sparse format is driven by `PluginConfig`'s `IXmlSerializable` implementation), so missing elements fall back to defaults on load. **JSON** is the remote management wire format — a three-part envelope of `schema` (from `ConfigSchema.Build`), `defaults` (a fresh instance) and `values` (the current config); loading reads only `values` while regenerating schema/defaults on every save. |
| [`PluginSdk/Config/PluginConfig.cs`](descriptions/PluginSdk/Config/PluginConfig.cs.md) | 299 | 1 | Abstract base class for managed plugin configuration. |
| [`PluginSdk/Config/TypeSerialization.cs`](descriptions/PluginSdk/Config/TypeSerialization.cs.md) | 413 | 1 | Bespoke XML read/write helpers and `System.Text.Json` converters for the small set of VRage value types that are first-class configuration values: `Color`, `Vector2D`, `Vector3D`, `Vector2I`, `Vector3I`, `Base6Directions.Direction` and `MyPositionAndOrientation`. |

## PluginSdk.Logging  ·  [module doc](modules/PluginSdk.Logging.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdk/Logging/ILogSink.cs`](descriptions/PluginSdk/Logging/ILogSink.cs.md) | 19 | 3 | Defines the single-method contract that every log destination must satisfy. |
| [`PluginSdk/Logging/LogEntry.cs`](descriptions/PluginSdk/Logging/LogEntry.cs.md) | 48 | 2 | A single immutable log record that is passed by `in` reference from `Logger` to `ILogSink`. |
| [`PluginSdk/Logging/LogEnvironment.cs`](descriptions/PluginSdk/Logging/LogEnvironment.cs.md) | 60 | 2 | Acts as the environment probe that decides which `ILogSink` the SDK uses. |
| [`PluginSdk/Logging/LogJson.cs`](descriptions/PluginSdk/Logging/LogJson.cs.md) | 51 | 2 | Centralises `System.Text.Json` configuration and serialization helpers so both `MagnetarLogSink` and `QuasarLogSink` produce identical JSON shapes for the optional structured `data` payload. |
| [`PluginSdk/Logging/LogLevel.cs`](descriptions/PluginSdk/Logging/LogLevel.cs.md) | 16 | 3 | Declares the severity levels used throughout the SDK logging subsystem. |
| [`PluginSdk/Logging/Logger.cs`](descriptions/PluginSdk/Logging/Logger.cs.md) | 84 | 2 | The primary logging facade a plugin holds as a `static readonly` field. |
| [`PluginSdk/Logging/MagnetarLogSink.cs`](descriptions/PluginSdk/Logging/MagnetarLogSink.cs.md) | 58 | 2 | The `ILogSink` used when the server runs under standalone Magnetar (no Quasar Agent). |
| [`PluginSdk/Logging/QuasarLogSink.cs`](descriptions/PluginSdk/Logging/QuasarLogSink.cs.md) | 89 | 2 | The `ILogSink` used when the server process is managed by the Quasar Agent. |

## PluginSdk.Runtime  ·  [module doc](modules/PluginSdk.Runtime.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdk/MissionScreenContent.cs`](descriptions/PluginSdk/MissionScreenContent.cs.md) | 35 | 2 | Immutable value type carrying the text payload that the Magnetar client mod renders through Space Engineers' mission-screen popup. |
| [`PluginSdk/MissionScreens.cs`](descriptions/PluginSdk/MissionScreens.cs.md) | 95 | 2 | Plugin-facing facade for opening Space Engineers mission-screen popups on connected clients from server-side plugin code, decoupled from the host launcher implementation. |
| [`PluginSdk/Paths/IPathResolver.cs`](descriptions/PluginSdk/Paths/IPathResolver.cs.md) | 48 | 2 | Defines the backend contract for cross-platform, case-insensitive path resolution. |
| [`PluginSdk/Paths/PathResolver.cs`](descriptions/PluginSdk/Paths/PathResolver.cs.md) | 48 | 2 | Plugin-facing static facade for cross-platform, case-insensitive path resolution. |
| [`PluginSdk/Paths/ShimPathResolver.cs`](descriptions/PluginSdk/Paths/ShimPathResolver.cs.md) | 36 | 2 | Default, no-op implementation of `IPathResolver` used when the server is running on a case-insensitive filesystem (Windows) or when no real case-insensitive backend has been installed yet. |
| [`PluginSdk/ServerControl.cs`](descriptions/PluginSdk/ServerControl.cs.md) | 142 | 2 | Exposes the dedicated server's lifecycle controls (save, reload config, quit, restart) as a stable plugin-facing API, decoupled from the host launcher implementation. |
| [`PluginSdk/Tools/SerializableDictionary.cs`](descriptions/PluginSdk/Tools/SerializableDictionary.cs.md) | 80 | 2 | Provides a generic dictionary that can be round-tripped by `XmlSerializer`, which cannot handle the standard `Dictionary<TKey, TValue>`. |

## PluginSdk.Stats  ·  [module doc](modules/PluginSdk.Stats.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdk/Stats/PluginStats.cs`](descriptions/PluginSdk/Stats/PluginStats.cs.md) | 90 | 2 | The process-wide publish/subscribe hub for plugin statistics: a producer publishes a self-describing `StatsSnapshot` under a provider name, and a consumer reads the latest snapshot by name, lists the active providers, or subscribes to `Updated` to receive every publication as it happens. |
| [`PluginSdk/Stats/StatsAttributes.cs`](descriptions/PluginSdk/Stats/StatsAttributes.cs.md) | 183 | 2 | The attribute vocabulary a plugin uses to annotate a stats POCO, plus the two enums that describe how each value aggregates. |
| [`PluginSdk/Stats/StatsSchema.cs`](descriptions/PluginSdk/Stats/StatsSchema.cs.md) | 172 | 2 | Reflection-based schema for a stats POCO, plus the capture routines that project live POCO instances into the serializable `StatInstance` and `StatGroup` shapes. |
| [`PluginSdk/Stats/StatsSnapshot.cs`](descriptions/PluginSdk/Stats/StatsSnapshot.cs.md) | 53 | 2 | The serializable payload a producer publishes through `PluginStats`: a timestamped tree of groups, each pairing a schema with the instances captured against it. |

## PluginSdkTests  ·  [module doc](modules/PluginSdkTests.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdkTests/ChangeNotificationTests.cs`](descriptions/PluginSdkTests/ChangeNotificationTests.cs.md) | 257 | 2 | Verifies the change-notification contract of `PluginConfig` — the base class for all Magnetar plugin configuration objects. |
| [`PluginSdkTests/CommandTests.cs`](descriptions/PluginSdkTests/CommandTests.cs.md) | 470 | 2 | Comprehensive specification for the PluginSdk chat-command pipeline: `CommandRegistry`, `CommandDispatcher`, `CommandModule`, `CommandCaller`, `CommandReply`, `ICommandResponder`, and the associated attributes (`CommandRoot`, `Command`, `Permission`). |
| [`PluginSdkTests/LoggingTests.cs`](descriptions/PluginSdkTests/LoggingTests.cs.md) | 198 | 2 | Specifies the PluginSdk logging subsystem: `Logger`, `LogEntry`, `ILogSink`, `LogLevel`, `QuasarLogSink`, `MagnetarLogSink`, and `LogEnvironment`. |
| [`PluginSdkTests/PathResolverTests.cs`](descriptions/PluginSdkTests/PathResolverTests.cs.md) | 88 | 2 | Specifies the `PathResolver` façade and its `IPathResolver` plug-in point. |
| [`PluginSdkTests/SchemaTests.cs`](descriptions/PluginSdkTests/SchemaTests.cs.md) | 525 | 2 | Specifies the schema and JSON-envelope subsystems of `PluginSdk.Config`. |
| [`PluginSdkTests/SerializationTests.cs`](descriptions/PluginSdkTests/SerializationTests.cs.md) | 464 | 2 | End-to-end round-trip and format-pinning tests for `PluginSdk.Config` serialisation. |
| [`PluginSdkTests/ServerControlTests.cs`](descriptions/PluginSdkTests/ServerControlTests.cs.md) | 62 | 2 | Specifies the `ServerControl` static façade that plugins call to trigger server lifecycle operations (save, reload config, quit, restart). |
| [`PluginSdkTests/TestConfig.cs`](descriptions/PluginSdkTests/TestConfig.cs.md) | 197 | 2 | Defines the shared fixture types used across all PluginSdkTests test classes. |

## Shared.Config  ·  [module doc](modules/Shared.Config.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/Config/ConfigManager.cs`](descriptions/Shared/Config/ConfigManager.cs.md) | 90 | 2 | `ConfigManager` is the singleton root of all runtime configuration for Magnetar. |
| [`Shared/Config/CoreConfig.cs`](descriptions/Shared/Config/CoreConfig.cs.md) | 74 | 2 | `CoreConfig` persists the fundamental installation-level settings to `config.xml` in the Pulsar/Magnetar data directory. |
| [`Shared/Config/GitHubPluginConfig.cs`](descriptions/Shared/Config/GitHubPluginConfig.cs.md) | 6 | 3 | `GitHubPluginConfig` is the per-plugin configuration record stored inside a `Profile` for plugins sourced from GitHub releases. |
| [`Shared/Config/LocalFolderConfig.cs`](descriptions/Shared/Config/LocalFolderConfig.cs.md) | 7 | 3 | `LocalFolderConfig` is the per-plugin configuration record stored inside a `Profile` for plugins sourced from a local development folder (the "DevFolder" feature). |
| [`Shared/Config/PluginDataConfig.cs`](descriptions/Shared/Config/PluginDataConfig.cs.md) | 10 | 3 | `PluginDataConfig` is the abstract base for per-plugin configuration records that are embedded in a `Profile`. |
| [`Shared/Config/ProfilesConfig.cs`](descriptions/Shared/Config/ProfilesConfig.cs.md) | 156 | 2 | `ProfilesConfig` manages the on-disk lifecycle of named plugin-enable profiles. |
| [`Shared/Config/Sources/LocalHubConfig.cs`](descriptions/Shared/Config/Sources/LocalHubConfig.cs.md) | 9 | 3 | `LocalHubConfig` is the configuration record for a locally-stored plugin hub — a directory on the filesystem that acts as a hub catalogue. |
| [`Shared/Config/Sources/LocalPluginConfig.cs`](descriptions/Shared/Config/Sources/LocalPluginConfig.cs.md) | 8 | 3 | `LocalPluginConfig` is the configuration record for a plugin that is installed directly from a local filesystem folder, without going through a hub or GitHub. |
| [`Shared/Config/Sources/ModConfig.cs`](descriptions/Shared/Config/Sources/ModConfig.cs.md) | 8 | 3 | `ModConfig` is the configuration record for a Steam Workshop mod source. |
| [`Shared/Config/Sources/RemoteHubConfig.cs`](descriptions/Shared/Config/Sources/RemoteHubConfig.cs.md) | 14 | 3 | `RemoteHubConfig` is the configuration record for a GitHub-hosted plugin hub. |
| [`Shared/Config/Sources/RemotePluginConfig.cs`](descriptions/Shared/Config/Sources/RemotePluginConfig.cs.md) | 14 | 3 | `RemotePluginConfig` is the configuration record for a GitHub-hosted plugin that is registered directly as a source (not via a hub). |
| [`Shared/Config/SourcesConfig.cs`](descriptions/Shared/Config/SourcesConfig.cs.md) | 134 | 2 | `SourcesConfig` is the XML-serialised registry of all plugin and mod sources available to Magnetar. |

## Shared.Core  ·  [module doc](modules/Shared.Core.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/AssemblyResolver.cs`](descriptions/Shared/AssemblyResolver.cs.md) | 107 | 2 | Provides a scoped `AppDomain.AssemblyResolve` handler that satisfies managed assembly load requests from one or more "source" folders, but only when the *requesting* assembly is on an allow-list. |
| [`Shared/Flags.cs`](descriptions/Shared/Flags.cs.md) | 171 | 2 | Parses Magnetar's own command-line switches once at startup (in a static constructor) and exposes them as read-only boolean/string/enum flags for the rest of the loader. |
| [`Shared/Launcher.cs`](descriptions/Shared/Launcher.cs.md) | 52 | 2 | Performs pre-launch sanity checks before Magnetar starts the SE Dedicated Server: refuses to start if the SE process is already running, rejects the removed `-plugin` switch, and verifies that an app `.config` exists when the SE folder ships one. |
| [`Shared/Loader.cs`](descriptions/Shared/Loader.cs.md) | 156 | 2 | The orchestrator that instantiates all enabled plugins at startup. |
| [`Shared/LogFile.cs`](descriptions/Shared/LogFile.cs.md) | 130 | 2 | Magnetar's central logging facade writing per-start `info_*.log` files. |
| [`Shared/PluginList.cs`](descriptions/Shared/PluginList.cs.md) | 868 | 1 | The plugin catalog. |
| [`Shared/PluginProgress.cs`](descriptions/Shared/PluginProgress.cs.md) | 45 | 2 | Plain-text console progress reporter for plugin download and compilation, replacing the former WinForms splash screen that does not exist on the headless DS. |
| [`Shared/Preloader.cs`](descriptions/Shared/Preloader.cs.md) | 225 | 1 | Implements Magnetar's "preloader plugin" mechanism: BepInEx/Pulsar-style assembly patching of SE DS DLLs *on disk* before they are loaded into the CLR. |
| [`Shared/Steam.cs`](descriptions/Shared/Steam.cs.md) | 81 | 2 | Thin Steam helper for the Dedicated Server: resolves the Steam install path cross-platform, redirects `Steamworks.NET` assembly resolution to a bundled copy, and checks Workshop item install state through the *game-server* UGC API. |
| [`Shared/Tools.cs`](descriptions/Shared/Tools.cs.md) | 196 | 2 | Grab-bag of cross-cutting utilities used throughout Magnetar: SHA-256 hashing of files/strings/folders (used for cache invalidation), human-friendly "time ago" formatting, console/error message reporting, file globbing, filename sanitizing, JSON-based deep copy, interactive-terminal detection, and a cross-platform native crash handler. |
| [`Shared/Updater.cs`](descriptions/Shared/Updater.cs.md) | 209 | 1 | Handles Magnetar's self-update against a GitHub release repo. |

## Shared.Data  ·  [module doc](modules/Shared.Data.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/Data/GitHubPlugin.AssetFile.cs`](descriptions/Shared/Data/GitHubPlugin.AssetFile.cs.md) | 77 | 2 | Defines `GitHubPlugin.AssetFile`, the XML-serializable record describing one cached file that belongs to a compiled GitHub plugin: either a non-code asset extracted from the source archive, a NuGet library DLL, or NuGet content. |
| [`Shared/Data/GitHubPlugin.CacheManifest.cs`](descriptions/Shared/Data/GitHubPlugin.CacheManifest.cs.md) | 241 | 1 | Defines `GitHubPlugin.CacheManifest`, the persistent on-disk cache record for a compiled GitHub plugin. |
| [`Shared/Data/GitHubPlugin.cs`](descriptions/Shared/Data/GitHubPlugin.cs.md) | 381 | 1 | `GitHubPlugin` is the `PluginData` implementation that compiles a plugin from C# source pulled directly from a GitHub repository archive. |
| [`Shared/Data/LegacyWorkshopArchive.cs`](descriptions/Shared/Data/LegacyWorkshopArchive.cs.md) | 212 | 1 | `LegacyWorkshopArchive` locates and expands early Space Engineers Workshop packages that Steam stores as a single `*_legacy.bin` ZIP archive instead of loose mod files. |
| [`Shared/Data/LocalFolderPlugin.cs`](descriptions/Shared/Data/LocalFolderPlugin.cs.md) | 334 | 1 | `LocalFolderPlugin` is the developer-facing `PluginData` that compiles a plugin from a local source folder on every launch (no GitHub download, no cache). |
| [`Shared/Data/LocalPlugin.cs`](descriptions/Shared/Data/LocalPlugin.cs.md) | 109 | 2 | `LocalPlugin` is the `PluginData` for a pre-compiled plugin DLL sitting on disk (not compiled by Magnetar, not from GitHub). |
| [`Shared/Data/ModPlugin.cs`](descriptions/Shared/Data/ModPlugin.cs.md) | 81 | 2 | `ModPlugin` is the `PluginData` for a Steam Workshop mod referenced by its numeric workshop id. |
| [`Shared/Data/ObsoletePlugin.cs`](descriptions/Shared/Data/ObsoletePlugin.cs.md) | 15 | 3 | `ObsoletePlugin` is a placeholder `PluginData` registered as a ProtoBuf subtype so the plugin-list deserializer can tolerate plugins that have been removed or superseded. |
| [`Shared/Data/PluginData.cs`](descriptions/Shared/Data/PluginData.cs.md) | 354 | 1 | `PluginData` is the abstract base for every kind of plugin entry in Magnetar's plugin list: GitHub-compiled (`GitHubPlugin`), local source folder (`LocalFolderPlugin`), local DLL (`LocalPlugin`), Steam Workshop mod (`ModPlugin`), and the placeholder `ObsoletePlugin`. |
| [`Shared/Data/PluginStatus.cs`](descriptions/Shared/Data/PluginStatus.cs.md) | 12 | 3 | `PluginStatus` enumerates the load/health states a `PluginData` can be in, used to drive the status column in the plugin UI and to gate loading. |
| [`Shared/Data/Profile.cs`](descriptions/Shared/Data/Profile.cs.md) | 81 | 2 | `Profile` is a named set of enabled plugins — the persisted selection a user activates. |

## Shared.Network  ·  [module doc](modules/Shared.Network.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/Network/GitHub.cs`](descriptions/Shared/Network/GitHub.cs.md) | 174 | 2 | `GitHub` is a thin static HTTP façade over the GitHub REST API and raw-content CDN. |
| [`Shared/Network/NuGetClient.cs`](descriptions/Shared/Network/NuGetClient.cs.md) | 248 | 1 | `NuGetClient` wraps the NuGet v3 client SDK to download and extract packages from `api.nuget.org` into a local cache inside Magnetar's data directory. |
| [`Shared/Network/NuGetLogger.cs`](descriptions/Shared/Network/NuGetLogger.cs.md) | 87 | 2 | `NuGetLogger` adapts the NuGet SDK's `ILogger` interface to Magnetar's `LogFile` / NLog pipeline. |
| [`Shared/Network/NuGetPackage.cs`](descriptions/Shared/Network/NuGetPackage.cs.md) | 124 | 2 | `NuGetPackage` represents a single NuGet package that has already been extracted to disk. |
| [`Shared/Network/NuGetPackageId.cs`](descriptions/Shared/Network/NuGetPackageId.cs.md) | 47 | 2 | `NuGetPackageId` is a serialisable DTO that identifies a single NuGet package by name and version string. |
| [`Shared/Network/NuGetPackageList.cs`](descriptions/Shared/Network/NuGetPackageList.cs.md) | 20 | 3 | `NuGetPackageList` is a compact container that carries a plugin's NuGet dependency declaration in two optional forms: a path to a `packages.config` file (`Config`) and/or an inline array of `NuGetPackageId` records (`PackageIds`). |
| [`Shared/Network/SimpleHttpClient.cs`](descriptions/Shared/Network/SimpleHttpClient.cs.md) | 202 | 1 | `SimpleHttpClient` is a thin, synchronous REST façade built on `HttpWebRequest`. |

## Shared.Votes  ·  [module doc](modules/Shared.Votes.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/Votes/ConsentManager.cs`](descriptions/Shared/Votes/ConsentManager.cs.md) | 180 | 2 | Owns the telemetry-consent state machine: it decides, once per startup, whether anonymous plugin-usage statistics may be sent, and exposes the result to the rest of the loader through static properties. |
| [`Shared/Votes/Model/ConsentRequest.cs`](descriptions/Shared/Votes/Model/ConsentRequest.cs.md) | 14 | 3 | Defines the JSON request body sent to the statistics server's `/Consent` endpoint when a user grants or withdraws data-handling consent. |
| [`Shared/Votes/Model/PluginVote.cs`](descriptions/Shared/Votes/Model/PluginVote.cs.md) | 24 | 3 | Represents the statistics record for a single plugin as returned by the `/Stats` REST endpoint. |
| [`Shared/Votes/Model/PluginVotes.cs`](descriptions/Shared/Votes/Model/PluginVotes.cs.md) | 24 | 3 | Top-level response container returned by the `/Stats` REST endpoint. |
| [`Shared/Votes/Model/TrackRequest.cs`](descriptions/Shared/Votes/Model/TrackRequest.cs.md) | 11 | 3 | Request body POSTed to `/Track` each time the game starts, recording which plugins were active for a given anonymous instance. |
| [`Shared/Votes/Model/VoteRequest.cs`](descriptions/Shared/Votes/Model/VoteRequest.cs.md) | 20 | 3 | Request body POSTed to `/Vote` when a player changes their vote on a plugin. |
| [`Shared/Votes/VotesClient.cs`](descriptions/Shared/Votes/VotesClient.cs.md) | 88 | 2 | The single outbound client for Magnetar's statistics back-end, providing four REST operations: consent management, votes download, session tracking, and voting. |
