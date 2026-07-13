# ConfigTerminalTests/LiveEndToEndTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 156

## Summary
Live end-to-end test of the real create → start → "Game ready" → stop flow against an installed dedicated server plus a patched Magnetar launcher, exercising the exact model/process code paths the New-World wizard drives. Gated behind `MAGNETAR_LIVE=1` so it never runs in a normal `dotnet test`. It snapshots and restores the instance's cfg and `LastSession.sbl` and removes the created world, leaving the instance as found.

## Types
### LiveEndToEndTests — class, public
Single-fact live integration test that logs progress via `ITestOutputHelper`.

- **Fields:** `output` — the xUnit test output sink.
- **Methods:**
  - `Create_start_ready_stop()` — returns early unless `MAGNETAR_LIVE=1`; builds an `InstanceBinding` from env-overridable defaults, backs up cfg + `LastSession.sbl`, opens a `DsInstance`, finds the template, seeds session options from the template seed into the dedicated cfg (defaulting `Session.MaxPlayers` to 4), sets `WorldName`/`PremadeCheckpointPath`, starts the `MagnetarProcess` with `-ignorelastsession`, asserts `ServerState.Running`, waits up to 6 minutes for "Game ready" via `ReadinessDetector`, stops gracefully (SIGTERM → save + quit) asserting `NotRunning`, then in `finally` force-kills if needed, restores cfg + `LastSession.sbl`, and deletes the created world and `.bak`.
  - `WaitForReady(binding, timeout)` (private) — polls `LogCatalog.ActiveGameLog` through `ReadinessDetector.IsReady` every 2s until ready or timeout.
  - `Env(name, fallback)` (private static) — env var with fallback.
  - `Log(m)` (private) — timestamped line to the test output.

## Cross-references
- **Uses:** `InstanceBinding`, `DsInstance`, `WorldTemplate`, `WorldTemplateCatalog`, `DedicatedConfigDocument`, `WorldConfigDocument`, `OptionRegistry`, `OptionDefinition`, `ConfigDocumentBase`, `LastSessionFile` (`ConfigTerminal/Model/`); `MagnetarProcess`, `LaunchSpec`, `OpResult`, `ServerStatus`/`ServerState` (`ConfigTerminal/Process/`); `LogCatalog`, `LogFileInfo`, `ReadinessDetector` (`ConfigTerminal/Logs/`); `AtomicFile` (`ConfigTerminal/Io/`); a live DS install + patched Magnetar launcher; xUnit (`ITestOutputHelper`); `System.Diagnostics.Stopwatch`, `System.Threading`.
- **Used by:** _none within the repository_
