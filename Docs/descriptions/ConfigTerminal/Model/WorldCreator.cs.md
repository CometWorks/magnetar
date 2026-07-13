# ConfigTerminal/Model/WorldCreator.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** static class · **Lines:** 88

## Summary
Creates a new world by copying a DS world template (`Content/CustomWorlds/…`) into `Saves/` and stamping the chosen name into its `Sandbox_config.sbc` — no server start required. Because the DS overrides checkpoint Settings/Mods/SessionName from `Sandbox_config.sbc` on load, a folder copy plus a patched config is a complete, immediately editable world; the large (often gzipped) checkpoint is never touched. The only skipped DS behaviour is creation-time generation (RandomizeSeed / procedural asteroids), which the stock templates already ship.

## Types
### WorldCreator — static class, internal
- **Fields:** `Utf8NoBom` — UTF-8 encoding without BOM.
- **Methods:**
  - `CreateFromTemplate(WorldTemplate template, string worldName, string savesPath)` — validates inputs (rejects an existing target folder / missing template), assembles the copy in a hidden `.<name>.creating` staging folder, stamps the config, and `Directory.Move`s it into place so a failure never leaves a half-populated world; cleans up staging on error. Returns the created world folder path.
  - `StampWorldConfig(worldDir, worldName)` (private static) — opens (or synthesizes from the checkpoint) `Sandbox_config.sbc`, sets `SessionName` and a fresh save time, and writes it directly (no `.bak` — the whole folder is still staging).
  - `CopyDirectory(sourceDir, destDir)` (private static) — recursive file/dir copy (no overwrite).

## Cross-references
- **Uses:** `WorldTemplate`/`WorldConfigDocument`/`CheckpointReader` (this module); `System.IO`, `System.Text`.
- **Used by:** [NewWorldWizard.cs](../Ui/NewWorldWizard.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md)
