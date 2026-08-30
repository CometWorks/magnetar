# Building Magnetar

The whole build is one command:

```sh
dotnet build -c Release Magnetar.slnx
```

There are no build scripts. MSBuild compiles everything and its Deploy targets
stage a complete, portable install tree (see [Deployment](#deployment)).

The set of launchers depends on the host OS:

| Host OS | Launchers produced | Target frameworks |
| ------- | ------------------ | ----------------- |
| Windows | `MagnetarLegacy` + `MagnetarInterim` | `net48` + `net10.0` |
| Linux   | `MagnetarInterim` | `net10.0` |

`MagnetarLegacy` runs the dedicated server on .NET Framework 4.8 and is
Windows only, because the .NET Framework reference assemblies it needs do not
exist on Linux. `MagnetarInterim` runs the server on .NET 10 (via
[se-dotnet-compat](https://github.com/CometWorks/dotnet-compat), plus
[se-linux-compat](https://github.com/CometWorks/linux-compat) on Linux) and
builds on both platforms. Each project selects its target frameworks with the
MSBuild `$(OS)` reserved property, so the same solution builds correctly on
either host.

## Prerequisites

* The Pulsar submodule. Magnetar's plugin-loader core (`Pulsar.Shared`,
  `Pulsar.Protocol`) and the out-of-process Roslyn compiler come from the
  [`Pulsar/`](../Pulsar/) git submodule:

  ```sh
  git clone --recurse-submodules https://github.com/CometWorks/magnetar
  # or, in an existing clone:
  git submodule update --init
  ```

* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
* On Windows, the
  [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)
  for the `MagnetarLegacy` target.
* A Space Engineers Dedicated Server install (Steam or `steamcmd`). The build
  references its assemblies; nothing else is downloaded.

No native libraries are needed at build time. On Linux the
[linux-compat](https://github.com/CometWorks/linux-compat) plugin downloads
them as assets when the server first runs.

## Configuration

Build settings live in [Directory.Build.props](../Directory.Build.props),
following Pulsar's convention. Three properties matter:

| Property | Meaning | Default (Windows) | Default (Linux) |
| -------- | ------- | ----------------- | --------------- |
| `Magnetar` | Deploy folder for the install tree | `%APPDATA%\Magnetar` | `$XDG_DATA_HOME/Magnetar`, else `~/.local/share/Magnetar` |
| `DS64` | Folder containing `SpaceEngineersDedicated.exe` | Steam registry key, else the default Steam path | `~/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64` |
| `Steamworks` | Folder containing `Steamworks.NET.dll` | `$(DS64)` | `$(DS64)` |

To override them, copy the "Override Project Settings" `PropertyGroup` into a
`Directory.Build.props.user` file (git ignored) in the repo root, wrapped in a
top-level `<Project>` element, and fill in your paths. Anything left empty
there falls back to the defaults. Environment variables and `-p:DS64=...`
style command line properties work too.

The submodule's projects are deliberately not part of `Magnetar.slnx`. They
build through the `ProjectReference`s in
[Legacy.csproj](../Legacy/Legacy.csproj), which forward the properties they
need (`Steamworks`, the deployment root, and `SteamApiFileName`, pinned to
`steam_api64.dll` because the DS depot carries the Windows files on every
platform).

## Deployment

Every build deploys. The Verify targets fail early with a clear message when
`DS64` or `Steamworks` is wrong; the Deploy targets then stage the install
tree into `$(Magnetar)`:

```
$(Magnetar)/
  MagnetarLegacy.exe                 Windows only
  MagnetarInterim.exe | .bin         plus its .dll/.deps.json/.runtimeconfig.json
  LICENSE, README.md
  Libraries/
    MagnetarLegacy/                  per-launcher managed dependencies
    MagnetarInterim/                 (Pulsar.Shared, PluginSdk, Harmony, ...)
    Compiler/                        the out-of-process Roslyn compiler;
                                     one copy serves both launchers
  Config/                            MagnetarConfig (Terminal.Gui) with its
                                     own dependencies; deployed by the
                                     ConfigTerminal project
```

The tree is portable: copy it anywhere and run the launcher from there. The
Deploy targets wipe and rewrite `Libraries/` and `Config/` on every build, so
treat `$(Magnetar)` as build output, not as a place for your own files. The
launcher's configuration is safe because it lives in the `Magnetar` folder,
which the deploy never touches (see [Configuration.md](Configuration.md)).

To build just one launcher on Windows, restrict the target framework:

```powershell
dotnet build -c Release Legacy/Legacy.csproj -f net48      # MagnetarLegacy
dotnet build -c Release Legacy/Legacy.csproj -f net10.0    # MagnetarInterim
```

The `DeployLibraryFile` list in `Legacy.csproj` is maintained by hand,
mirroring Pulsar's convention. A safety net in the Deploy target warns when
the build output contains a copy-local dependency the list does not stage,
which is the usual symptom after a Pulsar submodule bump adds a package.

## Run / verify

Run the launcher from the deploy folder in place of
`SpaceEngineersDedicated.exe`:

```powershell
& "$env:APPDATA\Magnetar\MagnetarInterim.exe"
```

```sh
~/.local/share/Magnetar/MagnetarInterim.bin
```

A successful launch logs `Game ready...` once the world has loaded. Stop the
server with `Ctrl+C`, or with `SIGTERM` for a save and clean exit.

On Linux the server also needs the native runtime libraries (libsteam_api,
EOS, Havok, RecastDetour, VRageNative) and a current `Steamworks.NET.dll`.
The linux-compat plugin downloads them as assets on first run. To supply your
own copies instead, drop them into `Libraries/MagnetarInterim/`; the launcher
picks up any `lib*.so*` and `Steamworks.NET.dll` found there before anything
else. A build wipes that folder, so re-copy manual overrides after building.

## MagnetarMod MDK2 project

The companion world mod under [MagnetarMod/](../MagnetarMod/) has its own MDK2
project. The actual Space Engineers/Workshop content root is
[`MagnetarMod/src/`](../MagnetarMod/src/); the `.csproj` and local MDK config
stay one level above it:

```sh
dotnet build MagnetarMod/MagnetarMod.csproj
```

It targets `net48`, uses `Mal.Mdk2.References` and `Mal.Mdk2.ModAnalyzers`,
and reads the local Space Engineers install from
`MagnetarMod/MagnetarMod.mdk.local.ini`. It is not part of `Magnetar.slnx`, so
the release pipeline never builds it. Space Engineers compiles the world mod
when loading it; the MDK2 project exists for local validation and analyzer
coverage.

## How the multi-target build works

* Target frameworks are OS-conditional in
  [Legacy.csproj](../Legacy/Legacy.csproj) and
  [PluginSdkTests.csproj](../PluginSdkTests/PluginSdkTests.csproj):
  `net48;net10.0` on Windows, `net10.0` on Linux. The submodule's
  `Pulsar/Shared` multi-targets the same pair on its own.
* The assembly name switches per target framework: `net48` builds
  `MagnetarLegacy`, `net10.0` builds `MagnetarInterim`.
* Windows-only items (application icon, `app.manifest`, the
  `VRage.Platform.Windows` reference) are gated with
  `Condition="'$(OS)' == 'Windows_NT'"`.
* Platform-specific code uses `RuntimeInformation.IsOSPlatform(...)` where it
  must compile for both `net48` and `net10.0`, and `OperatingSystem.IsLinux()`
  only inside `#if NETCOREAPP`. `Loader/NativeLibraryPreloader.cs` is excluded
  from the `net48` compile entirely.

## Continuous integration / releases

[`.github/workflows/release.yml`](../.github/workflows/release.yml) builds
both platforms and publishes a GitHub release with the two `.7z` bundles
attached.

### Triggers

| Trigger | Behaviour |
| ------- | --------- |
| Push to `main` | Reads `<Version>` from [Directory.Build.props](../Directory.Build.props). Builds and publishes a public **latest** release `v<version>` only if that version is strictly higher than the latest existing release (the first release ever always counts as newer). Otherwise the whole run is skipped. |
| Manual run (`workflow_dispatch`) | Always builds for the current version. A **draft** boolean input (default **true**) decides the outcome: when set it publishes a draft release (tag `v<version>`, or `v<version>-build.<run>` if that tag exists); when cleared it publishes a real, public **latest** release. |

### Jobs

* **version-check** parses the version, decides `should_build` / `draft`, and
  probes the DS depot's public build id (via `steamcmd +app_info_print`, no
  depot download) to key the DS cache.
* **build-linux** and **build-windows** check out the repo with the Pulsar
  submodule, restore the cached DS library set (or download the depot via
  `steamcmd` on a miss), run `dotnet build -c Release Magnetar.slnx` with the
  `Magnetar` property pointed at a staging tree, run both test suites
  (Linux job), verify the staged tree, and pack it with 7-Zip as
  `MagnetarFor<OS>-<version>.7z`.
* **release** downloads both bundles and creates the release with `gh`.

### Dedicated Server cache

The build only references the managed assemblies in `DedicatedServer64/`, so
each job caches just that library set (roughly 186 MB, via `actions/cache`,
path `ds64`), never the multi-GB `Content/`. The cache key is
`ds64-<os>-<ds_buildid>`, so an unchanged DS version restores instantly and a
new Keen release causes exactly one fresh `steamcmd` download per OS. The
Linux job forces the Windows depot (`+@sSteamCmdForcePlatformType windows`)
because there is no native Linux DS. Both jobs bootstrap `steamcmd` once
(`+quit`) and retry the `app_update`, because a brand-new `steamcmd`
self-updates on its first run and otherwise aborts with
`Failed to install app '298740' (Missing configuration)`.

### Required repository configuration

None. The `GITHUB_TOKEN` (`contents: write`) publishes the release; no other
secret is needed.

### Testing the workflow from a branch

The workflow lives on `main`, so `workflow_dispatch` can run it against any
branch, executing that branch's workflow and code. The default `draft=true`
keeps such runs on the draft path. A push to a non-`main` branch triggers
nothing.

```sh
git push origin HEAD:my-branch
gh workflow run release.yml -R CometWorks/magnetar --ref my-branch
gh run watch -R CometWorks/magnetar \
  "$(gh run list -R CometWorks/magnetar --workflow=release.yml -L1 --json databaseId -q '.[0].databaseId')"
```

Prune leftover draft releases with
`gh release delete <tag> -R CometWorks/magnetar --yes`.
