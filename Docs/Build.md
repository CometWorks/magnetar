# Building Magnetar

Magnetar builds on **Windows** and **Linux**. The set of launchers produced
depends on the host OS:

| Host OS | Launchers produced | Target frameworks |
| ------- | ------------------ | ----------------- |
| Windows | `MagnetarLegacy` + `MagnetarInterim` | `net48` + `net10.0` |
| Linux   | `MagnetarInterim` | `net10.0` |

`MagnetarLegacy` runs the dedicated server on .NET Framework 4.8 and is
Windows-only — the .NET Framework reference assemblies it needs do not exist on
Linux. `MagnetarInterim` runs the server on .NET 10 (via
[se-dotnet-compat](https://github.com/viktor-ferenczi/se-dotnet-compat), plus
[se-linux-compat](https://github.com/viktor-ferenczi/se-linux-compat) on Linux)
and is built on both platforms.

The per-OS target frameworks are selected in each project with the MSBuild
`$(OS)` reserved property (`Windows_NT` on Windows, `Unix` elsewhere), so the
same `Magnetar.sln` builds correctly on either host with no manual switches.

---

## Windows

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)
  (reference assemblies for the `net48` / `MagnetarLegacy` target)
* Space Engineers Dedicated Server installed via Steam

### Dedicated server location

The DS folder (`DedicatedServer64/`, containing `SpaceEngineersDedicated.exe`)
is resolved automatically from the Steam uninstall registry key, falling back to
`C:\Program Files (x86)\Steam\...`. See [Directory.Build.props](../Directory.Build.props).

Override it if your install is elsewhere:

```powershell
# environment variable
$env:DS64 = "D:\Steam\steamapps\common\SpaceEngineersDedicatedServer\DedicatedServer64"
dotnet build -c Release Magnetar.sln

# or per-invocation MSBuild property
dotnet build -c Release Magnetar.sln -p:DS64="D:\...\DedicatedServer64"
```

### Build

```powershell
dotnet build -c Release Magnetar.sln
```

This builds both launchers. Each project's targets then:

* **Pre-build** ([verify.bat](../verify.bat)) — fails the build early with a clear
  message if the resolved `DS64` path does not exist.
* **Post-build** ([deploy.bat](../Legacy/deploy.bat)) — copies the launcher and
  its dependencies into the Magnetar install folder, by default
  `%APPDATA%\Magnetar` (override with the `Magnetar` property/env var). The
  launcher executable lands at the root; its managed dependencies go under
  `Libraries\MagnetarLegacy\` or `Libraries\MagnetarInterim\`.

To build just one launcher, restrict the target framework:

```powershell
dotnet build -c Release Legacy/Legacy.csproj -f net48      # MagnetarLegacy
dotnet build -c Release Legacy/Legacy.csproj -f net10.0    # MagnetarInterim
```

### Run / verify

Run either launcher in place of `SpaceEngineersDedicated.exe`:

```powershell
& "$env:APPDATA\Magnetar\MagnetarLegacy.exe"
& "$env:APPDATA\Magnetar\MagnetarInterim.exe"
```

A successful launch logs `Game ready...` once the world has loaded. The server
then runs normally; stop it with `Ctrl+C` (or kill the process).

---

## Linux

> **Do not** target `net48` on Linux. Build the solution as-is — the OS-conditional
> target frameworks already restrict the Linux build to `net10.0`.

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
* `git`, `dotnet`, `bash`
* Space Engineers Dedicated Server installed via Steam (or any local copy of
  `DedicatedServer64/`)

### One-time dependency staging

Linux needs a few dependencies that aren't on NuGet staged into
`build/Libraries/` before the first build. [build.sh](../build.sh) orchestrates
this:

```sh
./build.sh --deps-only
```

It populates `build/Libraries/` with:

| Artefact | Source |
| -------- | ------ |
| `Steamworks.NET.dll` | built from [rlabrecque/Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET) by [Scripts/build_steamworks_net.sh](../Scripts/build_steamworks_net.sh) |
| `libsteam_api.so` | the proprietary Linux Steamworks SDK blob — drop it in `Vendor/` or set `LIBSTEAM_API_SO=` |
| `libEOSSDK-Linux-Shipping.so` | Epic Online Services SDK (drop it in Magnetar's `Vendor/`, or set `LIBEOSSDK_SO=`) |
| `libHavok.so`, `libRecastDetour.so`, `libVRageNative.so` | PE-loader replacements for Keen's Windows native DLLs, built from [se-linux-compat](https://github.com/viktor-ferenczi/se-linux-compat)'s `NativeWrappers/` (or `LIBHAVOK_SO=` etc.) |

The proprietary blobs (`libsteam_api.so`, `libEOSSDK-Linux-Shipping.so`) are not
committed; `build.sh` prints exactly where it looked if one is missing. Override
the DS location for staging the same way as the build:

```sh
DS64=/opt/se1-ds/DedicatedServer64 ./build.sh --deps-only
```

`build.sh` flags: `--deps-only` (stage only), `--skip-deps` (package only),
`--clean` (wipe caches and rebuild), or no args to stage **and** package a
`dist/MagnetarForLinux.<date>.<hash>.7z` bundle.

### Build

Once `build/Libraries/` is populated:

```sh
dotnet build -c Release Magnetar.sln
```

On Linux, `Legacy.csproj`'s `AfterBuild` target copies `build/Libraries/*` next
to the produced `MagnetarInterim` apphost (`cp -a`, preserving soname symlinks).
If `build/Libraries/` is missing the build fails fast with a clear message.

### Publish

Produce a framework-dependent bundle ready to drop next to the dedicated server:

```sh
dotnet publish -c Release Legacy/Legacy.csproj \
    -r linux-x64 --self-contained false \
    -o ~/.local/share/Magnetar
```

The `AfterPublish` target stages `build/Libraries/*` into the publish output too.
Running the full `./build.sh` (no flags) does this publish and packs the result
into `dist/`.

### Run / verify

```sh
~/.local/share/Magnetar/MagnetarInterim
```

A successful launch logs `Game ready...` once the world has loaded.

---

## Build properties & environment variables

These are read at **build time** (MSBuild property or environment variable of
the same name); resolved per OS in [Directory.Build.props](../Directory.Build.props).

| Name | Effect | Default (Windows) | Default (Linux) |
| ---- | ------ | ----------------- | --------------- |
| `DS64` | Folder containing `SpaceEngineersDedicated.exe` | Steam registry key | `~/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64` |
| `Magnetar` | Install/deploy folder | `%APPDATA%\Magnetar` | `~/.local/share/Magnetar` |
| `Steamworks` | Path to `Steamworks.NET.dll` (net48/net10 reference) | `$(DS64)\Steamworks.NET.dll` | `build/Libraries/Steamworks.NET.dll` |

`build.sh` honours additional overrides for Linux dependency staging:
`MAGNETAR_REPO_DIR`, `BUILD_DIR`, `LIBRARIES_DIR`, `OUTPUT_DIR`, plus the per-blob
`LIBSTEAM_API_SO`, `LIBEOSSDK_SO`, `LIBHAVOK_SO`, `LIBRECASTDETOUR_SO`,
`LIBVRAGENATIVE_SO` (and the `LINUXCOMPAT_NATIVE` search root).

Runtime knobs (set when launching, not building) — `MAGNETAR_SAFE_MODE`,
`XDG_CONFIG_HOME`, `XDG_DATA_HOME`, and the `-ds64` / `-config` command-line
overrides — are documented in the [README](../README.md).

---

## How the multi-target build works

* **Target frameworks** are OS-conditional in
  [Legacy.csproj](../Legacy/Legacy.csproj),
  [Shared.csproj](../Shared/Shared.csproj) and
  [PluginSdkTests.csproj](../PluginSdkTests/PluginSdkTests.csproj):
  `net48;net10.0` on Windows, `net10.0` on Linux.
* **Assembly name** switches per target framework: `net48` →
  `MagnetarLegacy`, `net10.0` → `MagnetarInterim`.
* **Windows-only items** (application icon, `app.manifest`, the
  `VRage.Platform.Windows` reference, `verify.bat`/`deploy.bat`) are gated with
  `Condition="'$(OS)' == 'Windows_NT'"`.
* **Linux-only items** (the `build/Libraries/*` copy targets, the
  `Steamworks.NET.dll` presence check) are gated with
  `Condition="'$(OS)' != 'Windows_NT'"`.
* **Source guards** — platform-specific code uses
  `RuntimeInformation.IsOSPlatform(...)` where it must compile for both `net48`
  and `net10.0`, and `OperatingSystem.IsLinux()` (.NET 5+) only inside
  `#if NETCOREAPP`. `Loader/NativeLibraryPreloader.cs` (the Linux native
  bootstrap) is excluded from the `net48` compile entirely.

---

## Continuous integration / Releases

[`.github/workflows/release.yml`](../.github/workflows/release.yml) builds both
platforms and publishes a GitHub release with the two `.7z` bundles attached.

### Triggers

| Trigger | Behaviour |
| ------- | --------- |
| Push to `main` | Reads `<Version>` from [Legacy/Legacy.csproj](../Legacy/Legacy.csproj). Builds and publishes a public **latest** release `v<version>` only if that version is strictly higher than the latest existing release (the first release ever always counts as newer). Otherwise the whole run is skipped — nothing is built and no existing release is touched. |
| Manual run (`workflow_dispatch`) | Always builds and publishes a **draft** release (not marked latest) for the current Legacy version, regardless of what is already released. The draft tag is `v<version>`, or `v<version>-build.<run>` if that tag already exists. |

### Jobs

* **version-check** — parses the version and decides `should_build` / `draft`;
  every other job is gated on `should_build`.
* **build-linux** (`ubuntu-latest`) — installs the .NET 8 + 10 SDKs and
  `p7zip-full`; builds the [se-linux-compat](https://github.com/viktor-ferenczi/se-linux-compat)
  `NativeWrappers` in Docker (see
  [`.github/docker/nativewrappers.Dockerfile`](../.github/docker/nativewrappers.Dockerfile)),
  cached by the upstream commit SHA so they only rebuild when that repo's `HEAD`
  changes; downloads the Vendor blobs and the **Windows** DS depot via
  `steamcmd`; then runs [`build.sh`](../build.sh) and uploads the bundle,
  renamed to `MagnetarForLinux-<version>.7z`.
* **build-windows** (`windows-latest`) — installs the .NET 10 SDK (the image
  ships the .NET Framework 4.8 targeting pack); downloads the DS via `steamcmd`;
  builds `Magnetar.sln` with `Magnetar` pointed at a staging tree so
  [deploy.bat](../Legacy/deploy.bat) lays out the install folder there, then
  packs it as `MagnetarForWindows-<version>.7z`.
* **release** (`ubuntu-latest`) — downloads both bundles and creates the release
  with `gh`.

### Required repository configuration

| Name | Kind | Purpose |
| ---- | ---- | ------- |
| `VENDOR_ARCHIVE_URL` | Repository **secret** | Download URL returning `Vendor.7z` (the `Vendor/` folder with `libsteam_api.so` and `libEOSSDK-Linux-Shipping.so`). Fetched fresh every run; the existing `Vendor/` is removed and replaced. |

The DS is retrieved anonymously (Steam app `298740`); the Linux job forces the
Windows depot (`+@sSteamCmdForcePlatformType windows`) because there is no native
Linux DS — Magnetar runs the Windows files via the native wrappers. The
`GITHUB_TOKEN` (`contents: write`) is used for the release; no other secret is
needed.
