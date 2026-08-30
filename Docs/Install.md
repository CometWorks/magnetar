# Install & Releases

Magnetar replaces `SpaceEngineersDedicated.exe` in your Dedicated Server
installation. Two launchers are provided:

* **`MagnetarLegacy`** — runs the
  [Space Engineers 1](https://steampowered.com/app/244850) Dedicated Server on
  [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
  (Windows only).
* **`MagnetarInterim`** — runs the Dedicated Server on
  [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (via
  [dotnet-compat](https://github.com/CometWorks/dotnet-compat);
  Windows and Linux).

On **Windows** both launchers are shipped. On **Linux** only `MagnetarInterim`
is shipped — .NET Framework 4.8 is Windows-only, and the Linux dedicated server
runs on .NET 10 via [dotnet-compat](https://github.com/CometWorks/dotnet-compat)
plus [linux-compat](https://github.com/CometWorks/linux-compat).

## Prebuilt bundles

Prebuilt bundles are published on the
[GitHub Releases](https://github.com/CometWorks/magnetar/releases) page:

| Asset | Contents |
| ----- | -------- |
| `MagnetarForLinux-<version>.7z` | `install.sh` / `uninstall.sh` + the `MagnetarInterim.bin` (.NET 10) install tree. Extract and run `./install.sh`. |
| `MagnetarForWindows-<version>.7z` | The `Magnetar/` install tree: `MagnetarLegacy.exe` (.NET 4.8) and `MagnetarInterim.exe` (.NET 10) plus `Libraries/` (per-launcher dependencies and the out-of-process compiler). Extract into `%APPDATA%` so it lands as `%APPDATA%\Magnetar`. |

After installing, see **[Usage](Usage.md)** for how to run the launcher.

## How releases are produced

Releases are produced automatically by the
[`Release`](../.github/workflows/release.yml) GitHub Actions workflow, which
builds both platforms (pulling the dedicated server via `steamcmd` and the
[linux-compat](https://github.com/CometWorks/linux-compat) native
wrappers) and attaches both `.7z` files. A push to `main` publishes a new public
release when the version in `Directory.Build.props` is higher than the latest
release; a manual run produces a draft by default, or a public release if you
clear its **draft** option. See
[Build.md](Build.md#continuous-integration--releases) for the full release
process.
