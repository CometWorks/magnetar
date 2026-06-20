# PluginSdk/Commands/RegisteredCommand.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** sealed class · **Lines:** 78

## Summary
`RegisteredCommand` is the internal representation of a single chat command as resolved from a `[Command]`-decorated method. It caches all reflection metadata needed to permission-check, bind arguments, and invoke the handler at dispatch time. It is created by `CommandRegistry.RegisterModule` and stored in the `CommandRoot` trie. Plugins never interact with this type directly.

## Types

### `RegisteredCommand` — sealed class, internal

Per-command metadata store. Instantiation happens once per registration; `Invoke` is called at every dispatch.

- **Properties:**
  - `OwnerId` — Assembly simple name that owns this command; used for diagnostics.
  - `Prefix` — Root prefix (e.g., `"ess"`).
  - `Path` — `IReadOnlyList<string>` of path segments relative to the root (e.g., `["grid", "list"]`). Empty for the default (bare `!prefix`) command.
  - `Description` — One-line description from `[Command]`.
  - `HelpText` — Extended help text; falls back to `Description` when the attribute's `HelpText` is null or empty.
  - `MinPromoteLevel` — `MyPromoteLevel` from `[Permission]`, or `Admin` when absent.
  - `ModuleType` — `Type` of the `CommandModule` subclass; used by `Activator.CreateInstance` in `Invoke`.
  - `Method` — `MethodInfo` of the handler method; used by `Method.Invoke`.
  - `Parameters` — `ParameterInfo[]` cached from `method.GetParameters()`; passed to `ArgumentBinder.TryBind`.
  - `Syntax` — Auto-generated usage string (e.g., `!ess tp <target> [distance]`); built once in the constructor by `BuildSyntax`.
- **Methods:**
  - `IsVisibleTo(MyPromoteLevel level) → bool` — Returns `level >= MinPromoteLevel`; used to filter overview/help listings and to gate execution.
  - `Invoke(CommandContext context, object[] values) → object` — Creates a fresh `ModuleType` instance via `Activator.CreateInstance`, assigns `Context`, then calls `Method.Invoke(module, values)`. Returns the handler's return value (consumed by `CommandDispatcher.DispatchResult`).
  - `BuildSyntax(string prefix, IReadOnlyList<string> path, ParameterInfo[] parameters) → string` (private static) — Concatenates `!prefix path…` followed by required params in `<name>` brackets, optional params in `[name]` brackets, and trailing `params` arrays as `[name...]`.

## Cross-references
- **Uses:** `PluginSdk/Commands/CommandModule.cs`, `PluginSdk/Commands/CommandContext.cs`, `PluginSdk/Commands/ArgumentBinder.cs`, `VRage.Game.ModAPI.MyPromoteLevel` (SE DS assembly)
- **Used by:** [CommandDispatcher.cs](CommandDispatcher.cs.md), [CommandRegistry.cs](CommandRegistry.cs.md), [CommandRoot.cs](CommandRoot.cs.md)
