# Branch Changes: `magnetar`

Summary of all changes made on the `magnetar` branch relative to `main`.

## 1. Implementation (`a07afe9`)

Added the Magnetar loader for Space Engineers 2 (.NET 10):

- Full plugin loader with Harmony patching (`Magnetar/` project)
- Compiler integration for runtime plugin compilation
- Plugin extensions: GitHub, local folder, local, and mod plugins
- Launcher, loader, and Steam mod support
- UI screens: plugin management, profiles, sources, consent dialogs
- Deploy script for automated builds

Also added `Directory.Build.props` for shared MSBuild properties.

## 2. Removed In-Game UI (`22a3161`)

Stripped all in-game UI screens from the Magnetar loader:

- Removed 14 screen classes (plugin menus, source management, profiles, dialogs)
- Removed UI-related patches (menu creation, file dialogs, keyboard shortcuts)
- Removed `PluginData`, `GitHubPlugin`, `LocalFolderPlugin`, `LocalPlugin` extension classes
- Simplified `LoaderTools` to remove UI-dependent plugin loading code
- Retained the loading screen and core loader functionality

This prepares for management via the Web UI instead of in-game screens.

## 3. Replace Error Dialogs (`113a0cb`)

Replaced Windows message box error dialogs with console output and exit codes:

- Added console-based error reporting in Legacy, Magnetar, and Modern programs
- Simplified crash handling in Modern to use console output
- Updated `Shared/Tools.cs` error display to avoid UI dependencies
- Made Updater error handling more robust with proper exit codes

## 4. Closing Splash (`3e392e1`)

Added proper splash screen cleanup on Magnetar startup.

## 5. Consolidation (`d28d9c5`)

Major consolidation aligning the codebase more closely with Pulsar conventions:

- **Removed** the entire `Magnetar/` project (folded into Legacy)
- **Removed** the entire `Modern/` project (SE2 loader)
- **Simplified** Legacy project: removed UI screens, extension classes, and UI patches
- **Updated** Legacy to handle both .NET Framework and .NET 10 compilation paths
- **Cleaned** solution file, removing Magnetar and Modern project references
- Net reduction: ~13,100 lines removed, ~150 lines added

## 6. Web UI and Admin Plugin (`a2d1667`)

Added two new components for remote server management:

### Admin Plugin (`Admin/`)

A .NET Framework 4.8 plugin that runs inside the Dedicated Server:

- REST API on `http://127.0.0.1:9000/` with CORS support
- Endpoints for server state, player management, chat, save, and stop
- Game engine access via `MySession`, `MyMultiplayer`, and reflection
- In-memory chat log (max 200 messages)
- See [AdminPlugin.md](AdminPlugin.md) for full API documentation

### Web UI (`webui/`)

A Python FastAPI web application for browser-based server management:

- Dark-themed responsive UI with sidebar navigation
- DS configuration editor (reads/writes `SpaceEngineers-Dedicated.cfg` XML)
- Pulsar configuration editor (core settings, profiles, plugin sources)
- Live server dashboard with real-time status, players, and chat
- Player management (kick, ban, promote) via the Admin plugin
- 100+ world/session settings with categorized forms
- See [WebUI.md](WebUI.md) for full documentation

### Files Added

- `Admin/Admin.csproj`, `AdminPlugin.cs`, `HttpServer.cs`, `GameBridge.cs`
- `webui/` - Complete Python application (32 files, ~4,700 lines)
- Solution updated to include the Admin project
