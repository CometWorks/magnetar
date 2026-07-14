# ConfigTerminal/Model/ProfileCatalog.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 147

## Summary
Manages the instance's plugin *profiles* — named presets of enabled plugins stored as `Profiles/<Key>.xml`, with `Current.xml` the active set the server loads. It mirrors Magnetar's own `Pulsar.Shared.Config.ProfilesConfig` (load, save/add, update, rename, remove) but from outside the game, editing the files directly through `AtomicFile`. "Current" is reserved and never listed as a preset.

## Types
### ProfileInfo — sealed class, internal
One saved profile on disk: `Name`, `Key` (file-name stem), `FilePath`, `MatchesActive` (its enabled set equals Current's).
### ProfileCatalog — sealed class, internal
- **Fields/consts:** `CurrentKey` ("Current"), `configDir`, `writer`; `ProfilesDir` (property).
- **Methods:**
  - `NamedProfiles()` — the saved profiles (excluding Current.xml and .bak), each with a `MatchesActive` computed by comparing `CollectionsSignature()` to the active one, sorted by name.
  - `ActiveMatchKey()` — key of the saved profile matching Current, or null.
  - `Exists(key)` — whether a profile file exists.
  - `SaveCurrentAs(name)` — saves the active set as a new named profile; throws on the reserved key; returns false without writing when the key already exists (caller confirms via `Update`).
  - `Update(key)` — overwrites an existing named profile with the active set, keeping its name.
  - `Load(key)` — copies a named profile's enabled set into Current.xml (renaming it "Current").
  - `Rename(key, newName)` — writes under the new key and deletes the old file.
  - `Delete(key)` — deletes a saved profile; refuses to delete Current.
  - `WriteSnapshot` / `KeyFor` / `TryDelete` (private) — snapshot-write, name→key validation, best-effort delete.

## Cross-references
- **Uses:** `PluginProfileDocument` (this module); `AtomicFile` (`ConfigTerminal/Io/`); `System.IO`, `System.Linq`.
- **Used by:** [Diagnostics.cs](../Diagnostics.cs.md), [ProfilesView.cs](../Ui/ProfilesView.cs.md), [PluginInteropTests.cs](../../ConfigTerminalTests/PluginInteropTests.cs.md), [ProfileCatalogTests.cs](../../ConfigTerminalTests/ProfileCatalogTests.cs.md)
