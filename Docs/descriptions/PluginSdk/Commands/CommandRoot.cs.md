# PluginSdk/Commands/CommandRoot.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** sealed class · **Lines:** 133

## Summary
`CommandRoot` owns the trie-like tree of commands registered under one `!prefix` namespace. It stores commands as a tree of `Node` objects keyed case-insensitively by path segment, enabling O(depth) resolution of multi-word paths (e.g., `["grid", "list"]` resolves by walking two `Node.Children` dictionaries). It is an internal type — plugins never interact with it directly; it is managed exclusively by `CommandRegistry` and read by `CommandDispatcher`.

## Types

### `CommandRoot` — sealed class, internal

Each `CommandRoot` corresponds to exactly one `!prefix` entry in `CommandRegistry.roots`. The `Default` property holds the command for a bare `!prefix` invocation (an empty `[Command("")]` path); `null` when no default is registered.

- **Properties:**
  - `Prefix` — The `!prefix` token (e.g., `"ess"`).
  - `Title` — Human-readable name shown in chat sender label and overview headers.
  - `Description` — Optional one-line description shown in `!help` listing.
  - `OwnerId` — Assembly simple name that registered this root.
  - `Default` — `RegisteredCommand` for a bare `!prefix`; `null` if none.
- **Methods:**
  - `Add(RegisteredCommand command)` — Walks or creates `Node` children along `command.Path` and sets `node.Command`. Empty path sets `Default`. Last-registration-wins semantics (overwriting is allowed).
  - `TryResolve(IReadOnlyList<string> tokens, out RegisteredCommand command, out int consumed) → bool` — Greedily walks the trie consuming tokens; returns the deepest `Command` found and the number of tokens consumed (the remainder are handler arguments). Returns `false` when no command node is reached.
  - `IsAvailableTo(MyPromoteLevel level) → bool` — Returns `true` if at least one command (including `Default`) is visible to the given promote level. Used by the global `!help` listing to filter out roots the caller cannot use.
  - `EnumerateCommands() → IEnumerable<RegisteredCommand>` — DFS traversal of the `Node` tree returning all non-null commands, sorted alphabetically by joined path string.

### `Node` — sealed class (private nested), internal

Minimal trie node. Holds a case-insensitive `Dictionary<string, Node>` of children and a nullable `RegisteredCommand Command`.

## Cross-references
- **Uses:** `PluginSdk/Commands/RegisteredCommand.cs`, `VRage.Game.ModAPI.MyPromoteLevel` (SE DS assembly)
- **Used by:** [MagnetarCommands.cs](../../Legacy/Commands/MagnetarCommands.cs.md), [CommandDispatcher.cs](CommandDispatcher.cs.md), [CommandRegistry.cs](CommandRegistry.cs.md), [CommandTests.cs](../../PluginSdkTests/CommandTests.cs.md)
