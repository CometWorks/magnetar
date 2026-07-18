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
[se-dotnet-compat](https://github.com/CometWorks/dotnet-compat), plus
[se-linux-compat](https://github.com/CometWorks/linux-compat) on Linux)
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
  `Libraries\MagnetarLegacy\` or `Libraries\MagnetarInterim\`. Roslyn compiler
  dependencies that are private to source-plugin compilation live in the
  matching `Compiler\` subdirectory.

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
| `libHavok.so`, `libRecastDetour.so`, `libVRageNative.so` | PE-loader replacements for Keen's Windows native DLLs, downloaded prebuilt from the [linux-native-wrappers](https://github.com/CometWorks/linux-native-wrappers) GitHub release by [Scripts/fetch_native_wrappers.sh](../Scripts/fetch_native_wrappers.sh) into `build/native/` (or `LIBHAVOK_SO=` etc.) |

The proprietary blobs (`libsteam_api.so`, `libEOSSDK-Linux-Shipping.so`) are not
committed; `build.sh` prints exactly where it looked if one is missing. Override
the DS location for staging the same way as the build:

```sh
DS64=/opt/se1-ds/DedicatedServer64 ./build.sh --deps-only
```

`build.sh` flags: `--deps-only` (stage only), `--skip-deps` (package only),
`--clean` (wipe caches and rebuild), or no args to stage **and** package the
`dist/MagnetarForLinux.7z` bundle.

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

The full packager also publishes the **`MagnetarConfig`** terminal UI
(`ConfigTerminal/`, framework-dependent net10.0) into its own `Config/` folder
and drops a root launcher/shim next to `MagnetarInterim` — `Magnetar/MagnetarConfig`
(bash) on Linux, `<Magnetar>\MagnetarConfig.bat` on Windows — so the config tool
ships in both bundles with its Terminal.Gui dependencies isolated from the
launcher. See [Config tool internals → Build & packaging](ConfigTerminalInternals.md#13-build-packaging-and-documentation-integration).

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
overrides — are documented in [Configuration.md](Configuration.md).

---

## MagnetarMod MDK2 project

The companion world mod under [MagnetarMod/](../MagnetarMod/) has its own MDK2
project. The actual Space Engineers/Workshop content root is
[`MagnetarMod/src/`](../MagnetarMod/src/); the `.csproj` and local MDK config
stay one level above it:

```sh
dotnet build MagnetarMod/MagnetarMod.csproj
dotnet build Magnetar.sln -c Workshop
```

It targets `net48`, uses `Mal.Mdk2.References` and
`Mal.Mdk2.ModAnalyzers`, and reads the local Space Engineers install from
`MagnetarMod/MagnetarMod.mdk.local.ini`. It is included in `Magnetar.sln` only
for the `Workshop|Any CPU` solution configuration. The normal `Debug` and
`Release` solution configurations leave `MagnetarMod` unselected for build, so
the regular release pipeline's `dotnet build -c Release Magnetar.sln` does not
build the mod project. Space Engineers still compiles the world mod when loading
it; the MDK2 project is for local/workshop validation and analyzer coverage.

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
| Push to `main` | Reads `<Version>` from [Directory.Build.props](../Directory.Build.props). Builds and publishes a public **latest** release `v<version>` only if that version is strictly higher than the latest existing release (the first release ever always counts as newer). Otherwise the whole run is skipped — nothing is built and no existing release is touched. |
| Manual run (`workflow_dispatch`) | Always builds for the current version, regardless of what is already released. A **draft** boolean input (default **true**) decides the outcome: when set (the default) it publishes a **draft** release (not marked latest), tag `v<version>` or `v<version>-build.<run>` if that tag already exists; when cleared it publishes a real, public **latest** release `v<version>` — no version-gate check, since the operator asked for it explicitly. |

### Jobs

* **version-check** — parses the version and decides `should_build` / `draft`;
  every other job is gated on `should_build`. The version is the single
  `<Version>` defined in [Directory.Build.props](../Directory.Build.props), which
  also drives `AssemblyVersion` / `FileVersion` for every project. When building, it also probes the
  DS depot's public **build id** (via `steamcmd +app_info_print`, no depot
  download) and exposes it as the `ds_buildid` output used to key the DS cache.
* **build-linux** (`ubuntu-latest`) — installs the .NET 8 + 10 SDKs and
  `p7zip-full`; restores the cached **DS library set** (or downloads the
  **Windows** DS depot via `steamcmd` on a cache miss — see below); downloads
  the Vendor blobs; then runs [`build.sh`](../build.sh), which fetches the
  prebuilt native wrappers from the
  [linux-native-wrappers](https://github.com/CometWorks/linux-native-wrappers)
  release via
  [Scripts/fetch_native_wrappers.sh](../Scripts/fetch_native_wrappers.sh)
  (cached by release tag under `build/native/`), and uploads the bundle,
  renamed to `MagnetarForLinux-<version>.7z`.
* **build-windows** (`windows-latest`) — installs the .NET 10 SDK (the image
  ships the .NET Framework 4.8 targeting pack); restores the cached DS library
  set (or downloads via `steamcmd` on a miss); builds `Magnetar.sln` with
  `Magnetar` pointed at a staging tree so [deploy.bat](../Legacy/deploy.bat)
  lays out the install folder there, then packs it as
  `MagnetarForWindows-<version>.7z`.
* **release** (`ubuntu-latest`) — downloads both bundles and creates the release
  with `gh`.

### Dedicated Server cache

The build only references the managed assemblies in `DedicatedServer64/`, so
each build job caches just that **~186 MB library set** (via `actions/cache`,
path `ds64`) — never the multi-GB `Content/`. The cache key is
`ds64-<os>-<ds_buildid>`, where `ds_buildid` is the DS depot's public build id
from `version-check`. Consequences:

* Unchanged DS version → cache hit → the `steamcmd` download is skipped entirely
  (faster, and no exposure to `steamcmd`'s first-run flakiness).
* First build after Keen ships a new DS version → new build id → cache miss → a
  full `steamcmd` download into a scratch dir, of which only `DedicatedServer64/`
  is copied into `ds64` and cached. So the DS auto-updates exactly once per DS
  release.
* `ds64/` is populated only after the post-download marker check passes, so a
  failed download never caches a partial tree. If the build id can't be probed,
  the key falls back to a unique value (cache miss, full download) rather than
  wedging the release.

Two ~186 MB caches (Linux + Windows) stay well under GitHub's 10 GB per-repo
cache budget, so they are not evicted by the small native-wrappers cache.

### Required repository configuration

| Name | Kind | Purpose |
| ---- | ---- | ------- |
| `VENDOR_ARCHIVE_URL` | Repository **secret** | Download URL returning `Vendor.7z` (the `Vendor/` folder with `libsteam_api.so` and `libEOSSDK-Linux-Shipping.so`). Fetched fresh every run; the existing `Vendor/` is removed and replaced. |

On a cache miss the DS is retrieved anonymously (Steam app `298740`); the Linux job forces the
Windows depot (`+@sSteamCmdForcePlatformType windows`) because there is no native
Linux DS — Magnetar runs the Windows files via the native wrappers. Both jobs
bootstrap `steamcmd` once (`+quit`) and then retry the `app_update` a few times:
a brand-new `steamcmd` self-updates on its first run, which otherwise makes the
in-session install abort with `Failed to install app '298740' (Missing
configuration)`. The `GITHUB_TOKEN` (`contents: write`) is used for the release;
no other secret is needed.

### Testing the workflow from a branch

Because the workflow lives on the default branch (`main`), `workflow_dispatch` is
registered and can be run against **any** branch — a dispatched run executes the
workflow *and* code from the chosen branch. Leaving the **draft** input at its
default (`true`) keeps it on the **draft** path, so it never publishes a public
release; pass `-f draft=false` only when you deliberately want a real release. A
push to a non-`main` branch does not trigger anything (the push trigger is
`main`-only). To iterate on a branch without touching `main`:

```sh
git push origin HEAD:my-branch
gh workflow run release.yml -R viktor-ferenczi/Magnetar --ref my-branch
gh run watch -R viktor-ferenczi/Magnetar \
  "$(gh run list -R viktor-ferenczi/Magnetar --workflow=release.yml -L1 --json databaseId -q '.[0].databaseId')"
```

Each dispatch (with the default `draft=true`) creates a draft release for the
current version (`v<version>`, or `v<version>-build.<run>` if that tag already
exists); prune them with
`gh release delete <tag> -R viktor-ferenczi/Magnetar --yes`.
