# Magnetar

A plugin and mod loader for the Space Engineers Dedicated Server (SE1).
Hard fork of [Pulsar](https://github.com/SpaceGT/Pulsar), adapted to run
the headless dedicated server on both Windows and Linux — no WinForms,
no Telerik UI, no Windows-service host.

## Executables

`MagnetarLegacy` runs the [Space Engineers 1](https://steampowered.com/app/244850) Dedicated Server on [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework) (Windows only)<br>
`MagnetarInterim` runs the [Space Engineers 1](https://steampowered.com/app/244850) Dedicated Server on [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
(via [se-dotnet-compat](https://github.com/viktor-ferenczi/se-dotnet-compat); Windows and Linux)<br>

On **Windows** both launchers are built. On **Linux** only `MagnetarInterim`
is built — `.NET Framework 4.8` is Windows-only, and the Linux dedicated
server runs on .NET 10 via [se-dotnet-compat](https://github.com/viktor-ferenczi/se-dotnet-compat)
plus [se-linux-compat](https://github.com/viktor-ferenczi/se-linux-compat).

## Usage

Run the `MagnetarLegacy` or `MagnetarInterim` executable from your Dedicated
Server installation in place of `SpaceEngineersDedicated.exe`.<br>
Magnetar loads the server with all enabled plugins, then hands off to the
normal startup.<br>

## Building

Both platforms build from `Magnetar.sln` with the .NET 10 SDK:

```sh
dotnet build -c Release Magnetar.sln
```

On **Windows** this produces `MagnetarLegacy` (.NET Framework 4.8) and
`MagnetarInterim` (.NET 10). On **Linux** it produces `MagnetarInterim`
only, after a one-time `./build.sh` dependency-staging step.

See **[Docs/Build.md](Docs/Build.md)** for full per-platform instructions —
prerequisites, dedicated-server detection, dependency staging, publishing,
and the build-time property/environment overrides.

## Documentation

* **[Docs/TOC.md](Docs/TOC.md)** — the **code handbook**: an architecture
  overview, launch sequence, and a navigable module-by-module / file-by-file
  reference for the entire source tree (start here to understand the internals).
* **[Docs/Index.md](Docs/Index.md)** — flat index of every documented source file.
* **[Docs/Build.md](Docs/Build.md)** — building and packaging.
* **[skills/se-dev-plugin-sdk](skills/se-dev-plugin-sdk/SKILL.md)** — the
  plugin-author handbook for `PluginSdk` (config, commands, logging, paths).

## Layout

| Path                         | Purpose                                                           |
| ---------------------------- | ----------------------------------------------------------------- |
| `Legacy/`                    | Launcher (`MagnetarLegacy` / `MagnetarInterim`) — entry point, preloader, patches |
| `Shared/`                    | Cross-project plugin loader / config / network code               |
| `Compiler/`                  | Roslyn-based on-disk source plugin compiler                       |
| `PluginSdk/`                 | Public API surface plugins compile against                        |
| `Scripts/`                   | Build helpers (Steamworks.NET, licenses)                          |
| `build/Libraries/`           | Staged Linux dependencies (gitignored, populated by `./build.sh`) |
| `dist/`                      | Packaged distributables (gitignored)                              |

## Configuration

* **Config dir**
  * Windows — `%APPDATA%\Magnetar` (next to the install).
  * Linux — `$XDG_CONFIG_HOME/Magnetar`, falling back to `~/.config/Magnetar`.
* **Install dir (default)**
  * Windows — `%APPDATA%\Magnetar`.
  * Linux — `$XDG_DATA_HOME/Magnetar`, falling back to `~/.local/share/Magnetar`.
* **DS location** — auto-detected from the Steam registry (Windows) or
  `~/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64`
  (Linux), or any Steam library listed in `libraryfolders.vdf`. Override
  with `-ds64 /path/to/DedicatedServer64`.

## Environment variables

| Variable             | Effect                                                           |
| -------------------- | ---------------------------------------------------------------- |
| `MAGNETAR_SAFE_MODE` | When `1`, disables preloader patches for a one-off recovery run. |
| `XDG_CONFIG_HOME`    | Overrides the config-dir base (Linux).                           |
| `XDG_DATA_HOME`      | Overrides the install-dir base (Linux).                          |
| `DS64`               | Build-time override for the DS reference path.                   |

## Running

Run the launcher in place of `SpaceEngineersDedicated.exe`:

```sh
# Windows
%APPDATA%\Magnetar\MagnetarLegacy.exe
%APPDATA%\Magnetar\MagnetarInterim.exe

# Linux
~/.local/share/Magnetar/MagnetarInterim
```

Magnetar resolves the DS install, applies any preloader patches, loads
enabled plugins, then hands off to the dedicated server's own `Main`.

Pass `-daemon` to detach the process from its parent (typically Quasar) at
startup, so the parent terminating does not take the server down with it. On
Linux this is a `setsid()` — the process leaves the parent's session and
process group (an explicit `kill -HUP <pid>` still reloads the config). When
launched as a child it detaches in place, keeping the PID and inherited
stdout/stderr so a managing parent keeps capturing the log stream until it
exits; when the process is itself a process-group leader (e.g. a wrapper script
that `exec`s it), `setsid()` is not permitted in place, so it re-execs a
detached child and the original exits. On Windows it detaches from the inherited
console.

## Plugins

Plugins are registered on
[PluginHub-DS](https://github.com/viktor-ferenczi/PluginHub-DS/). Adding
other hubs is possible but extends the trust boundary — plugins run
unsandboxed native code.

## Contact

[Discord](https://discord.gg/z8ZczP2YZY) for support and developer
discussion. GitHub issues and PRs for bug reports and contributions.
