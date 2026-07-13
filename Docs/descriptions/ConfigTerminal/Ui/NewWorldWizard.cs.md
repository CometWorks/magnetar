# ConfigTerminal/Ui/NewWorldWizard.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Ui` · **Kind:** static class · **Lines:** 158

## Summary
New-world creation by folder copy: pick a template, name the world, then copy the template into `Saves/<name>` and stamp the name into its `Sandbox_config.sbc` via `WorldCreator`. No server start is required — the world exists and is editable immediately, and it is activated (`LastSession.sbl`) so the DS loads it next.

## Types
### NewWorldWizard — static class, internal
Modal, sequential wizard driven from the shell.

- **Methods:**
  - `Run(AppShell)` — the entry point: aborts with an error if no templates exist (needs a DS install), picks a template and name, confirms (listing what is copied/stamped/activated), then runs `WorldCreator.CreateFromTemplate` in the background and on success reloads the instance, activates the world, rebuilds the Worlds list, and reports success.
  - `ActivateCreatedWorld(shell, name)` — private; finds the new world, writes its `LastSessionFile`, and clears the cfg's `IgnoreLastSession`/`LoadWorld`/`PremadeCheckpointPath` before saving and reloading (mirrors `WorldsView.ActivateWorld`); on failure tells the user to activate manually.
  - `PickTemplate(DsInstance)` — private; a modal `Dialog` listing the DS templates (Content/CustomWorlds), returning the chosen `WorldTemplate` or null.
  - `PromptName(WorldTemplate, DsInstance)` — private; a modal name prompt seeded with the template name, live-validating against path separators and duplicate world names; returns the trimmed name or null.

## Cross-references
- **Uses:** Terminal.Gui `Dialog`/`ListView`/`Label`/`TextField`/`Button`/`Application`; `DsInstance`/`WorldTemplate`/`WorldInfo`/`WorldCreator`/`LastSessionFile`/`DedicatedConfigDocument` (`ConfigTerminal/Model/`); `PlatformPaths` (`ConfigTerminal/Io/`); `AppShell`, `Dialogs`, `TurboVisionTheme` (this module).
- **Used by:** [AppShell.cs](AppShell.cs.md)
