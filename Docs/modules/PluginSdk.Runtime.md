# Module: PluginSdk.Runtime

**Project:** `PluginSdk` · **Files:** 7 · **Source lines:** 483

## Purpose

Provides plugins with a stable, host-agnostic API surface for two cross-cutting runtime concerns: (1) cross-platform case-insensitive path resolution that works identically on Windows and Linux by swapping a backend at startup, and (2) dedicated-server lifecycle control (save, reload config, quit, restart) and a pre-teardown notification event — all backed by host-bound delegates that default to safe no-ops until the launcher installs real implementations.

## Role in Magnetar

Acts as the plugin-facing contract layer between plugin code and the underlying host launchers (MagnetarInterim on Linux/.NET 10, MagnetarLegacy on Windows/.NET 4.8). Plugins call PathResolver and ServerControl unconditionally; the host injects real backends at startup. This decoupling means the same plugin binary runs on both platforms without conditional compilation.

## Key types

| Type | Kind | Defined in | Summary |
| ---- | ---- | ---------- | ------- |
| `IPathResolver` | interface | [`PluginSdk/Paths/IPathResolver.cs`](../descriptions/PluginSdk/Paths/IPathResolver.cs.md) | Backend contract for cross-platform case-insensitive path normalization and resolution. |
| `PathResolver` | static class | [`PluginSdk/Paths/PathResolver.cs`](../descriptions/PluginSdk/Paths/PathResolver.cs.md) | Plugin-facing static facade that delegates all path operations to the currently installed IPathResolver backend. |
| `ShimPathResolver` | class | [`PluginSdk/Paths/ShimPathResolver.cs`](../descriptions/PluginSdk/Paths/ShimPathResolver.cs.md) | Default no-op IPathResolver used on Windows or before a real backend is installed. |
| `ServerTerminationKind` | enum | [`PluginSdk/ServerControl.cs`](../descriptions/PluginSdk/ServerControl.cs.md) | Discriminates admin-initiated Shutdown vs Restart intent carried by ServerControl.Terminating. |
| `ServerControl` | static class | [`PluginSdk/ServerControl.cs`](../descriptions/PluginSdk/ServerControl.cs.md) | Plugin-facing facade for server lifecycle operations (save, reload, quit, restart) backed by host-bound delegates. |
| `SerializableDictionary` | class | [`PluginSdk/Tools/SerializableDictionary.cs`](../descriptions/PluginSdk/Tools/SerializableDictionary.cs.md) | Generic Dictionary subclass implementing IXmlSerializable so XmlSerializer can round-trip dictionary-typed plugin config options. |

## Files

| File | Lines | Summary |
| ---- | ----- | ------- |
| [`PluginSdk/MissionScreenContent.cs`](../descriptions/PluginSdk/MissionScreenContent.cs.md) | 35 | `MissionScreenContent` is the immutable payload a plugin passes to `MissionScreens` when it wants Magnetar to show the Space Engineers mission-screen popup on clients. |
| [`PluginSdk/MissionScreens.cs`](../descriptions/PluginSdk/MissionScreens.cs.md) | 95 | `MissionScreens` is the plugin-facing API for showing Space Engineers mission-screen popups on clients. |
| [`PluginSdk/Paths/IPathResolver.cs`](../descriptions/PluginSdk/Paths/IPathResolver.cs.md) | 48 | Defines the backend contract for cross-platform, case-insensitive path resolution. |
| [`PluginSdk/Paths/PathResolver.cs`](../descriptions/PluginSdk/Paths/PathResolver.cs.md) | 48 | Plugin-facing static facade for cross-platform, case-insensitive path resolution. |
| [`PluginSdk/Paths/ShimPathResolver.cs`](../descriptions/PluginSdk/Paths/ShimPathResolver.cs.md) | 36 | Default, no-op implementation of `IPathResolver` used when the server is running on a case-insensitive filesystem (Windows) or when no real case-insensitive backend has been installed yet. |
| [`PluginSdk/ServerControl.cs`](../descriptions/PluginSdk/ServerControl.cs.md) | 142 | Exposes the dedicated server's lifecycle controls (save, reload config, quit, restart) as a stable plugin-facing API, decoupled from the host launcher implementation. |
| [`PluginSdk/Tools/SerializableDictionary.cs`](../descriptions/PluginSdk/Tools/SerializableDictionary.cs.md) | 79 | Provides a generic dictionary that can be round-tripped by `XmlSerializer`, which cannot handle the standard `Dictionary<TKey, TValue>`. |

## Public API surface

- `PathResolver.Install(IPathResolver backend) — host installs the Linux case-insensitive backend once at startup`
- `PathResolver.Normalize / ToWindowsPath / GetFileName / GetFileNameWithoutExtension / ResolveContentFilePath / ResolveAbsolute — plugin-facing path utilities`
- `PathResolver.IsCaseInsensitiveResolverActive — lets plugins detect whether a real Linux resolver is active`
- `ServerControl.SaveWorld() / ReloadConfig() / SaveAndQuit() / SaveAndRestart() / QuitWithoutSaving() / RestartWithoutSaving() — server lifecycle actions for plugins`
- `ServerControl.Terminating (event Action<ServerTerminationKind>) — fires before teardown when an admin drives shutdown or restart from in-game`
- `ServerControl.Bind(...) — internal; host installs real delegate implementations at launcher startup`
- `ServerControl.RaiseTerminating(ServerTerminationKind) — internal; host fires the Terminating event with per-subscriber fault isolation`
- `SerializableDictionary<TKey,TValue> — XML-serializable dictionary for use in PluginConfig-derived classes`

## Dependencies

**Uses modules:** _none_
**Used by modules:** [Legacy.Integration](Legacy.Integration.md), [Legacy.Loader](Legacy.Loader.md), [PluginSdkTests](PluginSdkTests.md)
**External systems:** LinuxCompat plugin (provides the real IPathResolver implementation on Linux, not in this repo); MagnetarInterim (binds ServerControl and installs PathResolver backend); MagnetarLegacy (binds ServerControl and installs PathResolver backend); SE DS assemblies (VRage.Utils.MyLog used in ServerControl.RaiseTerminating)

---
[◀ Back to TOC](../TOC.md) · [Full file index](../Index.md)
