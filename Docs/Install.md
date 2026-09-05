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

## Installing

Magnetar is portable, like Pulsar. Download the bundle for your platform from
the [GitHub Releases](https://github.com/CometWorks/magnetar/releases) page
and extract its `Magnetar/` folder anywhere you like:

| Asset | Contents |
| ----- | -------- |
| `MagnetarForLinux-<version>.7z` | `MagnetarInterim.bin` (.NET 10) with `Libraries/` and the `MagnetarConfig.bin` terminal UI. |
| `MagnetarForWindows-<version>.7z` | `MagnetarLegacy.exe` (.NET 4.8) and `MagnetarInterim.exe` (.NET 10) with `Libraries/` and `MagnetarConfig.exe`. |

`<version>` has four components: the Pulsar release Magnetar is built on, plus a
Magnetar build number, as in `2.3.3.0`. See
[Versioning](Build.md#versioning) for how it is bumped.

Then run the launcher in place of `SpaceEngineersDedicated.exe`. The launchers
keep their shared configuration and logs in the `Magnetar` folder inside the
install folder, so the whole thing moves as one unit. To uninstall, delete the
folder.

An update replaces the install folder's binaries (`Libraries/`, the launchers
and the config tool), so do not keep unrelated files in it. The `Magnetar` configuration
folder survives updates.

`MagnetarInterim` and `MagnetarConfig` need the .NET 10 runtime
(`Microsoft.NETCore.App 10.x`) installed on the host. On Linux the native
runtime libraries arrive through the linux-compat plugin on first launch, so
the host also needs outbound HTTPS to GitHub at that point.

After installing, see **[Usage](Usage.md)** for how to run the launcher.

## How releases are produced

Releases are produced automatically by the
[`Release`](../.github/workflows/release.yml) GitHub Actions workflow, which
builds both platforms with `dotnet build` (pulling the dedicated server via
`steamcmd` for the build-time references) and attaches both `.7z` files. A
push to `main` publishes a new public release when the version in
`Directory.Build.props` is higher than the latest release; a manual run
produces a draft by default, or a public release if you clear its **draft**
option. See [Build.md](Build.md#continuous-integration--releases) for the full
release process.
