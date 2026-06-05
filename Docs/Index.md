# Magnetar — Full File Index

Every documented source file, grouped by module. 123 files across 16 modules.

[◀ Back to TOC](TOC.md)

## Compiler  ·  [module doc](modules/Compiler.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Compiler/LogFile.cs`](descriptions/Compiler/LogFile.cs.md) | 79 | 2 | Minimal NLog-backed file logger used by the Compiler module to record Roslyn reference loading, publicizing, and compilation diagnostics to a flat `info.log` file. |
| [`Compiler/PublicizedAssemblies.cs`](descriptions/Compiler/PublicizedAssemblies.cs.md) | 77 | 2 | Bridges Roslyn source analysis with assembly publicizing. |
| [`Compiler/Publicizer.cs`](descriptions/Compiler/Publicizer.cs.md) | 151 | 1 | Performs the actual IL-level publicizing of an SE DS assembly using Mono.Cecil: it reads the assembly from disk, forces every non-public type, field, method, and property to public, and re-emits it to an in-memory `MetadataReference` for Roslyn. |
| [`Compiler/RoslynCompiler.cs`](descriptions/Compiler/RoslynCompiler.cs.md) | 171 | 1 | The core in-process C# compiler used to build local/Workshop plugins from source at server startup. |
| [`Compiler/RoslynReferences.cs`](descriptions/Compiler/RoslynReferences.cs.md) | 84 | 2 | Builds and caches the global set of Roslyn `MetadataReference`s that plugins are compiled against — essentially the SE Dedicated Server / VRage / framework assembly closure. |

## Legacy.Commands  ·  [module doc](modules/Legacy.Commands.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Commands/CommandService.cs`](descriptions/Legacy/Commands/CommandService.cs.md) | 114 | 2 | `CommandService` is the host-side owner of the chat-command pipeline for the Legacy (.NET Framework 4.8 / Windows) build of Magnetar. |
| [`Legacy/Commands/MagnetarCommands.cs`](descriptions/Legacy/Commands/MagnetarCommands.cs.md) | 45 | 2 | Declares three built-in chat-command modules — `!save`, `!restart`, and `!quit` — that Magnetar registers with `CommandService` before any plugin loads. |
| [`Legacy/Commands/ServerCommandResponder.cs`](descriptions/Legacy/Commands/ServerCommandResponder.cs.md) | 37 | 2 | `ServerCommandResponder` is the `ICommandResponder` implementation that delivers command replies into the SE DS chat system. |

## Legacy.Integration  ·  [module doc](modules/Legacy.Integration.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Compiler/Interim.cs`](descriptions/Legacy/Compiler/Interim.cs.md) | 147 | 2 | Active only under `#if NETCOREAPP` (the Interim/.NET 10 build). |
| [`Legacy/Compiler/Legacy.cs`](descriptions/Legacy/Compiler/Legacy.cs.md) | 86 | 2 | Active only under `#if NETFRAMEWORK` (the .NET Framework 4.8 / Windows build). |
| [`Legacy/Compiler/References.cs`](descriptions/Legacy/Compiler/References.cs.md) | 36 | 2 | Provides the list of assembly references that the Roslyn compiler must know about when compiling SE scripts and plugins. |
| [`Legacy/Extensions/ModPlugin.cs`](descriptions/Legacy/Extensions/ModPlugin.cs.md) | 31 | 2 | Extends `ModPlugin` (the Magnetar data type representing a Steam Workshop mod) with the SE DS API objects needed to register a mod with the game engine at runtime. |
| [`Legacy/Paths/PathResolverBinder.cs`](descriptions/Legacy/Paths/PathResolverBinder.cs.md) | 77 | 2 | Wires the `PluginSdk.Paths.PathResolver` facade to the LinuxCompat plugin's case-insensitive path cache at startup. |
| [`Legacy/Paths/ReflectionPathResolver.cs`](descriptions/Legacy/Paths/ReflectionPathResolver.cs.md) | 94 | 2 | An `IPathResolver` backend that forwards path operations to the LinuxCompat plugin's `PathHelpers` and `PathCache` static methods via pre-bound delegates. |

## Legacy.Launcher  ·  [module doc](modules/Legacy.Launcher.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Launcher/Daemon.cs`](descriptions/Legacy/Launcher/Daemon.cs.md) | 160 | 2 | Detaches the running process from its parent when `-daemon` is set, so the parent terminating does not take the dedicated server down with it. |
| [`Legacy/Launcher/Folder.cs`](descriptions/Legacy/Launcher/Folder.cs.md) | 161 | 2 | Locates the Space Engineers Dedicated Server `DedicatedServer64` installation directory so the launcher knows which game binaries to load and patch. |
| [`Legacy/Launcher/Game.cs`](descriptions/Legacy/Launcher/Game.cs.md) | 141 | 2 | Thin bridge between Magnetar's launcher and the Space Engineers DS engine internals (`Sandbox`, `VRage`). |
| [`Legacy/Launcher/ServerControl.cs`](descriptions/Legacy/Launcher/ServerControl.cs.md) | 512 | 1 | Single source of truth for the dedicated server's lifecycle operations — save world, reload dedicated config, quit, and restart — with and without saving. |
| [`Legacy/Program.cs`](descriptions/Legacy/Program.cs.md) | 389 | 1 | Entry point for the Magnetar launcher. |

## Legacy.Loader  ·  [module doc](modules/Legacy.Loader.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Loader/LoaderTools.cs`](descriptions/Legacy/Loader/LoaderTools.cs.md) | 137 | 2 | Process-level utilities for the loader: restarting the dedicated server process with adjusted command-line arguments, and force-precompiling (JIT-preparing) plugin assemblies so member-access errors surface immediately instead of mid-game. |
| [`Legacy/Loader/NativeLibraryPreloader.cs`](descriptions/Legacy/Loader/NativeLibraryPreloader.cs.md) | 154 | 1 | Linux-only native-library bootstrap that runs once at the very top of `Main()`. |
| [`Legacy/Loader/PluginInstance.cs`](descriptions/Legacy/Loader/PluginInstance.cs.md) | 336 | 1 | Runtime wrapper around a single loaded plugin: it locates the plugin's `IPlugin` implementation type in the compiled assembly, instantiates it, performs reflection-based dependency injection of loader services into well-known static fields/methods, and drives the SE plugin lifecycle (`Init` / `Update` / `HandleInput` / `Dispose`). |
| [`Legacy/Loader/PluginLoader.cs`](descriptions/Legacy/Loader/PluginLoader.cs.md) | 217 | 1 | The top-level plugin host: a singleton `IHandleInputPlugin` that SE itself drives (`Init`/`Update`/`HandleInput`/`Dispose`). |
| [`Legacy/Loader/SteamMods.cs`](descriptions/Legacy/Loader/SteamMods.cs.md) | 88 | 2 | Downloads/updates Steam Workshop items (mod-plugins referenced by the active profile) by reproducing SE's own blocking workshop-download path. |

## Legacy.Patch  ·  [module doc](modules/Legacy.Patch.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Legacy/Patch/Patch_Compile.cs`](descriptions/Legacy/Patch/Patch_Compile.cs.md) | 65 | 2 | Postfix-patches `MyScriptCompiler.AnalyzeDiagnostics` to intercept Roslyn compilation failures before they reach SE's own error pipeline. |
| [`Legacy/Patch/Patch_ComponentRegistered.cs`](descriptions/Legacy/Patch/Patch_ComponentRegistered.cs.md) | 20 | 3 | Prefix-patches `MySession.RegisterComponentsFromAssembly` to inject plugin-provided session components at exactly the right moment in the SE session lifecycle. |
| [`Legacy/Patch/Patch_DedicatedServerRun.cs`](descriptions/Legacy/Patch/Patch_DedicatedServerRun.cs.md) | 78 | 2 | Transpiler-patches `VRage.Dedicated.DedicatedServer.Run` to remove the Telerik/WinForms configuration UI and the Windows Service branch, replacing the entire method body with a minimal headless startup sequence. |
| [`Legacy/Patch/Patch_ExitThreadSafe.cs`](descriptions/Legacy/Patch/Patch_ExitThreadSafe.cs.md) | 20 | 3 | Prefix-patches `MySandboxGame.ExitThreadSafe` to redirect in-game and admin-triggered exit requests through Magnetar's graceful shutdown path. |
| [`Legacy/Patch/Patch_LoadScripts.cs`](descriptions/Legacy/Patch/Patch_LoadScripts.cs.md) | 17 | 3 | Postfix-patches `MyScriptManager.LoadScripts` to trigger plugin entity-component registration at the correct point in session startup. |
| [`Legacy/Patch/Patch_MyDefinitionErrors.cs`](descriptions/Legacy/Patch/Patch_MyDefinitionErrors.cs.md) | 40 | 2 | Prefix-patches `MyDefinitionErrors.Add` to intercept Roslyn compilation-failure error messages and redirect them to Magnetar's own log, replacing SE's raw, path-cluttered error string with a cleaner structured output that pairs the mod name with the per-diagnostic messages already collected by `Patch_Compile`. |
| [`Legacy/Patch/Patch_MyDefinitionManager.cs`](descriptions/Legacy/Patch/Patch_MyDefinitionManager.cs.md) | 42 | 2 | Prefix-patches `MyDefinitionManager.LoadData` to inject client-side mod definitions for any `ModPlugin` entries in the active Magnetar configuration profile before SE processes the mod list. |
| [`Legacy/Patch/Patch_MyScriptManager.cs`](descriptions/Legacy/Patch/Patch_MyScriptManager.cs.md) | 78 | 2 | Postfix-patches `MyScriptManager.LoadData` to compile and load scripts for client-side `ModPlugin` entries after SE has processed all normal session mods. |
| [`Legacy/Patch/Patch_MySessionLoader.cs`](descriptions/Legacy/Patch/Patch_MySessionLoader.cs.md) | 34 | 2 | Contains two Harmony Prefix patches on `MySessionLoader.LoadMultiplayerScenarioWorld` and `MySessionLoader.LoadMultiplayerSession` that enforce a "trusted mods" security policy. |
| [`Legacy/Patch/Patch_PrepareCrashReport.cs`](descriptions/Legacy/Patch/Patch_PrepareCrashReport.cs.md) | 44 | 2 | Prefix-patches `VRage.Platform.Windows.MyCrashReporting.PrepareCrashAnalyticsReporting` to redirect the SE crash reporter to run the correct `SpaceEngineers.exe` binary, which in Magnetar's in-process hosting model is not necessarily the process that crashed. |
| [`Legacy/Patch/Patch_ServerChat.cs`](descriptions/Legacy/Patch/Patch_ServerChat.cs.md) | 50 | 2 | Prefix-patches `MyMultiplayerBase.OnChatMessageReceived_Server` to intercept global chat messages whose text begins with `'!'` and route them through Magnetar's `CommandService` before SE can broadcast them to other players. |

## PluginSdk.Commands  ·  [module doc](modules/PluginSdk.Commands.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdk/Commands/ArgumentBinder.cs`](descriptions/PluginSdk/Commands/ArgumentBinder.cs.md) | 155 | 2 | `ArgumentBinder` converts the ordered list of string tokens produced by `CommandLine.Tokenize` into the typed `object[]` array expected by a handler's `MethodInfo.Invoke` call. |
| [`PluginSdk/Commands/CommandAttribute.cs`](descriptions/PluginSdk/Commands/CommandAttribute.cs.md) | 54 | 2 | `CommandAttribute` is the marker that turns a public instance method of a `CommandModule` subclass into a chat command handler. |
| [`PluginSdk/Commands/CommandCaller.cs`](descriptions/PluginSdk/Commands/CommandCaller.cs.md) | 37 | 2 | `CommandCaller` is an immutable snapshot of the identity and permission level of the player (or server console) who issued a chat command. |
| [`PluginSdk/Commands/CommandContext.cs`](descriptions/PluginSdk/Commands/CommandContext.cs.md) | 55 | 2 | `CommandContext` is the per-invocation environment that a command handler accesses through `CommandModule.Context`. |
| [`PluginSdk/Commands/CommandDispatcher.cs`](descriptions/PluginSdk/Commands/CommandDispatcher.cs.md) | 245 | 1 | `CommandDispatcher` is the main entry point for chat message processing. |
| [`PluginSdk/Commands/CommandLine.cs`](descriptions/PluginSdk/Commands/CommandLine.cs.md) | 69 | 2 | `CommandLine` provides a single `Tokenize` method that splits a raw chat string (with the leading `!` already stripped) into an ordered `List<string>` of tokens. |
| [`PluginSdk/Commands/CommandModule.cs`](descriptions/PluginSdk/Commands/CommandModule.cs.md) | 21 | 3 | `CommandModule` is the plugin-facing base class for a group of chat commands. |
| [`PluginSdk/Commands/CommandRegistrationException.cs`](descriptions/PluginSdk/Commands/CommandRegistrationException.cs.md) | 15 | 3 | `CommandRegistrationException` is the specific exception thrown by `CommandRegistry` when a module fails to register — for example when the `[CommandRoot]` prefix is already claimed by a different plugin, the prefix is the reserved word `"help"`, a command path starts with the reserved word `"help"`, or the prefix string is empty or contains spaces. |
| [`PluginSdk/Commands/CommandRegistry.cs`](descriptions/PluginSdk/Commands/CommandRegistry.cs.md) | 124 | 2 | `CommandRegistry` is the authoritative store of all registered chat commands, keyed by root prefix. |
| [`PluginSdk/Commands/CommandReply.cs`](descriptions/PluginSdk/Commands/CommandReply.cs.md) | 70 | 2 | `CommandReply` is the value type that carries a fully-specified chat message from a command handler back to the host's `ICommandResponder`. |
| [`PluginSdk/Commands/CommandRoot.cs`](descriptions/PluginSdk/Commands/CommandRoot.cs.md) | 133 | 2 | `CommandRoot` owns the trie-like tree of commands registered under one `!prefix` namespace. |
| [`PluginSdk/Commands/CommandRootAttribute.cs`](descriptions/PluginSdk/Commands/CommandRootAttribute.cs.md) | 49 | 2 | `CommandRootAttribute` declares the `!prefix` namespace that a `CommandModule` subclass contributes to. |
| [`PluginSdk/Commands/ICommandRegistrar.cs`](descriptions/PluginSdk/Commands/ICommandRegistrar.cs.md) | 26 | 2 | `ICommandRegistrar` is the host-implemented sink through which plugins register their command modules. |
| [`PluginSdk/Commands/ICommandResponder.cs`](descriptions/PluginSdk/Commands/ICommandResponder.cs.md) | 18 | 3 | `ICommandResponder` is the abstraction between the command dispatch pipeline and the actual SE DS chat API. |
| [`PluginSdk/Commands/PermissionAttribute.cs`](descriptions/PluginSdk/Commands/PermissionAttribute.cs.md) | 28 | 2 | `PermissionAttribute` sets the minimum `MyPromoteLevel` a player must hold to invoke the decorated command. |
| [`PluginSdk/Commands/RegisteredCommand.cs`](descriptions/PluginSdk/Commands/RegisteredCommand.cs.md) | 78 | 2 | `RegisteredCommand` is the internal representation of a single chat command as resolved from a `[Command]`-decorated method. |
| [`PluginSdk/Commands/ServerCommands.cs`](descriptions/PluginSdk/Commands/ServerCommands.cs.md) | 49 | 2 | `ServerCommands` is the plugin-facing static facade for command registration, analogous to how `Harmony.PatchAll(Assembly)` is the entry point for Harmony patches. |

## PluginSdk.Config  ·  [module doc](modules/PluginSdk.Config.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdk/Config/ConfigAttributes.cs`](descriptions/PluginSdk/Config/ConfigAttributes.cs.md) | 405 | 1 | Declares the full attribute vocabulary a plugin uses to annotate a `PluginConfig`-derived class so Magnetar can discover, validate, remotely manage and lay out each configuration option in an external Web UI (rendered by the manager app, e.g. |
| [`PluginSdk/Config/ConfigSchema.cs`](descriptions/PluginSdk/Config/ConfigSchema.cs.md) | 543 | 1 | Reflection-based schema extractor that turns a `PluginConfig`-derived type into a `ConfigSchemaData` document describing its layout tree, options, nested struct definitions and enum definitions. |
| [`PluginSdk/Config/ConfigStorage.cs`](descriptions/PluginSdk/Config/ConfigStorage.cs.md) | 158 | 2 | Save/load facade for `PluginConfig`-derived instances in two formats. **XML** is the local on-disk format: written atomically via a temp file + rename, emitting only non-default values (the sparse format is driven by `PluginConfig`'s `IXmlSerializable` implementation), so missing elements fall back to defaults on load. **JSON** is the remote management wire format — a three-part envelope of `schema` (from `ConfigSchema.Build`), `defaults` (a fresh instance) and `values` (the current config); loading reads only `values` while regenerating schema/defaults on every save. |
| [`PluginSdk/Config/PluginConfig.cs`](descriptions/PluginSdk/Config/PluginConfig.cs.md) | 298 | 1 | Abstract base class for managed plugin configuration. |
| [`PluginSdk/Config/TypeSerialization.cs`](descriptions/PluginSdk/Config/TypeSerialization.cs.md) | 413 | 1 | Bespoke XML read/write helpers and `System.Text.Json` converters for the small set of VRage value types that are first-class configuration values: `Color`, `Vector2D`, `Vector3D`, `Vector2I`, `Vector3I`, `Base6Directions.Direction` and `MyPositionAndOrientation`. |

## PluginSdk.Logging  ·  [module doc](modules/PluginSdk.Logging.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdk/Logging/ILogSink.cs`](descriptions/PluginSdk/Logging/ILogSink.cs.md) | 19 | 3 | Defines the single-method contract that every log destination must satisfy. |
| [`PluginSdk/Logging/LogEntry.cs`](descriptions/PluginSdk/Logging/LogEntry.cs.md) | 48 | 2 | A single immutable log record that is passed by `in` reference from `Logger` to `ILogSink`. |
| [`PluginSdk/Logging/LogEnvironment.cs`](descriptions/PluginSdk/Logging/LogEnvironment.cs.md) | 39 | 2 | Acts as the environment probe that decides which `ILogSink` the SDK uses. |
| [`PluginSdk/Logging/LogJson.cs`](descriptions/PluginSdk/Logging/LogJson.cs.md) | 51 | 2 | Centralises `System.Text.Json` configuration and serialization helpers so both `MagnetarLogSink` and `QuasarLogSink` produce identical JSON shapes for the optional structured `data` payload. |
| [`PluginSdk/Logging/LogLevel.cs`](descriptions/PluginSdk/Logging/LogLevel.cs.md) | 16 | 3 | Declares the severity levels used throughout the SDK logging subsystem. |
| [`PluginSdk/Logging/Logger.cs`](descriptions/PluginSdk/Logging/Logger.cs.md) | 84 | 2 | The primary logging facade a plugin holds as a `static readonly` field. |
| [`PluginSdk/Logging/MagnetarLogSink.cs`](descriptions/PluginSdk/Logging/MagnetarLogSink.cs.md) | 58 | 2 | The `ILogSink` used when the server runs under standalone Magnetar (no Quasar Agent). |
| [`PluginSdk/Logging/QuasarLogSink.cs`](descriptions/PluginSdk/Logging/QuasarLogSink.cs.md) | 76 | 2 | The `ILogSink` used when the server process is managed by the Quasar Agent. |

## PluginSdk.Runtime  ·  [module doc](modules/PluginSdk.Runtime.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdk/Paths/IPathResolver.cs`](descriptions/PluginSdk/Paths/IPathResolver.cs.md) | 48 | 2 | Defines the backend contract for cross-platform, case-insensitive path resolution. |
| [`PluginSdk/Paths/PathResolver.cs`](descriptions/PluginSdk/Paths/PathResolver.cs.md) | 48 | 2 | Plugin-facing static facade for cross-platform, case-insensitive path resolution. |
| [`PluginSdk/Paths/ShimPathResolver.cs`](descriptions/PluginSdk/Paths/ShimPathResolver.cs.md) | 36 | 2 | Default, no-op implementation of `IPathResolver` used when the server is running on a case-insensitive filesystem (Windows) or when no real case-insensitive backend has been installed yet. |
| [`PluginSdk/ServerControl.cs`](descriptions/PluginSdk/ServerControl.cs.md) | 142 | 2 | Exposes the dedicated server's lifecycle controls (save, reload config, quit, restart) as a stable plugin-facing API, decoupled from the host launcher implementation. |
| [`PluginSdk/Tools/SerializableDictionary.cs`](descriptions/PluginSdk/Tools/SerializableDictionary.cs.md) | 79 | 2 | Provides a generic dictionary that can be round-tripped by `XmlSerializer`, which cannot handle the standard `Dictionary<TKey, TValue>`. |

## PluginSdkTests  ·  [module doc](modules/PluginSdkTests.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`PluginSdkTests/ChangeNotificationTests.cs`](descriptions/PluginSdkTests/ChangeNotificationTests.cs.md) | 257 | 2 | Verifies the change-notification contract of `PluginConfig` — the base class for all Magnetar plugin configuration objects. |
| [`PluginSdkTests/CommandTests.cs`](descriptions/PluginSdkTests/CommandTests.cs.md) | 470 | 2 | Comprehensive specification for the PluginSdk chat-command pipeline: `CommandRegistry`, `CommandDispatcher`, `CommandModule`, `CommandCaller`, `CommandReply`, `ICommandResponder`, and the associated attributes (`CommandRoot`, `Command`, `Permission`). |
| [`PluginSdkTests/LoggingTests.cs`](descriptions/PluginSdkTests/LoggingTests.cs.md) | 198 | 2 | Specifies the PluginSdk logging subsystem: `Logger`, `LogEntry`, `ILogSink`, `LogLevel`, `QuasarLogSink`, `MagnetarLogSink`, and `LogEnvironment`. |
| [`PluginSdkTests/PathResolverTests.cs`](descriptions/PluginSdkTests/PathResolverTests.cs.md) | 88 | 2 | Specifies the `PathResolver` façade and its `IPathResolver` plug-in point. |
| [`PluginSdkTests/SchemaTests.cs`](descriptions/PluginSdkTests/SchemaTests.cs.md) | 525 | 2 | Specifies the schema and JSON-envelope subsystems of `PluginSdk.Config`. |
| [`PluginSdkTests/SerializationTests.cs`](descriptions/PluginSdkTests/SerializationTests.cs.md) | 464 | 2 | End-to-end round-trip and format-pinning tests for `PluginSdk.Config` serialisation. |
| [`PluginSdkTests/ServerControlTests.cs`](descriptions/PluginSdkTests/ServerControlTests.cs.md) | 62 | 2 | Specifies the `ServerControl` static façade that plugins call to trigger server lifecycle operations (save, reload config, quit, restart). |
| [`PluginSdkTests/TestConfig.cs`](descriptions/PluginSdkTests/TestConfig.cs.md) | 197 | 2 | Defines the shared fixture types used across all PluginSdkTests test classes. |

## Shared.Config  ·  [module doc](modules/Shared.Config.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/Config/ConfigManager.cs`](descriptions/Shared/Config/ConfigManager.cs.md) | 77 | 2 | `ConfigManager` is the singleton root of all runtime configuration for Magnetar. |
| [`Shared/Config/CoreConfig.cs`](descriptions/Shared/Config/CoreConfig.cs.md) | 78 | 2 | `CoreConfig` persists the fundamental installation-level settings to `config.xml` in the Pulsar/Magnetar data directory. |
| [`Shared/Config/GitHubPluginConfig.cs`](descriptions/Shared/Config/GitHubPluginConfig.cs.md) | 6 | 3 | `GitHubPluginConfig` is the per-plugin configuration record stored inside a `Profile` for plugins sourced from GitHub releases. |
| [`Shared/Config/LocalFolderConfig.cs`](descriptions/Shared/Config/LocalFolderConfig.cs.md) | 7 | 3 | `LocalFolderConfig` is the per-plugin configuration record stored inside a `Profile` for plugins sourced from a local development folder (the "DevFolder" feature). |
| [`Shared/Config/PluginDataConfig.cs`](descriptions/Shared/Config/PluginDataConfig.cs.md) | 10 | 3 | `PluginDataConfig` is the abstract base for per-plugin configuration records that are embedded in a `Profile`. |
| [`Shared/Config/ProfilesConfig.cs`](descriptions/Shared/Config/ProfilesConfig.cs.md) | 156 | 2 | `ProfilesConfig` manages the on-disk lifecycle of named plugin-enable profiles. |
| [`Shared/Config/Sources/LocalHubConfig.cs`](descriptions/Shared/Config/Sources/LocalHubConfig.cs.md) | 9 | 3 | `LocalHubConfig` is the configuration record for a locally-stored plugin hub — a directory on the filesystem that acts as a hub catalogue. |
| [`Shared/Config/Sources/LocalPluginConfig.cs`](descriptions/Shared/Config/Sources/LocalPluginConfig.cs.md) | 8 | 3 | `LocalPluginConfig` is the configuration record for a plugin that is installed directly from a local filesystem folder, without going through a hub or GitHub. |
| [`Shared/Config/Sources/ModConfig.cs`](descriptions/Shared/Config/Sources/ModConfig.cs.md) | 8 | 3 | `ModConfig` is the configuration record for a Steam Workshop mod source. |
| [`Shared/Config/Sources/RemoteHubConfig.cs`](descriptions/Shared/Config/Sources/RemoteHubConfig.cs.md) | 14 | 3 | `RemoteHubConfig` is the configuration record for a GitHub-hosted plugin hub. |
| [`Shared/Config/Sources/RemotePluginConfig.cs`](descriptions/Shared/Config/Sources/RemotePluginConfig.cs.md) | 14 | 3 | `RemotePluginConfig` is the configuration record for a GitHub-hosted plugin that is registered directly as a source (not via a hub). |
| [`Shared/Config/SourcesConfig.cs`](descriptions/Shared/Config/SourcesConfig.cs.md) | 134 | 2 | `SourcesConfig` is the XML-serialised registry of all plugin and mod sources available to Magnetar. |

## Shared.Core  ·  [module doc](modules/Shared.Core.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/AssemblyResolver.cs`](descriptions/Shared/AssemblyResolver.cs.md) | 107 | 2 | Provides a scoped `AppDomain.AssemblyResolve` handler that satisfies managed assembly load requests from one or more "source" folders, but only when the *requesting* assembly is on an allow-list. |
| [`Shared/Flags.cs`](descriptions/Shared/Flags.cs.md) | 79 | 2 | Parses Magnetar's own command-line switches once at startup (in a static constructor) and exposes them as read-only boolean/enum flags for the rest of the loader. |
| [`Shared/Launcher.cs`](descriptions/Shared/Launcher.cs.md) | 52 | 2 | Performs pre-launch sanity checks before Magnetar starts the SE Dedicated Server: refuses to start if the SE process is already running, rejects the removed `-plugin` switch, and verifies that an app `.config` exists when the SE folder ships one. |
| [`Shared/Loader.cs`](descriptions/Shared/Loader.cs.md) | 148 | 2 | The orchestrator that instantiates all enabled plugins at startup. |
| [`Shared/LogFile.cs`](descriptions/Shared/LogFile.cs.md) | 97 | 2 | Magnetar's central logging facade. |
| [`Shared/PluginList.cs`](descriptions/Shared/PluginList.cs.md) | 842 | 1 | The plugin catalog. |
| [`Shared/PluginProgress.cs`](descriptions/Shared/PluginProgress.cs.md) | 45 | 2 | Plain-text console progress reporter for plugin download and compilation, replacing the former WinForms splash screen that does not exist on the headless DS. |
| [`Shared/Preloader.cs`](descriptions/Shared/Preloader.cs.md) | 225 | 1 | Implements Magnetar's "preloader plugin" mechanism: BepInEx/Pulsar-style assembly patching of SE DS DLLs *on disk* before they are loaded into the CLR. |
| [`Shared/Steam.cs`](descriptions/Shared/Steam.cs.md) | 81 | 2 | Thin Steam helper for the Dedicated Server: resolves the Steam install path cross-platform, redirects `Steamworks.NET` assembly resolution to a bundled copy, and checks Workshop item install state through the *game-server* UGC API. |
| [`Shared/Tools.cs`](descriptions/Shared/Tools.cs.md) | 179 | 2 | Grab-bag of cross-cutting utilities used throughout Magnetar: SHA-256 hashing of files/strings/folders (used for cache invalidation), human-friendly "time ago" formatting, console/error message reporting, file globbing, filename sanitizing, JSON-based deep copy, and a cross-platform native crash handler. |
| [`Shared/Updater.cs`](descriptions/Shared/Updater.cs.md) | 209 | 1 | Handles Magnetar's self-update against a GitHub release repo. |

## Shared.Data  ·  [module doc](modules/Shared.Data.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/Data/GitHubPlugin.AssetFile.cs`](descriptions/Shared/Data/GitHubPlugin.AssetFile.cs.md) | 77 | 2 | Defines `GitHubPlugin.AssetFile`, the XML-serializable record describing one cached file that belongs to a compiled GitHub plugin: either a non-code asset extracted from the source archive, a NuGet library DLL, or NuGet content. |
| [`Shared/Data/GitHubPlugin.CacheManifest.cs`](descriptions/Shared/Data/GitHubPlugin.CacheManifest.cs.md) | 241 | 1 | Defines `GitHubPlugin.CacheManifest`, the persistent on-disk cache record for a compiled GitHub plugin. |
| [`Shared/Data/GitHubPlugin.cs`](descriptions/Shared/Data/GitHubPlugin.cs.md) | 381 | 1 | `GitHubPlugin` is the `PluginData` implementation that compiles a plugin from C# source pulled directly from a GitHub repository archive. |
| [`Shared/Data/LocalFolderPlugin.cs`](descriptions/Shared/Data/LocalFolderPlugin.cs.md) | 334 | 1 | `LocalFolderPlugin` is the developer-facing `PluginData` that compiles a plugin from a local source folder on every launch (no GitHub download, no cache). |
| [`Shared/Data/LocalPlugin.cs`](descriptions/Shared/Data/LocalPlugin.cs.md) | 109 | 2 | `LocalPlugin` is the `PluginData` for a pre-compiled plugin DLL sitting on disk (not compiled by Magnetar, not from GitHub). |
| [`Shared/Data/ModPlugin.cs`](descriptions/Shared/Data/ModPlugin.cs.md) | 81 | 2 | `ModPlugin` is the `PluginData` for a Steam Workshop mod referenced by its numeric workshop id. |
| [`Shared/Data/ObsoletePlugin.cs`](descriptions/Shared/Data/ObsoletePlugin.cs.md) | 15 | 3 | `ObsoletePlugin` is a placeholder `PluginData` registered as a ProtoBuf subtype so the plugin-list deserializer can tolerate plugins that have been removed or superseded. |
| [`Shared/Data/PluginData.cs`](descriptions/Shared/Data/PluginData.cs.md) | 354 | 1 | `PluginData` is the abstract base for every kind of plugin entry in Magnetar's plugin list: GitHub-compiled (`GitHubPlugin`), local source folder (`LocalFolderPlugin`), local DLL (`LocalPlugin`), Steam Workshop mod (`ModPlugin`), and the placeholder `ObsoletePlugin`. |
| [`Shared/Data/PluginStatus.cs`](descriptions/Shared/Data/PluginStatus.cs.md) | 12 | 3 | `PluginStatus` enumerates the load/health states a `PluginData` can be in, used to drive the status column in the plugin UI and to gate loading. |
| [`Shared/Data/Profile.cs`](descriptions/Shared/Data/Profile.cs.md) | 81 | 2 | `Profile` is a named set of enabled plugins — the persisted selection a user activates. |

## Shared.Network  ·  [module doc](modules/Shared.Network.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/Network/GitHub.cs`](descriptions/Shared/Network/GitHub.cs.md) | 140 | 2 | `GitHub` is a thin static HTTP façade over the GitHub REST API and raw-content CDN. |
| [`Shared/Network/NuGetClient.cs`](descriptions/Shared/Network/NuGetClient.cs.md) | 248 | 1 | `NuGetClient` wraps the NuGet v3 client SDK to download and extract packages from `api.nuget.org` into a local cache inside Magnetar's data directory. |
| [`Shared/Network/NuGetLogger.cs`](descriptions/Shared/Network/NuGetLogger.cs.md) | 87 | 2 | `NuGetLogger` adapts the NuGet SDK's `ILogger` interface to Magnetar's `LogFile` / NLog pipeline. |
| [`Shared/Network/NuGetPackage.cs`](descriptions/Shared/Network/NuGetPackage.cs.md) | 124 | 2 | `NuGetPackage` represents a single NuGet package that has already been extracted to disk. |
| [`Shared/Network/NuGetPackageId.cs`](descriptions/Shared/Network/NuGetPackageId.cs.md) | 47 | 2 | `NuGetPackageId` is a serialisable DTO that identifies a single NuGet package by name and version string. |
| [`Shared/Network/NuGetPackageList.cs`](descriptions/Shared/Network/NuGetPackageList.cs.md) | 20 | 3 | `NuGetPackageList` is a compact container that carries a plugin's NuGet dependency declaration in two optional forms: a path to a `packages.config` file (`Config`) and/or an inline array of `NuGetPackageId` records (`PackageIds`). |
| [`Shared/Network/SimpleHttpClient.cs`](descriptions/Shared/Network/SimpleHttpClient.cs.md) | 198 | 2 | `SimpleHttpClient` is a thin, synchronous REST façade built on `HttpWebRequest`. |

## Shared.Stats  ·  [module doc](modules/Shared.Stats.md)

| File | Lines | Tier | Description |
| ---- | ----- | ---- | ----------- |
| [`Shared/Stats/Model/ConsentRequest.cs`](descriptions/Shared/Stats/Model/ConsentRequest.cs.md) | 14 | 3 | Defines the JSON request body sent to the statistics server's `/Consent` endpoint when a user grants or withdraws data-handling consent. |
| [`Shared/Stats/Model/PluginStat.cs`](descriptions/Shared/Stats/Model/PluginStat.cs.md) | 24 | 3 | Represents the statistics record for a single plugin as returned by the `/Stats` REST endpoint. |
| [`Shared/Stats/Model/PluginStats.cs`](descriptions/Shared/Stats/Model/PluginStats.cs.md) | 21 | 3 | Top-level response container returned by the `/Stats` REST endpoint. |
| [`Shared/Stats/Model/TrackRequest.cs`](descriptions/Shared/Stats/Model/TrackRequest.cs.md) | 17 | 3 | Request body POSTed to `/Track` each time the game starts, recording which plugins were active for a given (anonymized) player. |
| [`Shared/Stats/Model/VoteRequest.cs`](descriptions/Shared/Stats/Model/VoteRequest.cs.md) | 20 | 3 | Request body POSTed to `/Vote` when a player changes their vote on a plugin. |
| [`Shared/Stats/StatsClient.cs`](descriptions/Shared/Stats/StatsClient.cs.md) | 94 | 2 | The single outbound client for Magnetar's statistics back-end, providing four REST operations: consent management, stats download, session tracking, and voting. |
