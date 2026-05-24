# Magnetar

A Linux plugin loader for the Space Engineers Dedicated Server (SE1).
Hard fork of [Pulsar](https://github.com/SpaceGT/Pulsar), adapted to load
the headless dedicated server on .NET 10 — no WinForms, no Telerik UI,
no Windows-service host.

## Status

Linux-only. The Windows-loader and Modern (Avalonia) UI sources from the
upstream Pulsar tree have been removed. The remaining launcher (`Interim`)
runs the dedicated server on .NET 10 via
[se-dotnet-compat](https://github.com/viktor-ferenczi/se-dotnet-compat).

## Building

Requirements:

* .NET 10 SDK
* `git`, `dotnet`, `bash`
* Space Engineers Dedicated Server installed via Steam (or any local
  copy of `DedicatedServer64/`)

One-time dependency staging — populates `build/Libraries/` with the
managed `Steamworks.NET.dll` (built from
[rlabrecque/Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET))
and copies `libsteam_api.so` out of your DS install:

```sh
./build.sh
```

Override the DS location if it is not in the default Steam path:

```sh
DS64=/opt/se1-ds/DedicatedServer64 ./build.sh
```

Then build the launcher:

```sh
dotnet build -c Release Magnetar.sln
```

Or produce a publish bundle ready to drop next to the dedicated server:

```sh
dotnet publish -c Release Legacy/Legacy.csproj \
    -r linux-x64 --self-contained false \
    -o ~/.local/share/Magnetar
```

The `Legacy.csproj` `AfterBuild` / `AfterPublish` targets copy
`build/Libraries/*` next to the produced `Interim` apphost; if that
folder is missing the build fails fast with a clear message.

## Layout

| Path                         | Purpose                                                           |
| ---------------------------- | ----------------------------------------------------------------- |
| `Legacy/`                    | Launcher (`Interim` apphost) — entry point, preloader, patches    |
| `Shared/`                    | Cross-project plugin loader / config / network code (netstandard2.0) |
| `Updater/`                   | Out-of-process updater binary                                     |
| `Compiler/`                  | Roslyn-based on-disk source plugin compiler                       |
| `Scripts/`                   | Build helpers (Steamworks.NET, licenses)                          |
| `build/Libraries/`           | Staged dependencies (gitignored, populated by `./build.sh`)       |
| `dist/`                      | Packaged distributables (gitignored)                              |

## Configuration

* **Config dir** — `$XDG_CONFIG_HOME/Magnetar`, falling back to
  `~/.config/Magnetar`.
* **Install dir (default)** — `$XDG_DATA_HOME/Magnetar`, falling back to
  `~/.local/share/Magnetar`.
* **DS location** — auto-detected from
  `~/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64`
  (or any Steam library listed in `libraryfolders.vdf`). Override with
  `-ds64 /path/to/DedicatedServer64`.

## Environment variables

| Variable             | Effect                                                           |
| -------------------- | ---------------------------------------------------------------- |
| `MAGNETAR_SAFE_MODE` | When `1`, disables preloader patches for a one-off recovery run. |
| `XDG_CONFIG_HOME`    | Overrides the config-dir base.                                   |
| `XDG_DATA_HOME`      | Overrides the install-dir base (used by `Updater`).              |
| `DS64`               | Build-time override for the DS reference path.                   |

## Running

Run the launcher in place of `SpaceEngineersDedicated.exe`:

```sh
~/.local/share/Magnetar/Interim
```

Magnetar resolves the DS install, applies any preloader patches, loads
enabled plugins, then hands off to the dedicated server's own `Main`.

## Plugins

Plugins are registered on
[PluginHub-DS](https://github.com/viktor-ferenczi/PluginHub-DS/). Adding
other hubs is possible but extends the trust boundary — plugins run
unsandboxed native code.

## Contact

[Discord](https://discord.gg/z8ZczP2YZY) for support and developer
discussion. GitHub issues and PRs for bug reports and contributions.
