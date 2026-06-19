# Magnetar

A plugin and mod loader for the **Space Engineers (SE1) Dedicated Server**. Hard
fork of [Pulsar](https://github.com/SpaceGT/Pulsar), adapted to run the headless
dedicated server — no WinForms, no Telerik UI, no Windows-service host.

Magnetar ships two launchers that drop in for `SpaceEngineersDedicated.exe`:

| Launcher | Runtime | Platforms |
| -------- | ------- | --------- |
| `MagnetarLegacy` | .NET Framework 4.8 | Windows only |
| `MagnetarInterim` | .NET 10 (via [se-dotnet-compat](https://github.com/viktor-ferenczi/se-dotnet-compat)) | Windows + Linux |

On **Windows** both launchers are built; on **Linux** only `MagnetarInterim`
(.NET 10).

Compatibility plugins are loaded implicitly:
- [se-dotnet-compat](https://github.com/viktor-ferenczi/se-dotnet-compat) for .NET 10 compatibility
- [se-linux-compat](https://github.com/viktor-ferenczi/se-linux-compat) for Linux compatibility

You can register new plugins by making PRs to the [MagnetarHub](https://github.com/CometWorks/magnetar-hub).

## Control plane — Quasar

[**Quasar**](https://github.com/viktor-ferenczi/Quasar/releases) is a separate
control plane with a Web UI that can manage and control **multiple Magnetar
instances** from one place. Each Magnetar reports structured status and logs;
Quasar orchestrates them.

## Documentation

| Page | What's in it |
| ---- | ------------ |
| [Install & Releases](Docs/Install.md) | Prebuilt bundles, what to download, installing. |
| [Usage](Docs/Usage.md) | Running the launcher, daemon mode, handoff to the DS. |
| [Configuration](Docs/Configuration.md) | Config/install dirs, DS detection, environment variables. |
| [Plugins](Docs/Plugins.md) | Plugin hubs and the trust boundary. |
| [Building](Docs/Build.md) | Per-platform build, dependency staging, packaging, releases. |
| [Repository layout](Docs/Layout.md) | What lives where in the source tree. |

For the full **code handbook** — architecture overview, launch sequence, and a
navigable module-by-module / file-by-file reference of the entire source tree —
see **[Docs/TOC.md](Docs/TOC.md)**.

## Contact

[Discord](https://discord.gg/z8ZczP2YZY) for support and developer discussion.
GitHub issues and PRs for bug reports and contributions.
