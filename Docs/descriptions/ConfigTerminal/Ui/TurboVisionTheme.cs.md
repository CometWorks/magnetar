# ConfigTerminal/Ui/TurboVisionTheme.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** static class · **Lines:** 73

## Summary
The classic 16-color Turbo Vision / Turbo Pascal 7 IDE palette expressed as Terminal.Gui v1 `ColorScheme`s. Every colour is within the CGA 16-color set so it renders identically under the curses, Windows and Net drivers. `Apply()` builds the schemes and installs them as the global `Colors.*` defaults.

## Types
### TurboVisionTheme — static class, internal
The shared theme.

- **Properties:** `Window`, `Menu`, `Dialog`, `Error`, `Desktop` — the five `ColorScheme`s (each with Normal/Focus/HotNormal/HotFocus/Disabled attributes), set by `Apply`.
- **Methods:**
  - `A(fg, bg)` — private helper wrapping `Terminal.Gui.Attribute.Make`.
  - `Apply()` — constructs the five schemes (white-on-blue window, gray menu, gray dialog, red error, blue desktop) and assigns them to `Colors.Base`/`Menu`/`Dialog`/`Error`/`TopLevel`.

## Cross-references
- **Uses:** Terminal.Gui `ColorScheme`/`Attribute`/`Color`/`Colors`.
- **Used by:** [Program.cs](../Program.cs.md), [AccessListView.cs](AccessListView.cs.md), [AppShell.cs](AppShell.cs.md), [DashboardView.cs](DashboardView.cs.md), [DesktopBackground.cs](DesktopBackground.cs.md), [Dialogs.cs](Dialogs.cs.md), [FileDialogs.cs](FileDialogs.cs.md), [HubPluginsView.cs](HubPluginsView.cs.md), [InstancePickerDialog.cs](InstancePickerDialog.cs.md), [LogViewerView.cs](LogViewerView.cs.md), [ModListView.cs](ModListView.cs.md), [NewWorldWizard.cs](NewWorldWizard.cs.md), [OptionFormView.cs](OptionFormView.cs.md), [PasswordDialog.cs](PasswordDialog.cs.md), [PluginSourcesView.cs](PluginSourcesView.cs.md), [PluginsView.cs](PluginsView.cs.md), [ProfilesView.cs](ProfilesView.cs.md), [WorldsView.cs](WorldsView.cs.md), [UiSmokeTests.cs](../../ConfigTerminalTests/UiSmokeTests.cs.md)
