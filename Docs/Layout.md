# Repository layout

| Path                         | Purpose                                                           |
| ---------------------------- | ----------------------------------------------------------------- |
| `Pulsar/`                    | **Git submodule**: upstream [Pulsar](https://github.com/SpaceGT/Pulsar). Provides `Pulsar.Shared` (plugin model, config, network, loader), `Pulsar.Protocol`, and the out-of-process Roslyn `Compiler`. Never edited here; update by moving the submodule pin. |
| `Legacy/`                    | The server launcher (`MagnetarLegacy` / `MagnetarInterim`) — entry point, DS detection, daemon/pid/lifecycle, headless Harmony patches, chat commands, mission screens, Linux native bootstrap. References the submodule's `Shared` and `Compiler`. |
| `PluginSdk/`                 | Public API surface server plugins compile against                 |
| `PluginSdkTests/`            | xUnit specifications for every public `PluginSdk` API             |
| `ConfigTerminal/`            | `MagnetarConfig` — Terminal.Gui TUI to configure and operate one DS instance ([manual](ConfigTerminal.md) · [internals](ConfigTerminalInternals.md)) |
| `ConfigTerminalTests/`       | xUnit tests for `ConfigTerminal` (registry, documents, process/pid, plugins, workshop resolver) |
| `MagnetarMod/`               | Companion SE world mod project; Workshop/SE content lives under `MagnetarMod/src/` |
| `Directory.Build.props`      | Build settings (deploy folder, DS path); override locally with a git-ignored `Directory.Build.props.user` |

There is no forked `Shared/` or `Compiler/` project any more: everything that
is not server-specific comes from the `Pulsar/` submodule, and the Magnetar
projects use the `Magnetar.*` namespaces while `Pulsar.*` always refers to the
submodule's assemblies.
