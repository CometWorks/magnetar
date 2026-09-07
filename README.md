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

## Installation

Install the Space Engineers Dedicated Server game files separately, then either
use **[MagnetarConfig setup](#install-or-update-the-server)** or extract the
platform package from **[Magnetar releases](https://github.com/CometWorks/magnetar/releases)**
into a dedicated server installation folder. Linux uses `MagnetarInterim.bin`;
Windows provides `MagnetarInterim.exe` and `MagnetarLegacy.exe`.

The server keeps its own prerequisites: Interim requires the **.NET 10 runtime**;
on Windows, **.NET Framework 4.8** is also required by the plugin compiler and
Legacy launcher. Linux downloads native compatibility libraries on first launch,
so outbound HTTPS to GitHub is needed. The configuration tool's bundled runtime
does not install these prerequisites for the server.

See **[Install & Releases](Docs/Install.md)** for packages and manual setup, then
**[Usage](Docs/Usage.md)** for launcher arguments, daemon mode and consent.

## Control plane — Quasar

[**Quasar**](https://github.com/viktor-ferenczi/Quasar/releases) is a separate
control plane with a Web UI that can manage and control **multiple Magnetar
instances** from one place. Each Magnetar reports structured status and logs;
Quasar orchestrates them.

## Configuration tool — MagnetarConfig

**MagnetarConfig** is an optional terminal UI for configuring and operating
**one** Magnetar-managed Dedicated Server instance. It edits server/world
settings, mods, plugins, sources and profiles; manages worlds; starts the server;
and displays status and logs. Graceful stop/reload is supported on Linux;
Windows offers force-stop with a data-loss warning. Use Quasar for multiple
instances or remote operation.

Its **source, tests, manuals and releases live in
[CometWorks/config-tools](https://github.com/CometWorks/config-tools)**.
Magnetar server bundles no longer include it, and building Magnetar does not
build or download the configuration tool.

### Download and run

Choose the current **`magnetarconfig-v*`** release from
**[config-tools releases](https://github.com/CometWorks/config-tools/releases)**:

| Platform | Download |
| --- | --- |
| Linux x64 | `MagnetarConfig-linux-x64.bin` |
| Windows x64 | `MagnetarConfig-win-x64.exe` |

Each is a **single self-contained executable** with .NET and Terminal.Gui
included. No separate .NET SDK/runtime, Python, NuGet or external 7-Zip is
needed to run the tool. Linux uses standard `bash`/`stty` utilities. SHA-256
checksums and license notices accompany the downloads. Select the MagnetarConfig
release explicitly: this repository also releases PulsarConfig, so its global
`releases/latest/download` URL is unsuitable for choosing a specific tool.

Keep the tool in a folder of its own, outside the managed server installation;
this is required for setup changes on Windows. Existing instances can be opened
with explicit paths, or selected in the startup instance picker:

```sh
# Linux: use the same configuration and data folders as your server.
chmod +x MagnetarConfig-linux-x64.bin
./MagnetarConfig-linux-x64.bin -magnetar "/path/to/Magnetar/MagnetarInterim.bin" \
  -config "/path/to/Magnetar/Magnetar" -path "/path/to/DS-data"
```

```powershell
# Windows PowerShell: Legacy.exe can be selected instead of Interim.exe.
.\MagnetarConfig-win-x64.exe -magnetar "C:\Servers\Magnetar\MagnetarInterim.exe" -config "C:\Servers\Magnetar\Magnetar" -path "C:\Servers\DS-data"
```

`-config` is Magnetar's configuration/log/PID directory; `-path` is the Dedicated
Server data directory containing `SpaceEngineers-Dedicated.cfg` and `Saves`.
They must match the instance being managed. Pass `-ds64` when the tool cannot
find `DedicatedServer64` (also needed for world templates). `-diag` prints a
read-only instance report; `--help` lists the arguments. Placing the tool beside
a launcher enables adjacent-file discovery, but use an external copy for
Windows install/update/uninstall operations.

### Install or update the server

Open **File → Install / update / uninstall**, or start setup before an instance
exists:

```sh
./MagnetarConfig-linux-x64.bin --setup --target "$HOME/Games/Magnetar"
```

```powershell
.\MagnetarConfig-win-x64.exe --setup --target "C:\Servers\Magnetar"
```

Set the target and DedicatedServer64 location, review prerequisites, then choose
**Install**, **Update** or **Uninstall**. Setup downloads server packages from
**CometWorks/magnetar**, not config-tools, and does not install the DS game files
or system runtimes. Prerequisite reports are advisory; target validation,
checksums and running-server checks are mandatory. Stop servers using the target
before an update or uninstall.

For scripts, explicit actions require confirmation through `--yes`:

```sh
./MagnetarConfig-linux-x64.bin update --target "$HOME/Games/Magnetar" --yes
./MagnetarConfig-linux-x64.bin uninstall --target "$HOME/Games/Magnetar" --yes
```

Setup stages updates and retains a backup for rollback. Uninstall removes
server-owned launcher/library files; instances, worlds, profiles, local plugins,
unrelated files and the standalone tool are retained. See the
[setup guide](https://github.com/CometWorks/config-tools/blob/main/Docs/MagnetarConfig.md#installing-and-updating-magnetar)
for pinned releases and offline archives.

### Moving from the bundled tool

Download the standalone executable and update shortcuts/scripts that previously
ran `MagnetarConfig.bat`, a `MagnetarConfig` shell launcher, or the bundled
`Config/` tool. Open the **same `-config` and `-path` directories** and select
your server launcher with `-magnetar`; no world or profile conversion is needed.
The old bundled files are not required by the new tool. Keep existing data and
backups until you have verified the selected instance.

The older Linux `Bin`/shell-wrapper **server** layout is not automatically
converted by setup. Install the current server into a new folder and explicitly
select the existing config/data pair; do not run an old uninstall script over
those folders.

### Tool updates and appearance

Each interactive launch checks in the background for a newer **MagnetarConfig**
release. The prompt offers **Later** or **Update…**; choose **Update… → Update
and close**, then reopen the executable. Downloads require that explicit choice;
offline checks do not block startup. **Tools → Tool updates** checks on demand.
`--check-update`, `--self-update` and `--tool-version` work without selecting an
instance. Close other copies before updating; the previous executable is retained
as `.previous`. See [tool update and recovery details](https://github.com/CometWorks/config-tools#updating-the-tools).

These actions update **only MagnetarConfig**. **Setup → Update** or the CLI
`update --target ... --yes` updates the **Magnetar server**. Both products have
independent versions and release schedules.

**Tools → Theme** offers **Sandstone** (default), Graphite, Sage, Plum and the
original Turbo C / Turbo Vision appearance. The preference is stored for your
user account on this machine, shared with PulsarConfig and separate from server
instances. See [theme settings](https://github.com/CometWorks/config-tools#appearance).
For all screens, editing behavior and server controls, read the
**[MagnetarConfig manual](https://github.com/CometWorks/config-tools/blob/main/Docs/MagnetarConfig.md)**.

## Versioning

Magnetar's version is the vendored Pulsar version plus a build component:
`2.3.3.0` is the first Magnetar build on Pulsar 2.3.3, and `2.3.3.1` would be a
Magnetar-only release on the same Pulsar base. Release tags and bundle names use
all four components; `-version` prints the first three.

## Building

Clone with the Pulsar submodule and build the solution:

```sh
git clone --recurse-submodules https://github.com/CometWorks/magnetar
cd magnetar
dotnet build -c Release Magnetar.slnx
```

Every build deploys a portable install tree (default: `%APPDATA%\Magnetar`
on Windows, `~/.config/Magnetar` on Linux). Magnetar itself is portable:
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
| [Config tool](Docs/MagnetarConfig.md) | `MagnetarConfig` user manual: edit config/worlds/mods/plugins, start/stop, logs. |
| [Config tool internals](Docs/MagnetarConfigInternals.md) | Design and implementation of `MagnetarConfig`: file formats, architecture, state machines, testing. |
| [Plugins](Docs/Plugins.md) | Plugin hubs and the trust boundary. |
| [Building](Docs/Build.md) | Per-platform build, dependency staging, packaging, releases. |
| [Repository layout](Docs/Layout.md) | What lives where in the source tree. |

## Contact

[Discord](https://discord.gg/z8ZczP2YZY) for support and developer discussion.
GitHub issues and PRs for bug reports and contributions.
