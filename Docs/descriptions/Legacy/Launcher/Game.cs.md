# Legacy/Launcher/Game.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Launcher` · **Kind:** static helper class (+ one internal class) · **Lines:** 141

## Summary
Thin bridge between Magnetar's launcher and the Space Engineers DS engine internals (`Sandbox`, `VRage`). It reflects into SE statics to register Magnetar's plugin into the engine's plugin lists, sets up `MyFileSystem` paths, reads the game version straight out of the DS assembly's IL (so no game assembly needs to be loaded to learn the version), toggles SE debug/intro flags, and finally invokes the real DS entry point. `GameLog` adapts SE's `MyLog.Default` to Magnetar's `IGameLog` so the launcher can write to and open the engine log.

## Types
### GameLog — class, internal : `IGameLog`
Adapter exposing SE's engine log (`VRage.Utils.MyLog.Default`) through Magnetar's `IGameLog` abstraction (defined in Shared). Lets shared launcher code read/open/append the engine log without referencing VRage directly.

- **Methods:**
  - `Exists()` — true if `MyLog.Default.GetFilePath()` points at an existing `.log` file.
  - `Open()` — flushes `MyLog.Default`, then shell-opens the log file (`ProcessStartInfo { UseShellExecute = true }`); returns false if the file is missing or not a `.log`.
  - `Write(line)` — appends a line via `MyLog.Default.WriteLine`.

### Game — static class, internal
Static engine-bridge helpers. Several use reflection because the engine members are non-public statics that Magnetar must reach into during early launch.

- **Methods:**
  - `RegisterPlugin(IHandleInputPlugin plugin)` — reflects the private static `m_plugins` and `m_handleInputPlugins` lists on `VRage.Plugins.MyPlugins` and appends the plugin to both, injecting Magnetar's `PluginLoader` into SE's plugin pipeline.
  - `SetMainAssembly(assemblyPath)` — given the DS exe path, sets `MyFileSystem.ExePath` to its folder and `MyFileSystem.RootPath` to the parent (game root), and changes the process `CurrentDirectory` to the exe folder so the engine resolves its content relative to the real install.
  - `GetGameVersion(ds64Dir)` — opens `SpaceEngineers.Game.dll` with Mono.Cecil and statically scans the IL of `SetupBasicGameInfo` for the `Stfld` storing `GameVersion`; reads the `Ldc_I4` constant two instructions earlier and reformats the 7-digit integer (`1234567`) into a `Version` `1.234.567`. Returns null if the pattern isn't found. Avoids loading/executing any game assembly just to learn the version.
  - `SetupMyFakes()` — forces `Sandbox.Engine.Utils.MyFakes`'s static initializer to run, then sets `ENABLE_F12_MENU` from `Flags.DebugMenu` and disables `ENABLE_SPLASHSCREEN`.
  - `GetLoadProgress()` — heuristic 0..1 load-progress estimate based on current process `PrivateMemorySize64` divided by an expected ~700 MB growth, clamped.
  - `StartDedicatedServer(args)` — loads `SpaceEngineersDedicated.exe` from `MyFileSystem.ExePath`, reflects its non-public static `SpaceEngineersDedicated.MyProgram.Main`, and invokes it with `args` — the actual handoff to the SE DS.
  - `AddCompilationSymbols(params symbols)` — forwards to `MyScriptCompiler.Static.AddConditionalCompilationSymbols` so in-game script/mod compilation sees extra symbols (e.g. `NETCOREAPP`).
  - `ShowIntroVideo(bool)` — sets `MyPlatformGameSettings.ENABLE_LOGOS`.
  - `RunOnGameThread(Action)` — marshals an action onto SE's update thread via `MySandboxGame.Static.Invoke(action, "Magnetar")`.

## Cross-references
- **Uses:** SE DS assemblies — `Sandbox` (`MySandboxGame`, `MyScriptCompiler`/`VRage.Scripting`), `Sandbox.Engine.Utils.MyFakes`, `Sandbox.Game.MyPlatformGameSettings`, `VRage.FileSystem.MyFileSystem`, `VRage.Plugins.MyPlugins`, `VRage.Utils.MyLog`; `Mono.Cecil` / `Mono.Cecil.Cil` (IL scan); `Pulsar.Shared` (`IGameLog`, `Flags`); reflection.
- **Used by:** [MagnetarCommands.cs](../Commands/MagnetarCommands.cs.md), [ServerControl.cs](ServerControl.cs.md), [Program.cs](../Program.cs.md)
