# PluginSdk/Commands/CommandRegistrationException.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** sealed class : `Exception` · **Lines:** 15

## Summary
`CommandRegistrationException` is the specific exception thrown by `CommandRegistry` when a module fails to register — for example when the `[CommandRoot]` prefix is already claimed by a different plugin, the prefix is the reserved word `"help"`, a command path starts with the reserved word `"help"`, or the prefix string is empty or contains spaces. Keeping it as a distinct type lets the host (the loader layer) catch registration errors per-plugin without swallowing unrelated exceptions.

## Types

### `CommandRegistrationException` — sealed class : `Exception`, public

Single-message constructor with no additional properties; the message string from `CommandRegistry` is the full diagnostic.

## Cross-references
- **Uses:** `System.Exception`
- **Used by:** [CommandService.cs](../../Legacy/Commands/CommandService.cs.md), [CommandRegistry.cs](CommandRegistry.cs.md), [CommandTests.cs](../../PluginSdkTests/CommandTests.cs.md)
