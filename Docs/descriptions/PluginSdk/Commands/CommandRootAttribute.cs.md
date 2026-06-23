# PluginSdk/Commands/CommandRootAttribute.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** sealed class (Attribute) · **Lines:** 49

## Summary
`CommandRootAttribute` declares the `!prefix` namespace that a `CommandModule` subclass contributes to. It is applied at the class level and is read by `CommandRegistry.RegisterModule` to determine the root prefix, the human-readable title used as the chat sender label, and the optional description shown in the global `!help` listing. Multiple modules — even from different plugins — may share the same prefix; their commands are merged into one `CommandRoot`. If two commands resolve to the same path, the last registration wins (a later plugin may override an earlier one); there is no cross-plugin conflict check.

## Types

### `CommandRootAttribute` — sealed class : `Attribute`, public

Applied with `[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]`. The `Title` defaults to `Prefix` inside `CommandRegistry.GetOrCreateRoot` when left `null`.

- **Properties:**
  - `Prefix` — The token after `!` (e.g., `"ess"`). Must be a single word (no spaces). Matched case-insensitively. The reserved word `"help"` is rejected at registration time.
  - `Title` — Human-readable name used as the chat sender label in replies and in the overview header. Defaults to `Prefix` when `null`.
  - `Description` — One-line description shown next to `!prefix` in the global `!help` listing; optional.

## Cross-references
- **Uses:** `PluginSdk/Commands/CommandModule.cs` (applied to its subclasses)
- **Used by:** [CommandRegistry.cs](CommandRegistry.cs.md)
