# ConfigTerminal/Ui/ManifestPicker.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** static class · **Lines:** 17

## Summary
Quasar-style dev-folder picker: browse the filesystem and select a plugin's `.xml` manifest file, opening at the last-visited folder so adding several plugins in a row is frictionless. A thin wrapper over `FileDialogs.PickFile`.

## Types
### ManifestPicker — static class, internal
- **Methods:** `Pick(string initialFolder)` — returns the picked manifest path (filtered to `.xml`), or null on cancel.

## Cross-references
- **Uses:** `FileDialogs` (this module).
- **Used by:** [PluginsView.cs](PluginsView.cs.md)
