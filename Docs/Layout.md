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
is not server-specific comes from the `Pulsar/` submodule. Namespaces mark the
origin of code:

- `Magnetar.*` — Magnetar-original code.
- `Pulsar.Shared`, `Pulsar.Protocol` — the submodule's assemblies, referenced
  as projects.
- `Pulsar.Legacy.*` — source-level reuse of the submodule's `Legacy` project
  (which targets the game client and cannot be referenced as an assembly).
  Files that need no server-specific changes are compiled straight from the
  submodule via `<Compile Link>` entries in `Legacy/Legacy.csproj`; files that
  do need changes are forks under `Legacy/` that keep the upstream namespace,
  so the linked files resolve them and diffs against upstream show only real
  divergence. After a submodule bump, diff the forks against their upstream
  counterparts to pick up fixes.
