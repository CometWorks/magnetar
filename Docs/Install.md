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
| `MagnetarForLinux-<version>.7z` | `MagnetarInterim.bin` (.NET 10) with `Libraries/`. |
| `MagnetarForWindows-<version>.7z` | `MagnetarLegacy.exe` (.NET 4.8) and `MagnetarInterim.exe` (.NET 10) with `Libraries/`. |

For guided installation, download **MagnetarConfig** separately from the current
`magnetarconfig-v*` release on
[config-tools releases](https://github.com/CometWorks/config-tools/releases):
`MagnetarConfig-linux-x64.bin` on Linux or `MagnetarConfig-win-x64.exe` on Windows.
It bundles its own .NET runtime, Terminal.Gui and archive reader; the server's
runtime requirements below still apply. Keep the tool outside the server target
(required for setup on Windows), make the Linux file executable with `chmod +x`,
and run it with `--setup --target <install-folder>`.

Setup offers **Check prerequisites**, **Install**, **Update** and **Uninstall**.
It downloads the server package from CometWorks/magnetar, but does not install
Dedicated Server game files or system runtimes. See the
[README quick start](../README.md#install-or-update-the-server) for Linux/Windows
commands and the [tool manual](MagnetarConfig.md) for offline archives and checksums.

`<version>` has four components: the Pulsar release Magnetar is built on, plus a
Magnetar build number, as in `2.3.3.0`. See
[Versioning](Build.md#versioning) for how it is bumped.

Then run the launcher in place of `SpaceEngineersDedicated.exe`. The launchers
keep their shared configuration and logs in the `Magnetar` folder inside the
install folder, so the whole thing moves as one unit. Use the tool's **Uninstall**
to remove package-owned program files while retaining user data. Deleting the
whole install folder manually also deletes configuration stored inside it; move
or back up anything you intend to keep first.

An update replaces the install folder's binaries (`Libraries/` and the launchers),
so do not keep unrelated files in it. The `Magnetar` configuration
folder survives updates.

`MagnetarInterim` needs the .NET 10 runtime
(`Microsoft.NETCore.App 10.x`) installed on the host. On Linux the native
runtime libraries arrive through the linux-compat plugin on first launch, so
the host also needs outbound HTTPS to GitHub at that point.

Keep server and tool updates separate: setup's **Update** changes Magnetar;
**Tools → Tool updates**, the startup update prompt, or `--self-update` changes
MagnetarConfig. The latter keeps a `.previous` executable and closes the tool;
reopen it after replacement. See
[tool update and recovery details](https://github.com/CometWorks/config-tools#updating-the-tools).

For an existing instance, use the same `-config` and `-path` with the standalone
tool; [migration from the bundled tool](../README.md#moving-from-the-bundled-tool)
does not require changing world/profile formats. The older Linux `Bin` server
layout needs a new installation folder and explicit existing config/data paths.

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
