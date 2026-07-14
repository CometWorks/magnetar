# ConfigTerminal/Ui/OptionFormView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 439

## Summary
The generic, registry-driven settings form used for the DS global config, the new-world defaults, and each world's settings. It renders a left category list and a right scrollable form of field widgets built from `OptionDefinition`s, with a filter box that spans all categories. Edits validate and commit live to the `ConfigDocumentBase`; valid values are picked up by the shell's ~1 s auto-save through the `EditSession`, while invalid free-typed values are shown in the error theme and kept out of the document until corrected. Implements `IAutoSaveContent`.

## Types
### OptionFormView — sealed class, internal (`Window`, `IAutoSaveContent`)
One reusable form over a list of options and a backing document.

- **Fields:**
  - `options` (`IReadOnlyList<OptionDefinition>`), `document` (`ConfigDocumentBase`), `session` (`EditSession`), `writer` (`AtomicFile`), `onSaved` (`Action`), `banner` (string) — the constructor inputs.
  - `categories` (`List<string>`), `filter` (`TextField`), `categoryList` (`ListView`), `formFrame` (`FrameView`), `form` (`ScrollView`), `hint` (`Label`), `currentCategory` (string) — the widgets and current selection.
  - `invalid` (`HashSet<OptionDefinition>`) — options whose editor currently holds an invalid, uncommitted value; keyed by the registry singleton so entries survive form rebuilds.
- **Constructor:** `OptionFormView(title, options, document, session, writer, onSaved, banner = null, editMods = null)` — applies the window theme, optionally adds a banner and an "Edit this world's Mods…" button (which invokes `editMods`), builds the filter box, the category list, the framed scroll form, and a bottom hint; selects the first category and lands initial focus on the filter.
- **Methods:**
  - `RebuildForm()` — clears the form and the `invalid` set (discarding uncommitted invalid text); with an empty filter shows the selected category's non-hidden fields, otherwise shows every matching field grouped under category headers; sizes the scroll content.
  - `AddGroup(defs, ref y)` — lays out a run of field rows, inserting a blank row around multi-line fields.
  - `MatchesFilter(def, q)` / `Contains(s, q)` — case-insensitive match against label, XML name, or help text.
  - `IsMultiline(def)` / `RowHeight(def)` — multi-line text occupies `MultilineRows` (3) rows.
  - `BuildRow(def, ref y)` — a container with a label (plus `StatusGlyph`) and the editor; wires the editor's `Enter` to update the hint.
  - `BuildEditor(def, x)` — dispatches on `OptionKind`: `Bool` → `CheckBox`; `Enum` → a horizontal `CyclingRadioGroup` (≤4 choices) or `ComboBox`; `MultilineText` → `TextView` (`AllowsTab=false`, commits on `Leave`); `BlockTypeLimits`/`StringList` → read-only "(edited elsewhere)" label; default → `TextField` validated on each change. Committing calls `document.Set` then `session.NotifyChanged`.
  - `OnFieldEdited(def, editor, text)` — live-validate via `EditSession.ErrorFor`; on valid, remove from `invalid`, restore theme, commit; on invalid, add to `invalid`, apply the error theme, leave the document unchanged.
  - `StatusGlyph(def)` — markers for a field absent from the file (`○`), live-via-reload (`↕`), and an enabled experimental bool (`▲`).
  - `HintFor(def)` — the bottom-line hint: help text, per-kind key instructions, default value, XML name, and live/restart liveness.
  - `FlushPendingSave()` — if dirty and validation passes, saves via `session.Save(writer)` and invokes `onSaved`; swallows errors leaving the change dirty for retry.
  - `InvalidFields` — labels of options in `invalid`.

### CyclingRadioGroup — sealed class, private (nested; `RadioGroup`)
A horizontal radio group whose selection advances cyclically on Space / arrow keys so enum fields have one unambiguous key. `ProcessKey` maps Space and Right to next, Left to previous, and lets Up/Down fall through to the form for focus movement.

## Cross-references
- **Uses:** Terminal.Gui `Window`/`ListView`/`ScrollView`/`FrameView`/`TextField`/`TextView`/`CheckBox`/`ComboBox`/`RadioGroup`/`Label`/`Button`; NStack `ustring`; `OptionDefinition`/`OptionKind`/`OptionChoice`/`Liveness`/`ConfigDocumentBase`/`EditSession` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); `TurboVisionTheme`, `IAutoSaveContent` (this module).
- **Used by:** [AppShell.cs](AppShell.cs.md), [WorldsView.cs](WorldsView.cs.md)
