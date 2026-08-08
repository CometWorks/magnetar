# Module: PluginSdk.Runtime

**Project:** `PluginSdk` · **Files:** 10 · **Source lines:** 744

## Purpose

Provides plugins with stable, host-agnostic contracts for path resolution, server lifecycle,
typed cluster lifecycle request/ack, the authenticated cluster node link, and mission-screen
popups. Host-bound facilities preserve safe standalone defaults until a provider is installed.

## Role in Magnetar

Acts as the plugin-facing contract layer between plugins, Magnetar launchers, DirectTransport,
and ClusterRuntime. Single-owner rendezvous points prevent competing transport or lifecycle
providers, while typed acknowledgements keep Registry authority out of the launcher.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `IPathResolver` | interface | [`PluginSdk/Paths/IPathResolver.cs`](../descriptions/PluginSdk/Paths/IPathResolver.cs.md) | Backend contract for cross-platform case-insensitive path normalization and resolution. |
| `PathResolver` | static class | [`PluginSdk/Paths/PathResolver.cs`](../descriptions/PluginSdk/Paths/PathResolver.cs.md) | Plugin-facing static facade that delegates all path operations to the currently installed IPathResolver backend. |
| `ShimPathResolver` | class | [`PluginSdk/Paths/ShimPathResolver.cs`](../descriptions/PluginSdk/Paths/ShimPathResolver.cs.md) | Default no-op IPathResolver used on Windows or before a real backend is installed. |
| `ServerTerminationKind` | enum | [`PluginSdk/ServerControl.cs`](../descriptions/PluginSdk/ServerControl.cs.md) | Discriminates admin-initiated Shutdown vs Restart intent carried by ServerControl.Terminating. |
| `ServerControl` | static class | [`PluginSdk/ServerControl.cs`](../descriptions/PluginSdk/ServerControl.cs.md) | Plugin-facing facade for server lifecycle operations (save, reload, quit, restart) backed by host-bound delegates. |
| `ClusterLifecycleRequest` / `ClusterLifecycleAcknowledgement` | classes | [`PluginSdk/Clustering/ClusterLifecycle.cs`](../descriptions/PluginSdk/Clustering/ClusterLifecycle.cs.md) | Correlated local intent and authoritative Gateway/Registry result. |
| `IClusterLifecycleProvider` | interface | [`PluginSdk/Clustering/ClusterLifecycle.cs`](../descriptions/PluginSdk/Clustering/ClusterLifecycle.cs.md) | Async provider contract whose completion represents an acknowledgement, not transport enqueue. |
| `ClusterLifecycle` | static class | [`PluginSdk/Clustering/ClusterLifecycle.cs`](../descriptions/PluginSdk/Clustering/ClusterLifecycle.cs.md) | Single-owner registration and fail-closed lifecycle routing. |
| `IClusterNodeLink` | interface | [`PluginSdk/Clustering/IClusterNodeLink.cs`](../descriptions/PluginSdk/Clustering/IClusterNodeLink.cs.md) | Transport-neutral authenticated Gateway data-link contract, including dedicated lifecycle frames. |
| `ClusterNodeLink` | static class | [`PluginSdk/Clustering/ClusterNodeLink.cs`](../descriptions/PluginSdk/Clustering/ClusterNodeLink.cs.md) | Single-owner transport rendezvous. |
| `SerializableDictionary` | class | [`PluginSdk/Tools/SerializableDictionary.cs`](../descriptions/PluginSdk/Tools/SerializableDictionary.cs.md) | Generic Dictionary subclass implementing IXmlSerializable so XmlSerializer can round-trip dictionary-typed plugin config options. |
| `MissionScreens` | static class | [`PluginSdk/MissionScreens.cs`](../descriptions/PluginSdk/MissionScreens.cs.md) | Plugin-facing facade for showing SE mission-screen popups to a player, a Steam id, or all clients, backed by host-bound sender delegates. |
| `MissionScreenContent` | readonly struct | [`PluginSdk/MissionScreenContent.cs`](../descriptions/PluginSdk/MissionScreenContent.cs.md) | Immutable text payload (title, objective prefix/text, description, OK caption) rendered by the MagnetarMod client on SE's mission screen. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`PluginSdk/Clustering/ClusterLifecycle.cs`](../descriptions/PluginSdk/Clustering/ClusterLifecycle.cs.md) | 166 | Defines the typed, asynchronous authority boundary for plugin/chat lifecycle requests in a cluster node. |
| [`PluginSdk/Clustering/ClusterNodeLink.cs`](../descriptions/PluginSdk/Clustering/ClusterNodeLink.cs.md) | 31 | Provides the process-wide rendezvous for one `IClusterNodeLink` provider. |
| [`PluginSdk/Clustering/IClusterNodeLink.cs`](../descriptions/PluginSdk/Clustering/IClusterNodeLink.cs.md) | 23 | Defines the transport-independent Gateway data-link contract shared by the DirectTransport provider and ClusterRuntime consumer. |
| [`PluginSdk/MissionScreenContent.cs`](../descriptions/PluginSdk/MissionScreenContent.cs.md) | 35 | Immutable value type carrying the text payload that the Magnetar client mod renders through Space Engineers' mission-screen popup. |
| [`PluginSdk/MissionScreens.cs`](../descriptions/PluginSdk/MissionScreens.cs.md) | 95 | Plugin-facing facade for opening Space Engineers mission-screen popups on connected clients from server-side plugin code, decoupled from the host launcher implementation. |
| [`PluginSdk/Paths/IPathResolver.cs`](../descriptions/PluginSdk/Paths/IPathResolver.cs.md) | 48 | Defines the backend contract for cross-platform, case-insensitive path resolution. |
| [`PluginSdk/Paths/PathResolver.cs`](../descriptions/PluginSdk/Paths/PathResolver.cs.md) | 48 | Plugin-facing static facade for cross-platform, case-insensitive path resolution. |
| [`PluginSdk/Paths/ShimPathResolver.cs`](../descriptions/PluginSdk/Paths/ShimPathResolver.cs.md) | 36 | Default, no-op implementation of `IPathResolver` used when the server is running on a case-insensitive filesystem (Windows) or when no real case-insensitive backend has been installed yet. |
| [`PluginSdk/ServerControl.cs`](../descriptions/PluginSdk/ServerControl.cs.md) | 182 | Exposes the dedicated server's lifecycle controls (save, reload config, quit, restart) as a stable plugin-facing API, decoupled from the host launcher implementation. |
| [`PluginSdk/Tools/SerializableDictionary.cs`](../descriptions/PluginSdk/Tools/SerializableDictionary.cs.md) | 80 | Provides a generic dictionary that can be round-tripped by `XmlSerializer`, which cannot handle the standard `Dictionary<TKey, TValue>`. |

## Public API surface

- `PathResolver.Install(IPathResolver backend) — host installs the Linux case-insensitive backend once at startup`
- `PathResolver.Normalize / ToWindowsPath / GetFileName / GetFileNameWithoutExtension / ResolveContentFilePath / ResolveAbsolute — plugin-facing path utilities`
- `PathResolver.IsCaseInsensitiveResolverActive — lets plugins detect whether a real Linux resolver is active`
- `ServerControl.SaveWorld() / ReloadConfig() / SaveAndQuit() / SaveAndRestart() / QuitWithoutSaving() / RestartWithoutSaving() — server lifecycle actions for plugins`
- `ServerControl.Terminating (event Action<ServerTerminationKind>) — fires before teardown when an admin drives shutdown or restart from in-game`
- `ServerControl.Bind(...) — internal; host installs real delegate implementations at launcher startup`
- `ServerControl.RaiseTerminating(ServerTerminationKind) — internal; host fires the Terminating event with per-subscriber fault isolation`
- `ClusterLifecycle.Register / Unregister / TryRequest — exact-owner async lifecycle routing; false only means standalone/no provider`
- `IClusterNodeLink.SendLifecycleRequest / LifecycleAcknowledged — dedicated typed lifecycle transport boundary`
- `SerializableDictionary<TKey,TValue> — XML-serializable dictionary for use in PluginConfig-derived classes`
- `MissionScreens.ShowToPlayer / ShowToSteam / ShowToAll(...) — send a mission-screen popup to one player (identity or Steam id) or all clients; string-parameter and MissionScreenContent overloads; return false when unbound or content is empty`
- `MissionScreens.IsHostSenderAvailable — true once the host has installed a server-side sender (does not guarantee the client receiver is enabled)`
- `MissionScreens.ChannelId / ProtocolVersion / ShowMissionScreenPacket — network channel/protocol constants shared with the MagnetarMod client receiver`
- `MissionScreens.Bind(...) — internal; host installs the real sender delegates at launcher startup`

## Dependencies

**Uses modules:** _none_
**Used by modules:** [Legacy.Commands](Legacy.Commands.md), [Legacy.Integration](Legacy.Integration.md), [Legacy.Loader](Legacy.Loader.md), [PluginSdkTests](PluginSdkTests.md)
**External systems:** LinuxCompat plugin (provides the real IPathResolver implementation on Linux, not in this repo); MagnetarInterim (binds ServerControl, MissionScreens, and installs PathResolver backend); MagnetarLegacy (binds ServerControl, MissionScreens, and installs PathResolver backend); SE DS assemblies (VRage.Utils.MyLog used in ServerControl.RaiseTerminating)

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
