# ConfigTerminal/Ui/ModListView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 219

## Summary
Ordered mod-list editor for a world's `Sandbox_config.sbc`: add (by Workshop id or URL, with background friendly-name resolution), delete, reorder, and toggle a mod's dependency flag. Implements `IAutoSaveContent`, saving change-only through a canonical-string snapshot with a `touched` gate so idle ticks skip the serialize-and-compare.

## Types
### ModListView — sealed class, internal (`Window`, `IAutoSaveContent`)
The per-world mods panel.

- **Fields:**
  - `doc` (`WorldConfigDocument`), `writer` (`AtomicFile`), `mods` (`ModList`), `list` (`ListView`).
  - `snapshot` (string) — canonical serialization at the last save, for change detection.
  - `touched` (bool) — gates the flush; `currentIssues` (`IReadOnlyList<string>`) — the last validation problems, surfaced as the leave-warning.
- **Methods:**
  - `ModListView(WorldInfo, AtomicFile)` — opens the doc, reads the mods, normalizes the baseline through `WriteMods` before snapshotting (so an untouched list isn't seen as changed), and builds the list + Add/Del/Up/Down/Toggle-Dependency buttons.
  - `Refresh()` — repopulates the numbered list (id, dep marker, friendly name).
  - `MoveSelected(int delta)` — reorders one slot up/down, restoring `TopItem` and keeping the selection on the moved mod.
  - `AddMod()` — prompts for a Workshop id or URL (validated via `WorkshopResolver.ExtractIds`), filters out ids already present, and resolves names in the background.
  - `ResolveOnline(List<long>)` — static; runs `WorkshopResolver.Resolve` off the UI thread, returning null on any transport failure.
  - `ApplyResolved(wanted, resolved)` — back on the UI thread, appends resolved mods (or bare ids when offline, with a warning) and surfaces resolver warnings.
  - `Remove()` / `ToggleDependency()` — remove the selected mod / flip its `IsDependency`.
  - `FlushPendingSave()` — skips when untouched; validates, and on failure keeps the invalid list unsaved (surfacing issues via `InvalidFields`); writes mods, no-ops if the canonical string is unchanged, else saves and re-snapshots. Swallows save errors for retry.
  - `InvalidFields` — the current validation issues.

## Cross-references
- **Uses:** Terminal.Gui `Window`/`ListView`/`Button`/`Label`; `WorldConfigDocument`/`WorldInfo`/`ModList`/`ModItem`/`WorkshopResolver` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); `Dialogs`, `TurboVisionTheme`, `IAutoSaveContent` (this module).
- **Used by:** [WorldsView.cs](WorldsView.cs.md)
