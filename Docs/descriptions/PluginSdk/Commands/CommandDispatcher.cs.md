# PluginSdk/Commands/CommandDispatcher.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** sealed class · **Lines:** 245

## Summary
`CommandDispatcher` is the main entry point for chat message processing. The host calls `Handle` for every chat message; the dispatcher tokenizes the line, resolves the prefix and command path against the `CommandRegistry`, enforces permission, binds arguments via `ArgumentBinder`, and invokes the handler. It also generates the built-in `!help` global listing and the per-root `!{prefix}` overview and `!{prefix} help <command>` detailed help, all filtered by the caller's promote level. Because it depends only on `CommandCaller` and `ICommandResponder`, it can be exercised in unit tests without a live SE session.

## Types

### `CommandDispatcher` — sealed class, public

Holds a `CommandRegistry` reference (read-only after construction) and an optional error callback. The `Prefix` property (default `'!'`) controls the command-initiating character.

- **Fields:** `registry` (private `CommandRegistry`), `onError` (private `Action<string, Exception>`)
- **Properties:**
  - `Prefix` — Character that triggers command processing (default `'!'`).
- **Methods:**
  - `CommandDispatcher(CommandRegistry registry, Action<string, Exception> onError)` — Constructor; `onError` receives a descriptive string and the exception whenever a handler throws, so the host can log it without crashing the dispatch loop.
  - `Handle(string message, in CommandCaller caller, ICommandResponder responder) → bool` — Public entry point. Returns `true` if the message was recognised as a command (and should be suppressed from normal chat), `false` otherwise. Internal flow:
    1. Strips the leading prefix character and tokenizes via `CommandLine.Tokenize`.
    2. If the first token is `"help"`, dispatches to `SendGlobalHelp` or `SendOverview` for a named root.
    3. Looks up the root prefix; unknown prefix → returns `false` (ordinary chat).
    4. Bare `!prefix` → runs the root's `Default` command if the caller can use it, otherwise prints the overview.
    5. `!prefix help [path]` → calls `SendHelp`.
    6. Resolves the command path via `root.TryResolve`; unknown path → error reply.
    7. Checks permission; insufficient level → error reply.
    8. Calls `ExecuteCommand`, which binds args and invokes the handler.
  - `ExecuteCommand(...)` (private) — Calls `ArgumentBinder.TryBind`; on success creates a `CommandContext`, invokes `command.Invoke`, and routes the return value through `DispatchResult`. Catches exceptions and sends a generic error reply; unwraps `TargetInvocationException`.
  - `DispatchResult(object result, CommandContext context)` (private static) — Interprets the handler's return value: `null`/`void` → no reply; `string` → `Respond(text)`; `CommandReply` → `Respond(reply)`; `IEnumerable` → one `Respond` per item (supports mixed `string`/`CommandReply` sequences); any other object → `result.ToString()`.
  - `SendGlobalHelp(...)` (private) — Lists every root whose `IsAvailableTo(caller.PromoteLevel)` returns true, sorted alphabetically by prefix.
  - `SendOverview(CommandRoot root, ...)` (private static) — Lists every command in `root` visible to the caller, sorted by path.
  - `SendHelp(CommandRoot root, List<string> path, ...)` (private static) — Shows syntax and `HelpText` for a specific command; falls back to overview when path is empty.
  - `Reply(ICommandResponder responder, CommandCaller caller, in CommandReply reply)` (private static) — Thin wrapper around `responder.Send`.

## Cross-references
- **Uses:** `PluginSdk/Commands/ArgumentBinder.cs`, `PluginSdk/Commands/CommandContext.cs`, `PluginSdk/Commands/CommandLine.cs`, `PluginSdk/Commands/CommandRegistry.cs`, `PluginSdk/Commands/CommandReply.cs`, `PluginSdk/Commands/CommandRoot.cs`, `PluginSdk/Commands/RegisteredCommand.cs`, `PluginSdk/Commands/ICommandResponder.cs`, `PluginSdk/Commands/CommandCaller.cs`
- **Used by:** [CommandService.cs](../../Legacy/Commands/CommandService.cs.md), [CommandTests.cs](../../PluginSdkTests/CommandTests.cs.md)
