# ConfigTerminal/Model/WorldCatalog.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 104

## Summary
Enumerates the worlds under a `Saves/` directory, building `WorldInfo` display metadata for each folder that holds a checkpoint and/or world config, sorted by last-save time descending.

## Types
### WorldInfo — sealed class, internal
Display metadata for one world folder.
- **Fields:** `FolderName` (identity used in RelativePath), `FolderPath`, `SessionName`, `LastSaveTime` (`DateTime?`), `ModCount`, `HasWorldConfig`, `HasCheckpoint`, `IsActive`.
- **Properties:** `SandboxPath` (`Sandbox.sbc`), `WorldConfigPath` (`Sandbox_config.sbc`).
### WorldCatalog — sealed class, internal
- **Properties:** `Worlds` (`IReadOnlyList<WorldInfo>`), `SavesPath`.
- **Methods:**
  - `Scan()` — (re)enumerates directories under Saves (skipping `Backup`, and folders with neither checkpoint nor config), populates each and sorts by last-save time then folder name.
  - `Populate(WorldInfo)` (private static) — reads `SessionName`/`LastSaveTime`/`ModCount` from the world config when present, falls back to the checkpoint's session name, then the folder name, and to the checkpoint file's last-write time.
  - `Find(string folderName)` — case/platform-insensitive folder-name lookup.

## Cross-references
- **Uses:** `WorldConfigDocument`/`CheckpointReader`/`CheckpointInfo` (this module); `PlatformPaths` (`ConfigTerminal/Io/`); `System.IO`, `System.Linq`.
- **Used by:** [Diagnostics.cs](../Diagnostics.cs.md), [DsInstance.cs](DsInstance.cs.md), [LastSessionFile.cs](LastSessionFile.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [DashboardView.cs](../Ui/DashboardView.cs.md), [ModListView.cs](../Ui/ModListView.cs.md), [NewWorldWizard.cs](../Ui/NewWorldWizard.cs.md), [WorldsView.cs](../Ui/WorldsView.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md)
