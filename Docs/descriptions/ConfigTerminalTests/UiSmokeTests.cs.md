# ConfigTerminalTests/UiSmokeTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 89

## Summary
Headless UI smoke test that builds the whole `AppShell` view tree against Terminal.Gui's `FakeDriver` and pumps a few main-loop iterations, catching constructor/layout exceptions without a real terminal. Deliberately thin — the real logic lives below the UI — it just proves the shell and each main content view construct and navigate without throwing. Runs in the `ui-single-threaded` collection so Terminal.Gui's global state is never touched concurrently.

## Types
### UiSmokeTests — class, public, implements `IDisposable`, `[Collection("ui-single-threaded")]`
Sets up a temp data dir with a `Saves` folder in the ctor; `Dispose` calls `Application.Shutdown()` and deletes the dir.

- **Fields:** `dir` — temp instance directory.
- **Methods:**
  - `Shell_builds_and_pumps_without_throwing()` — constructs a `FakeDriver`, reflectively instantiates Terminal.Gui's internal `FakeMainLoop`, `Application.Init`s with them, applies `TurboVisionTheme`, builds an `AppShell` over an `InstanceBinding` pointing at the temp dir, `Application.Begin`s it, and pumps `RunMainLoopIteration` a few times. Then exercises navigation: `ShowServerSettings`, `ShowWorlds`, `ShowNewWorldDefaults`, `ShowPlugins`, `ShowHubPlugins`, `ShowPluginSources`, `ShowProfiles`, pumps again, and ends the run state — all inside try/finally with `Application.Shutdown`.

## Cross-references
- **Uses:** `AppShell`, `TurboVisionTheme` (`ConfigTerminal/Ui/`); `InstanceBinding` (`ConfigTerminal/Model/`); Terminal.Gui `FakeDriver`/`Application`/`IMainLoopDriver` and internal `FakeMainLoop` (via reflection); xUnit (`Collection`, `Fact`); `System.Reflection`.
- **Used by:** _none within the repository_
