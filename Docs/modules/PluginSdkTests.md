# Module: PluginSdkTests

**Project:** `PluginSdkTests` · **Files:** 10 · **Source lines:** 2423

## Purpose

Xunit test project that specifies and regression-guards every public API in PluginSdk: the PluginConfig change-notification contract, the chat-command pipeline (registry, dispatch, argument binding, permissions, help), the logging subsystem (Logger, LogEntry, QuasarLogSink, MagnetarLogSink, LogEnvironment), the PathResolver façade and its Linux-compat backend hook, the ConfigSchema builder and JSON-envelope format, the ConfigStorage XML/JSON round-trips and sparse-save behaviour, and the ServerControl lifecycle façade. No production code lives here; the project exists purely to define the library's behavioural contract.

## Role in Magnetar

Specification and regression suite for PluginSdk. Sits at the leaf of the build graph (depends on PluginSdk but nothing depends on it) and is the authoritative source of truth for what the SDK must produce. CI failures here block any PluginSdk change from merging.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `ChangeNotificationTests` | class | [`PluginSdkTests/ChangeNotificationTests.cs`](../descriptions/PluginSdkTests/ChangeNotificationTests.cs.md) | Specifies PluginConfig property-change notification: equality gating, in-place mutation limitations, and the NotifyChanged escape hatch. |
| `CommandTests` | class | [`PluginSdkTests/CommandTests.cs`](../descriptions/PluginSdkTests/CommandTests.cs.md) | Comprehensive specification for the chat-command pipeline: registration, argument binding, permissions, default commands, overview/help, error handling, and global !help. |
| `LoggingTests` | class | [`PluginSdkTests/LoggingTests.cs`](../descriptions/PluginSdkTests/LoggingTests.cs.md) | Specifies Logger stamping, LogEntry field capture, QuasarLogSink ISO 8601 JSON format, MagnetarLogSink text format, and LogEnvironment sink selection. |
| `PathResolverTests` | class | [`PluginSdkTests/PathResolverTests.cs`](../descriptions/PluginSdkTests/PathResolverTests.cs.md) | Specifies the PathResolver static façade: default pass-through shim behaviour and IPathResolver backend installation for Linux case-insensitive path resolution. |
| `SchemaTests` | class | [`PluginSdkTests/SchemaTests.cs`](../descriptions/PluginSdkTests/SchemaTests.cs.md) | Specifies ConfigSchema.Build: layout containers, property metadata, collection shapes, struct/enum discovery, tree-list metadata, and StructCaption validation. |
| `JsonEnvelopeTests` | class | [`PluginSdkTests/SchemaTests.cs`](../descriptions/PluginSdkTests/SchemaTests.cs.md) | Specifies the three-part {schema,defaults,values} JSON envelope produced by ConfigStorage.SaveJson and backward-compatible flat-JSON loading. |
| `SparseXmlTests` | class | [`PluginSdkTests/SchemaTests.cs`](../descriptions/PluginSdkTests/SchemaTests.cs.md) | Specifies that ConfigStorage.SaveXml emits only changed properties and that LoadXml restores constructor defaults for absent properties. |
| `SerializationTests` | class | [`PluginSdkTests/SerializationTests.cs`](../descriptions/PluginSdkTests/SerializationTests.cs.md) | End-to-end XML and JSON round-trip tests for all supported type combinations in TestConfig. |
| `TypeSerializationTests` | class | [`PluginSdkTests/SerializationTests.cs`](../descriptions/PluginSdkTests/SerializationTests.cs.md) | Pins the exact on-disk XML and on-wire JSON format of VRage value types: Color hex, space-separated vectors, Direction by name, MyPositionAndOrientation structure. |
| `ServerControlTests` | class | [`PluginSdkTests/ServerControlTests.cs`](../descriptions/PluginSdkTests/ServerControlTests.cs.md) | Specifies ServerControl.Bind routing and null-binding safe-no-op restoration. |
| `ClusterLifecycleTests` | class | [`PluginSdkTests/ClusterLifecycleTests.cs`](../descriptions/PluginSdkTests/ClusterLifecycleTests.cs.md) | Specifies single-owner typed routing, exact request shape, fail-closed provider errors, and standalone fallback. |
| `ClusterNodeLinkTests` | class | [`PluginSdkTests/ClusterNodeLinkTests.cs`](../descriptions/PluginSdkTests/ClusterNodeLinkTests.cs.md) | Specifies exact-owner node-link registration. |
| `TestConfig` | class | [`PluginSdkTests/TestConfig.cs`](../descriptions/PluginSdkTests/TestConfig.cs.md) | Canonical PluginConfig fixture covering every option type, layout container, VRage type, struct, enum, list, dict, and nested-struct combination. |
| `TestStruct` | struct | [`PluginSdkTests/TestConfig.cs`](../descriptions/PluginSdkTests/TestConfig.cs.md) | StructMember fixture with all scalar types plus a Quality enum field; used as element type in StructList and as NestedStruct.Inner. |
| `TreeNode` | struct | [`PluginSdkTests/TestConfig.cs`](../descriptions/PluginSdkTests/TestConfig.cs.md) | Parent-child StructMember fixture with StructCaption on Label; exercises tree-list schema metadata. |
| `NestedStruct` | struct | [`PluginSdkTests/TestConfig.cs`](../descriptions/PluginSdkTests/TestConfig.cs.md) | StructMember fixture with nested List<int>, SerializableDictionary, and TestStruct; exercises deep schema recursion and equality. |
| `Quality` | enum | [`PluginSdkTests/TestConfig.cs`](../descriptions/PluginSdkTests/TestConfig.cs.md) | Non-contiguous enum with EnumCaption overrides; proves storage-by-name and schema caption extraction. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`PluginSdkTests/ChangeNotificationTests.cs`](../descriptions/PluginSdkTests/ChangeNotificationTests.cs.md) | 257 | Verifies the change-notification contract of `PluginConfig` — the base class for all Magnetar plugin configuration objects. |
| [`PluginSdkTests/ClusterLifecycleTests.cs`](../descriptions/PluginSdkTests/ClusterLifecycleTests.cs.md) | 113 | Verifies that one registered lifecycle provider receives all four `ServerControl` termination paths exactly once with the correct kind/save preference, no local delegate runs, provider registration is exact-owner, provider failure returns `Unavailable`, and no provider preserves standalone behavior. |
| [`PluginSdkTests/ClusterNodeLinkTests.cs`](../descriptions/PluginSdkTests/ClusterNodeLinkTests.cs.md) | 43 | Specifies the single-provider lifecycle of `ClusterNodeLink`: the first provider wins, another provider cannot replace or unregister it, and the exact provider can unregister cleanly. |
| [`PluginSdkTests/CommandTests.cs`](../descriptions/PluginSdkTests/CommandTests.cs.md) | 470 | Comprehensive specification for the PluginSdk chat-command pipeline: `CommandRegistry`, `CommandDispatcher`, `CommandModule`, `CommandCaller`, `CommandReply`, `ICommandResponder`, and the associated attributes (`CommandRoot`, `Command`, `Permission`). |
| [`PluginSdkTests/LoggingTests.cs`](../descriptions/PluginSdkTests/LoggingTests.cs.md) | 198 | Specifies the PluginSdk logging subsystem: `Logger`, `LogEntry`, `ILogSink`, `LogLevel`, `QuasarLogSink`, `MagnetarLogSink`, and `LogEnvironment`. |
| [`PluginSdkTests/PathResolverTests.cs`](../descriptions/PluginSdkTests/PathResolverTests.cs.md) | 88 | Specifies the `PathResolver` façade and its `IPathResolver` plug-in point. |
| [`PluginSdkTests/SchemaTests.cs`](../descriptions/PluginSdkTests/SchemaTests.cs.md) | 525 | Specifies the schema and JSON-envelope subsystems of `PluginSdk.Config`. |
| [`PluginSdkTests/SerializationTests.cs`](../descriptions/PluginSdkTests/SerializationTests.cs.md) | 464 | End-to-end round-trip and format-pinning tests for `PluginSdk.Config` serialisation. |
| [`PluginSdkTests/ServerControlTests.cs`](../descriptions/PluginSdkTests/ServerControlTests.cs.md) | 68 | Specifies the `ServerControl` static façade that plugins call to trigger server lifecycle operations (save, reload config, quit, restart). |
| [`PluginSdkTests/TestConfig.cs`](../descriptions/PluginSdkTests/TestConfig.cs.md) | 197 | Defines the shared fixture types used across all PluginSdkTests test classes. |

## Public API surface

- `ChangeNotificationTests (xunit test class)`
- `CommandTests (xunit test class)`
- `LoggingTests (xunit test class)`
- `PathResolverTests (xunit test class)`
- `SchemaTests / JsonEnvelopeTests / SparseXmlTests (xunit test classes)`
- `SerializationTests / TypeSerializationTests (xunit test classes)`
- `ServerControlTests (xunit test class)`
- `ClusterLifecycleTests / ClusterNodeLinkTests (xunit test classes)`
- `TestConfig (shared fixture PluginConfig subclass)`
- `TestStruct / TreeNode / NestedStruct / Quality (shared fixture types)`

## Dependencies

**Uses modules:** [PluginSdk.Commands](PluginSdk.Commands.md), [PluginSdk.Config](PluginSdk.Config.md), [PluginSdk.Logging](PluginSdk.Logging.md), [PluginSdk.Runtime](PluginSdk.Runtime.md)
**Used by modules:** _none_
**External systems:** SE DS assemblies (VRage, VRageMath, VRage.Game for MyPromoteLevel, Color, Vector types, Base6Directions, MyPositionAndOrientation)

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
