# ConfigTerminal/Model/WorldTemplateCatalog.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 114

## Summary
Enumerates the world templates the DS ships under `<ContentPath>/CustomWorlds/`, where ContentPath is the `Content/` folder sibling to `DedicatedServer64/`. Empty when the DS install is not found. Template names that are localization keys (e.g. `{LOCG:...}`) are unusable without loc tables, so the catalog falls back to the human-readable folder name.

## Types
### WorldTemplate — sealed class, internal
A world template shipped with the DS.
- **Fields:** `FolderName`, `FolderPath` (absolute — becomes `PremadeCheckpointPath`), `DisplayName`, `HasWorldConfig`, `HasCheckpoint`.
- **Properties:** `WorldConfigPath`, `SandboxPath`.
### WorldTemplateCatalog — sealed class, internal
- **Properties:** `Templates` (`IReadOnlyList<WorldTemplate>`), `CustomWorldsPath`.
- **Methods:**
  - `Scan(string ds64Dir)` (static) — resolves `../Content/CustomWorlds` from the DS install dir (null when absent) and rescans.
  - `Rescan()` — enumerates template dirs (needing a checkpoint and/or config), resolving each display name, sorted by display name.
  - `ResolveDisplayName(tpl)` (private static) / `IsLocalizationKey(name)` — prefers the config/checkpoint `SessionName` unless it is a `{...}` loc key, else the folder name.
  - `OpenSeed(WorldTemplate tpl)` (static) — opens the template's settings as an editable in-memory seed document, seeding the name from the checkpoint when it lacks a config.

## Cross-references
- **Uses:** `WorldConfigDocument`/`CheckpointReader` (this module); `System.IO`, `System.Linq`.
- **Used by:** [Diagnostics.cs](../Diagnostics.cs.md), [DsInstance.cs](DsInstance.cs.md), [WorldCreator.cs](WorldCreator.cs.md), [NewWorldWizard.cs](../Ui/NewWorldWizard.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md)
