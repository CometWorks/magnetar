# PluginSdk/Commands/CommandRegistry.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** sealed class · **Lines:** 124

## Summary
`CommandRegistry` is the authoritative store of all registered chat commands, keyed by root prefix. It is built once at startup by the host, receives module registrations from each plugin (via `ICommandRegistrar` → `ServerCommands`), and is then handed to `CommandDispatcher`. It enforces the uniqueness invariants — no prefix may be claimed by two different plugins, the prefix `"help"` is reserved for the global listing, and no command path may start with `"help"` — and throws `CommandRegistrationException` on any violation so the host can isolate the offending plugin.

## Types

### `CommandRegistry` — sealed class, public

Stores `CommandRoot` objects in a case-insensitive `Dictionary<string, CommandRoot>` keyed by prefix. The default permission level applied when a handler method carries no `[Permission]` attribute is `MyPromoteLevel.Admin` (constant `DefaultPermission`).

- **Fields:** `roots` (private `Dictionary<string, CommandRoot>`) — all registered roots.
- **Properties:**
  - `Prefixes` — `IReadOnlyCollection<string>` of currently registered prefix strings.
- **Methods:**
  - `RegisterAssembly(Assembly assembly) → int` — Scans all types in the assembly for non-abstract `CommandModule` subclasses decorated with `[CommandRoot]`, calls `RegisterModule` for each, and returns the count of modules registered. Ownership is attributed to the assembly's simple name.
  - `RegisterModule(Type moduleType, string ownerId)` — Validates the type (must be a `CommandModule`, must have `[CommandRoot]`, prefix must be a non-empty single word, prefix must not be `"help"`), then iterates the type's declared public instance methods, reads `[Command]` and `[Permission]` attributes, validates that no command path starts with `"help"`, and adds a `RegisteredCommand` to the appropriate `CommandRoot`.
  - `TryGetRoot(string prefix, out CommandRoot root) → bool` (internal) — Prefix lookup used by `CommandDispatcher`.
  - `EnumerateRoots() → IEnumerable<CommandRoot>` (internal) — Returns all roots for the global `!help` listing.
  - `GetOrCreateRoot(string prefix, CommandRootAttribute attr, string ownerId) → CommandRoot` (private) — Returns the existing root for `prefix` or creates a new one; does not check cross-plugin ownership here (ownership conflict would require tracking per-root owner, which is done via `OwnerId` on the root).
  - `SplitPath(string command) → List<string>` (private static) — Splits a space-separated command string into path segments, ignoring empty parts.

## Cross-references
- **Uses:** `PluginSdk/Commands/CommandRoot.cs`, `PluginSdk/Commands/CommandRootAttribute.cs`, `PluginSdk/Commands/CommandAttribute.cs`, `PluginSdk/Commands/PermissionAttribute.cs`, `PluginSdk/Commands/RegisteredCommand.cs`, `PluginSdk/Commands/CommandRegistrationException.cs`, `VRage.Game.ModAPI.MyPromoteLevel` (SE DS assembly)
- **Used by:** [CommandService.cs](../../Legacy/Commands/CommandService.cs.md), [CommandDispatcher.cs](CommandDispatcher.cs.md), [CommandTests.cs](../../PluginSdkTests/CommandTests.cs.md)
