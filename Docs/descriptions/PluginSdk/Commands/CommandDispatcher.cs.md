# PluginSdk/Commands/CommandDispatcher.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk.Commands` · **Kind:** sealed class · **Lines:** 308

## Summary
`CommandDispatcher` is the main entry point for chat message processing. The host calls `Handle` for every chat message; the dispatcher tokenizes the line, resolves the prefix and command path against the `CommandRegistry`, enforces permission, binds arguments via `ArgumentBinder`, and invokes the handler. It also generates the built-in `!help` global listing, the per-root `!{prefix}` overview, and `!{prefix} help <command>` detailed help, all filtered by the caller's promote level. Every multi-line help/overview/detail response is first offered to the caller as an in-game mission-screen popup via `PluginSdk.MissionScreens`; only when no mission-screen host sender is available does it fall back to line-by-line chat replies. Because it depends only on `CommandCaller`, `ICommandResponder`, and the optional mission-screen sender, it can be exercised in unit tests without a live SE session.

## Types

### `CommandDispatcher` — sealed class, public

Holds a `CommandRegistry` reference (read-only after construction) and an optional error callback. The `Prefix` property (default `'!'`) controls the command-initiating character.

- **Fields:** `registry` (private `CommandRegistry`), `onError` (private `Action<string, Exception>`)
- **Properties:**
  - `Prefix` — Character that triggers command processing (default `'!'`).
- **Methods:**
  - `CommandDispatcher(CommandRegistry registry, Action<string, Exception> onError = null)` — Constructor; `onError` receives a descriptive string and the exception whenever a handler throws, so the host can log it without crashing the dispatch loop. Throws `ArgumentNullException` if `registry` is null.
  - `Handle(string message, in CommandCaller caller, ICommandResponder responder) → bool` — Public entry point. Returns `true` if the message was recognised as a command (and should be suppressed from normal chat), `false` otherwise. Internal flow:
    1. Returns `false` immediately if the message is empty or does not begin with `Prefix`.
    2. Strips the leading prefix character and tokenizes via `CommandLine.Tokenize`; empty token list → returns `false`.
    3. If the first token is `"help"`, dispatches to `SendOverview` for a known named root (`!help <prefix>`) or to `SendGlobalHelp` otherwise (including for an unknown prefix), then returns `true`.
    4. Looks up the root prefix; unknown prefix → returns `false` (ordinary chat).
    5. Bare `!prefix` → runs the root's `Default` command if it exists and is visible to the caller, otherwise prints the overview.
    6. `!prefix help [path]` → calls `SendHelp`.
    7. Resolves the command path via `root.TryResolve`; unknown path → error reply.
    8. Checks visibility via `command.IsVisibleTo`; insufficient level → permission error reply.
    9. Calls `ExecuteCommand`, which binds args and invokes the handler.
  - `ExecuteCommand(...)` (private) — Calls `ArgumentBinder.TryBind`; on failure replies with the bind error plus the command `Syntax`. On success creates a `CommandContext`, invokes `command.Invoke`, and routes the return value through `DispatchResult`. Catches exceptions, unwraps `TargetInvocationException`, reports via `onError`, and sends a generic error reply.
  - `DispatchResult(object result, CommandContext context)` (private static) — Interprets the handler's return value: `null`/`void` → no reply; non-empty `string` → `Respond(text)`; `CommandReply` with content → `Respond(reply)`; `IEnumerable` → one `Respond` per item, accepting mixed `string`/`CommandReply` items and skipping empty/contentless ones; any other object → `result.ToString()`.
  - `SendGlobalHelp(CommandCaller caller, ICommandResponder responder)` (private) — Builds an "=== Server Commands ===" listing of every root whose `IsAvailableTo(caller.PromoteLevel)` is true, sorted alphabetically by prefix, each line showing `!prefix` plus the root `Description` when present (or a "No server commands available" line when none). Hands the assembled lines to `TryShowMissionHelp` (screen title "Magnetar Help", objective "Server Commands"); if that returns `false`, falls back to `ReplyLines` under author "Help".
  - `SendOverview(CommandRoot root, CommandCaller caller, ICommandResponder responder)` (private static) — Lists every command in `root` visible to the caller, with a header derived from the root `Title`/`Description` and per-command lines built from path and `Description` (or a "No server commands available" line when none). Offers the lines via `TryShowMissionHelp` (objective "Command Overview"); falls back to `ReplyLines` under the root `Title`.
  - `SendHelp(CommandRoot root, List<string> path, CommandCaller caller, ICommandResponder responder)` (private static) — Empty path → defers to `SendOverview`. Otherwise resolves the path; an unresolved or non-visible command yields a "No such command" error reply. On success builds `Usage: <Syntax>` plus the optional `HelpText`, offers them via `TryShowMissionHelp` (objective "Command Help"), and falls back to `ReplyLines` under the root `Title`.
  - `TryShowMissionHelp(CommandCaller caller, string screenTitle, string currentObjective, List<string> lines) → bool` (private static) — Returns `false` immediately when `PluginSdk.MissionScreens.IsHostSenderAvailable` is false. Otherwise joins the lines into a body and calls `PluginSdk.MissionScreens.ShowToPlayer(caller.IdentityId, screenTitle, null, currentObjective, body, "Close")`, returning its result. A `true` return means the popup was shown and chat fallback is skipped.
  - `ReplyLines(ICommandResponder responder, CommandCaller caller, string author, List<string> lines)` (private static) — Chat fallback: sends each line as a separate `CommandReply.Info` tagged with `author`.
  - `Reply(ICommandResponder responder, CommandCaller caller, in CommandReply reply)` (private static) — Thin wrapper around `responder.Send`.

## Cross-references
- **Uses:** `PluginSdk/Commands/ArgumentBinder.cs`, `PluginSdk/Commands/CommandContext.cs`, `PluginSdk/Commands/CommandLine.cs`, `PluginSdk/Commands/CommandRegistry.cs`, `PluginSdk/Commands/CommandReply.cs`, `PluginSdk/Commands/CommandRoot.cs`, `PluginSdk/Commands/RegisteredCommand.cs`, `PluginSdk/Commands/ICommandResponder.cs`, `PluginSdk/Commands/CommandCaller.cs`, `PluginSdk/MissionScreens.cs`
- **Used by:** [CommandService.cs](../../Legacy/Commands/CommandService.cs.md), [CommandTests.cs](../../PluginSdkTests/CommandTests.cs.md)
