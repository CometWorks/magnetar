# ConfigTerminal/Model/WorldConfigDocument.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 166

## Summary
`XDocument` wrapper for a world's `Sandbox_config.sbc` (`MyObjectBuilder_WorldConfiguration`). Editing session settings here is the correct minimal way to change a world: on load the DS reads `Sandbox.sbc` then overrides Settings/Mods/SessionName from this file. Session options live under `<Settings>`, and the per-world mod list under `<Mods>`.

## Types
### WorldConfigDocument — sealed class, internal (extends `ConfigDocumentBase`)
- **Consts:** `RootName` (`MyObjectBuilder_WorldConfiguration`), `SettingsName` (`Settings`), `ModsName` (`Mods`), xsi/xsd namespaces.
- **Methods:**
  - `Open(string filePath)` (static) / `CreateSkeleton()` — load-with-preserve-whitespace or in-memory skeleton (`<Settings>` seeded).
  - `ResolveScopeRoot(scope, create)` (override) — a world config carries only session settings; returns (optionally `AddFirst`-creating) the `<Settings>` element regardless of scope.
  - `RefreshLastSaveTime()` — updates an existing `<LastSaveTime>` to now (only when present, to avoid reordering the DS's element sequence) so a freshly created world sorts to the top.
  - `ReadMods()` — parses `<Mods>/<ModItem>` into a `ModList`, deduping by `PublishedFileId` and skipping zero ids.
  - `WriteMods(ModList list)` — rebuilds `<Mods>` in load order, placing it right after `<Settings>`; writes `FriendlyName` attribute plus `Name`/`PublishedFileId`/`PublishedServiceName`/`IsDependency` children.
  - `SeedFrom(CheckpointInfo info)` — seeds `SessionName` from a checkpoint reader when no config existed.
- **Properties:** `SessionName` (string, upsert); `LastSaveTime` (`DateTime?`, read-only parse).

## Cross-references
- **Uses:** `ConfigDocumentBase`/`ModList`/`ModItem`/`CheckpointInfo` (this module); `System.Xml.Linq`, `System.IO`, `System.Linq`.
- **Used by:** [WorldCatalog.cs](WorldCatalog.cs.md), [WorldCreator.cs](WorldCreator.cs.md), [WorldTemplateCatalog.cs](WorldTemplateCatalog.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [ModListView.cs](../Ui/ModListView.cs.md), [WorldsView.cs](../Ui/WorldsView.cs.md), [DocumentTests.cs](../../ConfigTerminalTests/DocumentTests.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md)
