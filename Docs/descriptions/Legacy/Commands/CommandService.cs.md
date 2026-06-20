# Legacy/Commands/CommandService.cs

**Project:** Legacy · **Namespace:** `Pulsar.Legacy.Commands` · **Kind:** class · **Lines:** 114

## Summary
`CommandService` is the host-side owner of the chat-command pipeline for the Legacy (.NET Framework 4.8 / Windows) build of Magnetar. It holds a `CommandRegistry` populated by plugins (via the `ServerCommands` facade) and routes incoming global-chat messages through a `CommandDispatcher`. A single long-lived instance is created by `PluginLoader`, exposed as `PluginLoader.Instance.Commands`, and installed as the host's `ICommandRegistrar` so plugins can register command modules at any time. Owner attribution (for logging) is derived from the registering assembly's simple name.

## Types

### `CommandService` — class, public : `ICommandRegistrar`
Owns the full server-side command lifecycle: registration, dispatch, error routing, and caller identity resolution. It implements `ICommandRegistrar` so the `ServerCommands` static facade (PluginSdk) can delegate both the assembly-scan and explicit-type overloads. When `HandleChat` is called (from `Patch_ServerChat`), it resolves the Steam 64-bit ID into a `CommandCaller` (identity, display name, promote level) using `MySession.Static`, then calls `CommandDispatcher.Handle`. Returning `true` from `Handle` signals that the message was consumed by a command root and should be suppressed from the normal chat relay.

- **Fields:**
  - `registry` — private `CommandRegistry`; the flat map from command root names to registered `CommandRoot`/`CommandModule` trees
  - `dispatcher` — private `CommandDispatcher`; parses the raw chat text and invokes the matched handler with a `CommandContext`; wired with `OnHandlerError` as its error callback

- **Methods:**
  - `CommandService()` — constructs the `CommandRegistry` and wires a `CommandDispatcher` over it with `OnHandlerError`
  - `Register(Assembly)` — scans the given assembly for all `CommandModule` types carrying `CommandRootAttribute`, attributes them to the assembly's simple name, logs count; catches and logs `CommandRegistrationException` and unexpected exceptions without throwing
  - `Register(Assembly, params Type[])` — registers an explicit list of `CommandModule` types rather than scanning; same error-isolation pattern; last registration wins for conflicting roots
  - `HandleChat(ulong steamId, string text) → bool` — entry point called by the Harmony patch; null-guards `MySession.Static`; builds a `CommandCaller` via `BuildCaller`; delegates to `dispatcher.Handle`; returns `false` on any exception (fail-open to avoid suppressing ordinary chat)
  - `BuildCaller(ulong steamId) → CommandCaller` — private static; resolves `identityId` via `MySession.Static.Players.TryGetIdentityId`, promote level via `GetUserPromoteLevel`, display name via `TryGetIdentity`; falls back to the Steam ID string when no identity exists
  - `OnHandlerError(string message, Exception ex)` — private static error callback forwarded to `CommandDispatcher`; writes to `LogFile.Error`

## Cross-references
- **Uses:**
  - `PluginSdk/Commands/CommandRegistry.cs` — holds and populates the registry
  - `PluginSdk/Commands/CommandDispatcher.cs` — dispatches parsed chat commands
  - `PluginSdk/Commands/CommandCaller.cs` — caller value-type built from SE identity data
  - `PluginSdk/Commands/ICommandRegistrar.cs` — interface this class implements
  - `PluginSdk/Commands/CommandRegistrationException.cs` — caught and logged on bad registrations
  - `Legacy/Commands/ServerCommandResponder.cs` — passed as the `ICommandResponder` to `dispatcher.Handle`
  - `Shared/LogFile` (via `Pulsar.Shared`) — logging
  - SE DS `Sandbox.Game.World.MySession` — session null-guard and identity/promote-level lookup
  - SE DS `VRage.Game.ModAPI.MyPromoteLevel` — promote level for `CommandCaller`
- **Used by:** [PluginLoader.cs](../Loader/PluginLoader.cs.md), [Patch_ServerChat.cs](../Patch/Patch_ServerChat.cs.md)
