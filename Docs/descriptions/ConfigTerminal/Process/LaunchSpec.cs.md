# ConfigTerminal/Process/LaunchSpec.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Process` · **Kind:** sealed class · **Lines:** 71

## Summary
Builds the Magnetar daemon launch command line from an `InstanceBinding` and validates user-supplied extra arguments. The tool owns the managed switches (`-daemon`, `-config`, `-path`, `-ds64`) and world/session selection; any extra arg that would fight those is rejected before launch so tool assumptions cannot be silently overridden.

## Types
### LaunchSpec — sealed class, internal
Mutable spec assembled by the caller and consumed by `MagnetarProcess.Start`.
- **Fields:** `ForbiddenExtra` (static readonly string[]) — the switches user extra args may not touch: `-session:`, `-ignorelastsession`, `-path`, `-config`, `-daemon`, `-ds64` (entries ending in `:` are prefix-matched, the rest exact-matched).
- **Properties:**
  - `Binding` (`InstanceBinding`) — source of config/data/ds64 dirs.
  - `IgnoreLastSession` (bool) — set for a world-creation start so the DS runs its new-world branch (§9.6), adding `-ignorelastsession`.
  - `ExtraArgs` (string[]) — extra launch args from tool settings (e.g. `-noconsent`); defaults to empty.
- **Methods:**
  - `RejectionReason()` — returns the rejection message if any `ExtraArgs` entry collides with a `ForbiddenExtra` switch (prefix match for `:`-suffixed entries, case-insensitive exact match otherwise), else null.
  - `BuildArgs()` — returns the ordered argument list (excluding the executable): always `-daemon`, then `-config <MagnetarConfigDir>`, `-ds64 <Ds64Dir>`, `-path <DataDir>` for each non-empty binding path, then `-ignorelastsession` when set, then the non-blank `ExtraArgs`.

## Cross-references
- **Uses:** `InstanceBinding` (`ConfigTerminal/Model/`); `System.Collections.Generic`, `System.Linq`.
- **Used by:** [MagnetarProcess.cs](MagnetarProcess.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md)
