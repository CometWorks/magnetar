# ConfigTerminalTests/ProfileCatalogTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 129

## Summary
xUnit tests for `ProfileCatalog`, which manages named plugin profiles derived from the active `Current` set. It verifies the Magnetar-compatible key sanitization, save/load round-trips of the enabled set, collision and reserved-name ("Current") handling, active-match tracking, update/rename/delete semantics, and that `Current` is never surfaced as a named profile and never destroyed. Uses a fresh temp dir (`IDisposable`) with a shared `AtomicFile`.

## Types
### ProfileCatalogTests — class, public, implements `IDisposable`
Seeds a `Current` profile via a `SeedCurrent` helper, then drives the catalog and re-reads profiles.

- **Fields:** `dir` — per-test temp directory; `writer` — shared `AtomicFile`.
- **Methods:**
  - `SeedCurrent(params localDlls)` (private) — opens `Current`, enables the given local DLLs, saves.
  - `CleanKey_matches_magnetar_semantics()` — `PluginProfileDocument.CleanKey` passes valid names through and replaces invalid filename chars (e.g. `/`) with `-`.
  - `Save_new_then_load_round_trips_the_enabled_set()` — `SaveCurrentAs` succeeds once, collides (false) on repeat, throws `InvalidOperationException` for reserved "Current"; the saved profile `MatchesActive` and `ActiveMatchKey` tracks it; changing the active set clears the match, and `Load` restores the set while keeping the active profile named `Current`.
  - `Update_overwrites_existing_profile_with_active_set()` — `Update` overwrites the saved profile with the current enabled set, preserving the profile `Name`.
  - `Rename_moves_the_file_and_updates_name()` — `Rename` moves the file, updates `Name`, and preserves the enabled set.
  - `Delete_removes_the_profile_but_not_current()` — `Delete` removes the named profile, leaves `Current.xml` intact, and throws `InvalidOperationException` for "Current".
  - `Current_is_never_listed_as_a_profile()` — `NamedProfiles` never includes a "Current"-keyed entry.

## Cross-references
- **Uses:** `ProfileCatalog`, `ProfileInfo`, `PluginProfileDocument` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); xUnit; `System.IO`, `System.Linq`.
- **Used by:** _none within the repository_
