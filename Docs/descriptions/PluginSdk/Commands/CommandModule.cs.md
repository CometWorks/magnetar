# PluginSdk/Commands/CommandModule.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** abstract class · **Lines:** 21

## Summary
`CommandModule` is the plugin-facing base class for a group of chat commands. A plugin subclasses it, decorates the subclass with `[CommandRoot]` and each handler method with `[Command]`, then registers it via `ServerCommands.Register`. The host instantiates a fresh module object per invocation (`Activator.CreateInstance` in `RegisteredCommand.Invoke`) and assigns `Context` before calling the handler, so modules should not store per-call state in instance fields.

## Types

### `CommandModule` — abstract class, public

Minimal base providing only the `Context` property. The `internal set` ensures only the dispatch infrastructure (`RegisteredCommand.Invoke`) can assign it, preventing accidental misuse by plugins.

- **Properties:**
  - `Context` — `CommandContext` assigned by the dispatcher before the handler runs. Gives the handler access to the caller, arguments, and `Respond` helpers.

## Cross-references
- **Uses:** `PluginSdk/Commands/CommandContext.cs`
- **Used by:** [MagnetarCommands.cs](../../Legacy/Commands/MagnetarCommands.cs.md), [CommandRegistry.cs](CommandRegistry.cs.md), [RegisteredCommand.cs](RegisteredCommand.cs.md), [CommandTests.cs](../../PluginSdkTests/CommandTests.cs.md)
