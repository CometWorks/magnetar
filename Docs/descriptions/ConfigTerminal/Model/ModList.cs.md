# ConfigTerminal/Model/ModList.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 50

## Summary
The per-world mod list model: a `ModItem` value type for one workshop mod and an ordered `ModList` with reorder and validation. List order is the SE load order.

## Types
### ModItem — sealed class, internal
One workshop mod entry.
- **Fields:** `PublishedFileId` (ulong), `FriendlyName`, `ServiceName` (default "Steam"), `IsDependency` (bool).
- **Properties:** `Name` — derived `"{id}.sbm"` (the DS's file name; never stored separately).
### ModList — sealed class, internal
- **Properties:** `Items` (`List<ModItem>`).
- **Methods:**
  - `MoveUp(int i)` / `MoveDown(int i)` — bounds-checked adjacent swap.
  - `Validate()` — returns human-readable issues for a zero `PublishedFileId` or any id appearing more than once (the cases the DS would reject).

## Cross-references
- **Uses:** `System.Collections.Generic`, `System.Linq`.
- **Used by:** [WorldConfigDocument.cs](WorldConfigDocument.cs.md), [AppShell.cs](../Ui/AppShell.cs.md), [ModListView.cs](../Ui/ModListView.cs.md), [DocumentTests.cs](../../ConfigTerminalTests/DocumentTests.cs.md)
