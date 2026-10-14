# Lifecycle test suite

Starts isolated Magnetar servers and sends each stop and restart command in
each server state: running, starting, loading the world, saving, restarting,
stopping, hung. Then it checks the outcome: whether the server restarted or
exited, the exit code, whether the world was saved, that no second Magnetar
process ran, and whether plugins got the `Terminating` event. The matrix is
in `cases.py`; `Docs/ServerControl.md` describes the expected behavior.

Runs on Linux, natively (`linux`) or under Proton with the Windows launchers
(`proton-interim`, `proton-legacy`). The Quasar part of the matrix lives in
the quasar repository (`Tests/Lifecycle`) and uses this folder's rig and probe.

## Pieces

- `Probe/` is a test plugin, loaded as a DevFolder plugin. The driver writes
  command lines into `probe-cmd.txt` in the DS data folder. The probe runs
  them on the game thread (or a worker thread with a `worker ` prefix) and
  appends what happened to `probe.log`: each world load, each world save and
  its caller, `Terminating` events, and the process exit code where .NET runs
  exit handlers (Windows). Commands: `chat <text>` (a chat command as the
  server console), `sdk <ServerControl method>`, `exit`
  (`MySandboxGame.ExitThreadSafe`), `update` (Keen's auto-update stop falls
  due: an update is pending and its countdown is over), `slowsave <seconds>` (the next saves take
  that long, so a command can arrive during one), `hang <seconds>` (blocks the
  game thread) and `crash` (an unhandled exception). A `probe-hold-init.txt`
  holding a number of seconds in the data folder keeps the server in plugin
  initialization that long, once.
- `rig.py` sets up, starts, observes and stops one server.
- `cases.py` is the matrix, `run.py` runs it, `cleanup.py <work>` kills what
  an interrupted run left behind.

## Template

`--template` is a folder with `data/` (a DS data folder) and `magnetar/` (a
Magnetar config folder). Each case gets a copy, so keep it small:

- `data/SpaceEngineers-Dedicated.cfg` and `data/Saves/LifecycleTest/`, a
  small world such as a copy of the Empty World template. Set
  `AutoSaveInMinutes` to 0 in the config and in the world's `Sandbox.sbc` and
  `Sandbox_config.sbc`, or autosaves blur the saved checks. The rig sets the
  ports, `LoadWorld`, `IgnoreLastSession` and the timed auto-restart.
- `magnetar/config.xml`, `magnetar/Sources/sources.xml` with the MagnetarHub
  source, and `magnetar/GitHub/` from a server that has run Essentials
  once, so the cases don't download and compile the hub plugins again. The
  rig writes the profile (Essentials and the probe) and the probe's
  DevFolder source.
- For Proton, also `NuGet/` copied from a Linux Magnetar folder, and
  `magnetar/GitHub/` from a Windows run: with `Platform: Windows`, NuGet checks
  package signatures, which fails under Wine.

## Running on Linux

```bash
Tests/Lifecycle/run.py --platform linux --install <Magnetar install> \
    --ds64 <DS>/DedicatedServer64 --template <template> --work <work> \
    --jobs 4 --port-base 28100 --wrap "<workspace>/notes/parallel-test-work/run-test.sh 4 --"
```

Build the branch into an install folder of your own
(`dotnet build Legacy/Legacy.csproj -c Release -p:Magnetar=<folder>`). Job
`i` uses DS port `port-base + i` and Steam port `port-base + 1000 + i`.
`--cases` takes comma separated globs of case ids. The results are in
`<work>/results.md` and `results.json`, and each case folder keeps its logs.
A full run takes about 1.5 minutes per case and job.

## Running under Proton

`--platform proton-interim` or `proton-legacy` with `--install` pointing at
a Windows package (`MagnetarInterim.exe`, `MagnetarLegacy.exe`, `Libraries`)
and `--prefix` at one Proton prefix per job (comma separated):
servers in one prefix share its wineserver, and stopping one case could take
the others down. The prefixes need, besides a Proton run once:

- `HKLM\SOFTWARE\Classes\Installer\Dependencies\Microsoft.VS.VC_RuntimeMinimumVSU_amd64,v14`,
  which the DS checks for. Wine has the DLLs.
- For MagnetarInterim, the .NET 10 runtime in `C:\Program Files\dotnet`.
- For MagnetarLegacy, .NET Framework 4.8 instead of Wine Mono. Its installer
  fails inside a Proton prefix (1603). It installs with system Wine 9 into a
  fresh prefix made with `WINEDLLOVERRIDES="mscoree,mshtml=" wineboot -i`, an
  `mscoree=native` override, Windows version `winxp64` for
  `dotNetFx40_Full_x86_x64.exe /q` and then `win7` for
  `ndp48-x86-x64-allos-enu.exe /q`. Proton adopts that prefix as `pfx/` of a
  compat data folder without reinstalling Mono.

A display is needed even for console programs: run a headless Weston with
Xwayland (`weston --backend=headless --renderer=noop --xwayland`) and pass
its X display with `--display`. `STEAM_COMPAT_PROTON` stays out of the
environment, or Magnetar reports `Platform: Proton` and stops (SE1-0186).
Signals don't apply to Windows, so those cases only run on Linux.
