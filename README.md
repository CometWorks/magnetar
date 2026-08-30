# Magnetar

A plugin and mod loader for the **Space Engineers (SE1) Dedicated Server**,
built on [Pulsar](https://github.com/SpaceGT/Pulsar) — the game-client plugin
loader — which is vendored as a git submodule. Pulsar provides the plugin
model, configuration, network and compiler infrastructure; Magnetar adds the
server host: headless launch of the dedicated server, daemon mode, lifecycle
control, chat commands, and the `PluginSdk` server plugins compile against.

Magnetar ships two launchers that drop in for `SpaceEngineersDedicated.exe`:

| Launcher | Runtime | Platforms |
| -------- | ------- | --------- |
| `MagnetarLegacy` | .NET Framework 4.8 | Windows only |
| `MagnetarInterim` | .NET 10 (via [dotnet-compat](https://github.com/CometWorks/dotnet-compat)) | Windows + Linux |

On **Windows** both launchers are built; on **Linux** only `MagnetarInterim`
(.NET 10).

Compatibility plugins are loaded implicitly:
- [dotnet-compat](https://github.com/CometWorks/dotnet-compat) for .NET 10 compatibility
- [linux-compat](https://github.com/CometWorks/linux-compat) for Linux compatibility

Command-line flags are unified with Pulsar: the plugin-loader flags are
Pulsar's own, plus Magnetar's server-specific flags (`-daemon`, `-config`,
`-ds64`, consent control, …); client-only options do not apply. Configuration,
profile and source files use Pulsar's current formats.

You can register new plugins by making PRs to the [MagnetarHub](https://github.com/CometWorks/magnetar-hub).

## Control plane — Quasar

[**Quasar**](https://github.com/viktor-ferenczi/Quasar/releases) is a separate
control plane with a Web UI that can manage and control **multiple Magnetar
instances** from one place. Each Magnetar reports structured status and logs;
Quasar orchestrates them.

## Configuration tool — MagnetarConfig

**MagnetarConfig** is a cross-platform terminal UI (Terminal.Gui, Turbo Vision
look) that configures **and operates one** Magnetar-managed Dedicated Server
instance: edit the global `SpaceEngineers-Dedicated.cfg`, per-world session
settings and mod lists, create/delete/activate worlds, manage plugins and
profiles, start/stop/reload the daemonized server (PID-file status), and read
the game and Magnetar logs. It ships in both bundles next to the launcher and
runs as `Config/MagnetarConfig` inside the install folder. See the
**[Config tool user manual](Docs/ConfigTerminal.md)**.

## Building

Clone with the Pulsar submodule and build the solution:

```sh
git clone --recurse-submodules https://github.com/CometWorks/magnetar
cd magnetar
dotnet build -c Release Magnetar.slnx
```

Every build deploys a portable install tree (default: `%APPDATA%\Magnetar`
on Windows, `~/.local/share/Magnetar` on Linux). Magnetar itself is portable:
its configuration lives next to the binaries and the install folder can be
moved anywhere. On Linux the native runtime libraries are downloaded by the
linux-compat plugin on first launch.

See **[Building](Docs/Build.md)** for details.

## Documentation

| Page | What's in it |
| ---- | ------------ |
| [Install & Releases](Docs/Install.md) | Prebuilt bundles, what to download, installing. |
| [Usage](Docs/Usage.md) | Running the launcher, daemon mode, handoff to the DS. |
| [Configuration](Docs/Configuration.md) | Config/install dirs, DS detection, environment variables. |
| [Config tool](Docs/ConfigTerminal.md) | `MagnetarConfig` user manual: edit config/worlds/mods/plugins, start/stop, logs. |
| [Config tool internals](Docs/ConfigTerminalInternals.md) | Design and implementation of `MagnetarConfig`: file formats, architecture, state machines, testing. |
| [Plugins](Docs/Plugins.md) | Plugin hubs and the trust boundary. |
| [Building](Docs/Build.md) | Per-platform build, dependency staging, packaging, releases. |
| [Repository layout](Docs/Layout.md) | What lives where in the source tree. |

## Contact

[Discord](https://discord.gg/z8ZczP2YZY) for support and developer discussion.
GitHub issues and PRs for bug reports and contributions.
