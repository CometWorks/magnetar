# Module: PluginSdk.Commands

**Project:** `PluginSdk` · **Files:** 17 · **Source lines:** 1289

## Purpose

Provides the full chat-command framework for Magnetar plugins: attribute-driven declaration of command handlers on CommandModule subclasses, a registry that organises them into prefix-keyed tries, a dispatcher that tokenises incoming chat messages and routes them to the right handler with argument binding and permission enforcement, and a static ServerCommands facade that plugins call to register their modules with the host.

## Role in Magnetar

Plugin-SDK contract layer between a plugin's command logic and the host's SE DS chat hook. The host installs an ICommandRegistrar and an ICommandResponder, wires the CommandDispatcher to the game's chat event, and the rest of the pipeline (parsing, resolution, permission, binding, reply dispatch) runs entirely within this module without touching SE DS internals directly.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `CommandModule` | class | [`PluginSdk/Commands/CommandModule.cs`](../descriptions/PluginSdk/Commands/CommandModule.cs.md) | Abstract base class plugins subclass to define a group of chat commands; a fresh instance is created per invocation. |
| `CommandAttribute` | class | [`PluginSdk/Commands/CommandAttribute.cs`](../descriptions/PluginSdk/Commands/CommandAttribute.cs.md) | Method-level attribute that marks a handler and provides its sub-path, description, and help text. |
| `CommandRootAttribute` | class | [`PluginSdk/Commands/CommandRootAttribute.cs`](../descriptions/PluginSdk/Commands/CommandRootAttribute.cs.md) | Class-level attribute that declares the !prefix namespace a CommandModule contributes to. |
| `PermissionAttribute` | class | [`PluginSdk/Commands/PermissionAttribute.cs`](../descriptions/PluginSdk/Commands/PermissionAttribute.cs.md) | Method-level attribute that sets the minimum MyPromoteLevel required to run a command; defaults to Admin when absent. |
| `CommandDispatcher` | class | [`PluginSdk/Commands/CommandDispatcher.cs`](../descriptions/PluginSdk/Commands/CommandDispatcher.cs.md) | Parses chat messages, resolves command paths, checks permissions, binds arguments, invokes handlers, and generates built-in help/overview/detail listings; multi-line help is preferentially shown as an in-game mission-screen popup (via PluginSdk.MissionScreens) and only falls back to line-by-line chat replies when no mission-screen host sender is available. |
| `CommandRegistry` | class | [`PluginSdk/Commands/CommandRegistry.cs`](../descriptions/PluginSdk/Commands/CommandRegistry.cs.md) | Stores all registered CommandRoot objects keyed by prefix and enforces registration invariants. |
| `CommandRoot` | class | [`PluginSdk/Commands/CommandRoot.cs`](../descriptions/PluginSdk/Commands/CommandRoot.cs.md) | Internal trie of RegisteredCommand nodes under one !prefix namespace; supports greedy path resolution. |
| `RegisteredCommand` | class | [`PluginSdk/Commands/RegisteredCommand.cs`](../descriptions/PluginSdk/Commands/RegisteredCommand.cs.md) | Internal per-command metadata cache (reflection info, permission level, syntax string) used by the dispatcher at runtime. |
| `CommandReply` | struct | [`PluginSdk/Commands/CommandReply.cs`](../descriptions/PluginSdk/Commands/CommandReply.cs.md) | Immutable value type carrying text, colour, font, author label, and broadcast flag for a single chat reply. |
| `CommandContext` | class | [`PluginSdk/Commands/CommandContext.cs`](../descriptions/PluginSdk/Commands/CommandContext.cs.md) | Per-invocation environment exposed to handlers via CommandModule.Context; provides caller identity, args, and Respond helpers. |
| `CommandCaller` | struct | [`PluginSdk/Commands/CommandCaller.cs`](../descriptions/PluginSdk/Commands/CommandCaller.cs.md) | Immutable snapshot of the issuing player's Steam ID, identity ID, name, and MyPromoteLevel. |
| `ServerCommands` | static class | [`PluginSdk/Commands/ServerCommands.cs`](../descriptions/PluginSdk/Commands/ServerCommands.cs.md) | Static plugin-facing facade for command registration; delegates to ICommandRegistrar installed by the host. |
| `ICommandRegistrar` | interface | [`PluginSdk/Commands/ICommandRegistrar.cs`](../descriptions/PluginSdk/Commands/ICommandRegistrar.cs.md) | Host-implemented sink for module registration; decouples plugins from the host's CommandRegistry. |
| `ICommandResponder` | interface | [`PluginSdk/Commands/ICommandResponder.cs`](../descriptions/PluginSdk/Commands/ICommandResponder.cs.md) | Host-implemented (or test-implemented) sink that delivers CommandReply values to game chat. |
| `ArgumentBinder` | static class | [`PluginSdk/Commands/ArgumentBinder.cs`](../descriptions/PluginSdk/Commands/ArgumentBinder.cs.md) | Converts ordered string token lists into typed object arrays for handler MethodInfo.Invoke calls. |
| `CommandLine` | static class | [`PluginSdk/Commands/CommandLine.cs`](../descriptions/PluginSdk/Commands/CommandLine.cs.md) | Tokeniser that splits a raw chat string into words, honouring double-quoted groups and backslash escapes. |
| `CommandRegistrationException` | class | [`PluginSdk/Commands/CommandRegistrationException.cs`](../descriptions/PluginSdk/Commands/CommandRegistrationException.cs.md) | Exception thrown by CommandRegistry when a module violates registration invariants. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`PluginSdk/Commands/ArgumentBinder.cs`](../descriptions/PluginSdk/Commands/ArgumentBinder.cs.md) | 155 | `ArgumentBinder` converts the ordered list of string tokens produced by `CommandLine.Tokenize` into the typed `object[]` array expected by a handler's `MethodInfo.Invoke` call. |
| [`PluginSdk/Commands/CommandAttribute.cs`](../descriptions/PluginSdk/Commands/CommandAttribute.cs.md) | 54 | `CommandAttribute` is the marker that turns a public instance method of a `CommandModule` subclass into a chat command handler. |
| [`PluginSdk/Commands/CommandCaller.cs`](../descriptions/PluginSdk/Commands/CommandCaller.cs.md) | 37 | `CommandCaller` is an immutable snapshot of the identity and permission level of the player (or server console) who issued a chat command. |
| [`PluginSdk/Commands/CommandContext.cs`](../descriptions/PluginSdk/Commands/CommandContext.cs.md) | 55 | `CommandContext` is the per-invocation environment that a command handler accesses through `CommandModule.Context`. |
| [`PluginSdk/Commands/CommandDispatcher.cs`](../descriptions/PluginSdk/Commands/CommandDispatcher.cs.md) | 308 | `CommandDispatcher` is the main entry point for chat message processing. |
| [`PluginSdk/Commands/CommandLine.cs`](../descriptions/PluginSdk/Commands/CommandLine.cs.md) | 69 | `CommandLine` provides a single `Tokenize` method that splits a raw chat string (with the leading `!` already stripped) into an ordered `List<string>` of tokens. |
| [`PluginSdk/Commands/CommandModule.cs`](../descriptions/PluginSdk/Commands/CommandModule.cs.md) | 21 | `CommandModule` is the plugin-facing base class for a group of chat commands. |
| [`PluginSdk/Commands/CommandRegistrationException.cs`](../descriptions/PluginSdk/Commands/CommandRegistrationException.cs.md) | 15 | `CommandRegistrationException` is the specific exception thrown by `CommandRegistry` when a module fails to register — for example when the `[CommandRoot]` prefix is already claimed by a different plugin, the prefix is the reserved word `"help"`, a command path starts with the reserved word `"help"`, or the prefix string is empty or contains spaces. |
| [`PluginSdk/Commands/CommandRegistry.cs`](../descriptions/PluginSdk/Commands/CommandRegistry.cs.md) | 124 | `CommandRegistry` is the authoritative store of all registered chat commands, keyed by root prefix. |
| [`PluginSdk/Commands/CommandReply.cs`](../descriptions/PluginSdk/Commands/CommandReply.cs.md) | 70 | `CommandReply` is the value type that carries a fully-specified chat message from a command handler back to the host's `ICommandResponder`. |
| [`PluginSdk/Commands/CommandRoot.cs`](../descriptions/PluginSdk/Commands/CommandRoot.cs.md) | 133 | `CommandRoot` owns the trie-like tree of commands registered under one `!prefix` namespace. |
| [`PluginSdk/Commands/CommandRootAttribute.cs`](../descriptions/PluginSdk/Commands/CommandRootAttribute.cs.md) | 49 | `CommandRootAttribute` declares the `!prefix` namespace that a `CommandModule` subclass contributes to. |
| [`PluginSdk/Commands/ICommandRegistrar.cs`](../descriptions/PluginSdk/Commands/ICommandRegistrar.cs.md) | 26 | `ICommandRegistrar` is the host-implemented sink through which plugins register their command modules. |
| [`PluginSdk/Commands/ICommandResponder.cs`](../descriptions/PluginSdk/Commands/ICommandResponder.cs.md) | 18 | `ICommandResponder` is the abstraction between the command dispatch pipeline and the actual SE DS chat API. |
| [`PluginSdk/Commands/PermissionAttribute.cs`](../descriptions/PluginSdk/Commands/PermissionAttribute.cs.md) | 28 | `PermissionAttribute` sets the minimum `MyPromoteLevel` a player must hold to invoke the decorated command. |
| [`PluginSdk/Commands/RegisteredCommand.cs`](../descriptions/PluginSdk/Commands/RegisteredCommand.cs.md) | 78 | `RegisteredCommand` is the internal representation of a single chat command as resolved from a `[Command]`-decorated method. |
| [`PluginSdk/Commands/ServerCommands.cs`](../descriptions/PluginSdk/Commands/ServerCommands.cs.md) | 49 | `ServerCommands` is the plugin-facing static facade for command registration, analogous to how `Harmony.PatchAll(Assembly)` is the entry point for Harmony patches. |

## Public API surface

- `ServerCommands.Register(Assembly assembly)`
- `ServerCommands.Register(Assembly assembly, params Type[] moduleTypes)`
- `ServerCommands.Registrar`
- `CommandDispatcher.Handle(string message, in CommandCaller caller, ICommandResponder responder)`
- `CommandRegistry.RegisterAssembly(Assembly assembly)`
- `CommandRegistry.RegisterModule(Type moduleType, string ownerId)`
- `CommandModule.Context`
- `CommandReply.Ok/Info/Error/Announce (static factories)`
- `ICommandRegistrar.Register(Assembly)`
- `ICommandResponder.Send(in CommandReply, in CommandCaller)`

## Dependencies

**Uses modules:** _none_  
**Used by modules:** [Legacy.Commands](Legacy.Commands.md), [Legacy.Loader](Legacy.Loader.md), [PluginSdkTests](PluginSdkTests.md)  
**External systems:** PluginSdk/MissionScreens.cs (CommandDispatcher renders help/overview/detail as mission-screen popups via MissionScreens.IsHostSenderAvailable and MissionScreens.ShowToPlayer); SE DS assemblies (VRage.Game.ModAPI.MyPromoteLevel, VRage.Game.MyFontEnum, VRageMath.Color)

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
