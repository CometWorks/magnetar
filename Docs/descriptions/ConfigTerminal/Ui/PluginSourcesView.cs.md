# ConfigTerminal/Ui/PluginSourcesView.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** sealed class · **Lines:** 158

## Summary
Manages the instance's plugin catalog *sources* — remote GitHub hubs (e.g. MagnetarHub), single remote plugin repos, and local hub folders that Magnetar scans for available plugins. Edits `Sources/sources.xml` in place via the same upsert approach as the rest of the tool, preserving the fields Magnetar manages itself (LastCheck, Hash). Space toggles a source; the three kinds are flattened into one list view.

## Types
### PluginSourcesView — sealed class, internal (`Window`)
The Plugin Sources panel.

- **Fields:** `plugins` (`MagnetarPlugins`), `list` (`ListView`), `rows` (`List<Row>`).
- **Nested types:**
  - `Kind` — private enum: `RemoteHub`, `RemotePlugin`, `LocalHub`.
  - `Row` — private class: `Kind`, `Key`, `Text`, `Enabled` — one flattened list entry.
- **Methods:**
  - `PluginSourcesView(magnetarConfigDir, writer)` — builds the list (Enter toggles) and the Add Hub/Add Plugin/Add Local/Toggle/Remove buttons; refreshes.
  - `ProcessKey(KeyEvent)` — Space toggles when the list has focus.
  - `Refresh()` — reloads and rebuilds `rows` from `RemoteHubs()`, `RemotePlugins()`, and `LocalHubs()`, preserving the selection.
  - `Selected()` — the selected `Row` or null.
  - `AddRemoteHub()` / `AddRemotePlugin()` / `AddLocalHub()` — prompt for repo/name/branch (and manifest file / folder), calling the matching `MagnetarPlugins.Add*` and warning on a duplicate.
  - `ToggleEnabled()` — flips the selected source's enabled state via the kind-specific setter.
  - `Remove()` — confirms and removes the selected source (its plugins stay listed in the profile but no longer resolve).

## Cross-references
- **Uses:** Terminal.Gui `Window`/`ListView`/`Button`; `MagnetarPlugins`/`RemoteHubSource`/`RemotePluginSource`/`LocalHubSource` (`ConfigTerminal/Model/`); `AtomicFile` (`ConfigTerminal/Io/`); `Dialogs`, `TurboVisionTheme` (this module); `System.IO.Path`.
- **Used by:** [AppShell.cs](AppShell.cs.md), [WorldsView.cs](WorldsView.cs.md)
