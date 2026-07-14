# ConfigTerminal/Model/DedicatedConfigDocument.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 176

## Summary
`XDocument` wrapper for `SpaceEngineers-Dedicated.cfg` (root `MyConfigDedicated`). Registry options are edited through the base upsert helpers; the structures that have dedicated editors — access lists, server password, and world-selection flags — get typed accessors here. Opening tolerates a missing file by building a minimal in-memory skeleton.

## Types
### DedicatedConfigDocument — sealed class, internal (extends `ConfigDocumentBase`)
- **Fields/consts:** `RootName` (`MyConfigDedicated`), `SessionSettingsName` (`SessionSettings`), `Xsi`/`Xsd` XML namespaces.
- **Methods:**
  - `Open(string filePath)` (static) — loads the cfg with `LoadOptions.PreserveWhitespace`, or returns a skeleton document (`existsOnDisk: false`) when the file is absent.
  - `CreateSkeleton()` (private static) — builds `<MyConfigDedicated>` with the xsi/xsd namespace attributes and an empty `<SessionSettings>`.
  - `ResolveScopeRoot(scope, create)` (override) — returns the root for `DedicatedRoot`; for `Session` returns (optionally creating) the `<SessionSettings>` child.
  - `UpsertRoot(name, value)` / `ReadItems(listName)` / `WriteItems(listName, ids)` (private) — root-level element upsert and access-list read/rewrite (items serialized as `<unsignedLong>`).
  - `SetPassword(string plaintext)` — sets `ServerPasswordHash`/`ServerPasswordSalt` via `PasswordHasher.Hash`; null/empty clears both.
- **Properties:**
  - World selection: `IgnoreLastSession` (bool), `LoadWorld`, `PremadeCheckpointPath`, `WorldName` (strings, upserting root elements); `ServerPort` (int, read-only, falls back to 27016).
  - Access lists: `Administrators`/`Banned`/`Reserved` (`IReadOnlyList<string>`) with `SetAdministrators`/`SetBanned`/`SetReserved`; `GroupId` (string, default "0").
  - `HasPassword` (bool, read-only) — true when `ServerPasswordHash` is non-empty.

## Cross-references
- **Uses:** `ConfigDocumentBase`/`PasswordHasher`/`OptionScope` (this module); `System.Xml.Linq`, `System.IO`; `ConfigTerminal/Io/`.
- **Used by:** [DsInstance.cs](DsInstance.cs.md), [EditSession.cs](EditSession.cs.md), [AccessListView.cs](../Ui/AccessListView.cs.md), [DashboardView.cs](../Ui/DashboardView.cs.md), [NewWorldWizard.cs](../Ui/NewWorldWizard.cs.md), [PasswordDialog.cs](../Ui/PasswordDialog.cs.md), [WorldsView.cs](../Ui/WorldsView.cs.md), [DocumentTests.cs](../../ConfigTerminalTests/DocumentTests.cs.md), [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md)
