# Install & Releases

Magnetar replaces `SpaceEngineersDedicated.exe` in your Dedicated Server
installation. Two launchers are provided:

* **`MagnetarLegacy`** — runs the
  [Space Engineers 1](https://steampowered.com/app/244850) Dedicated Server on
  [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
  (Windows only).
* **`MagnetarInterim`** — runs the Dedicated Server on
  [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (via
  [se-dotnet-compat](https://github.com/viktor-ferenczi/se-dotnet-compat);
  Windows and Linux).

On **Windows** both launchers are shipped. On **Linux** only `MagnetarInterim`
is shipped — .NET Framework 4.8 is Windows-only, and the Linux dedicated server
runs on .NET 10 via [se-dotnet-compat](https://github.com/viktor-ferenczi/se-dotnet-compat)
plus [se-linux-compat](https://github.com/viktor-ferenczi/se-linux-compat).

## Prebuilt bundles

Prebuilt bundles are published on the
[GitHub Releases](https://github.com/viktor-ferenczi/Magnetar/releases) page:

| Asset | Contents |
| ----- | -------- |
| `MagnetarForLinux-<version>.7z` | `install.sh` / `uninstall.sh` + the `MagnetarInterim` (.NET 10) bundle. Extract and run `./install.sh`. |
| `MagnetarForWindows-<version>.7z` | The `Magnetar/` install tree: `MagnetarLegacy.exe` (.NET 4.8) and `MagnetarInterim.exe` (.NET 10) plus their `Libraries/`. Extract next to your dedicated server. |

After installing, see **[Usage](Usage.md)** for how to run the launcher.

## How releases are produced

Releases are produced automatically by the
[`Release`](../.github/workflows/release.yml) GitHub Actions workflow, which
builds both platforms (pulling the dedicated server via `steamcmd` and the
[se-linux-compat](https://github.com/viktor-ferenczi/se-linux-compat) native
wrappers) and attaches both `.7z` files. A push to `main` publishes a new public
release when the version in `Directory.Build.props` is higher than the latest
release; a manual run produces a draft by default, or a public release if you
clear its **draft** option. See
[Build.md](Build.md#continuous-integration--releases) for the full release
process.
