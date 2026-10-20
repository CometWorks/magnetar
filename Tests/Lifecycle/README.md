# Lifecycle test suite

Starts isolated Magnetar servers and sends each stop and restart command in
each server state: running, starting, loading the world, saving, restarting,
stopping, hung. Then it checks the outcome: whether the server restarted or
exited, the exit code, whether the world was saved, that no second Magnetar
process ran, whether plugins got the `Terminating` event, and that the plugins
were disposed without an error and while the game thread didn't update them. The matrix is
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
  initialization that long, once. The probe's `Dispose` takes a second and
  logs how many updates the game thread ran meanwhile.
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
- For Proton, `magnetar/GitHub/` from a run of the same launcher under Proton,
  because the plugin cache is per runtime, and optionally `NuGet/` from such a
  run to skip the NuGet restore.

## Running on Linux

```bash
Tests/Lifecycle/run.py --platform linux --install <Magnetar install> \
    --ds64 <DS>/DedicatedServer64 --template <template> --work <work> \
    --jobs 4 --port-base 28100 --wrap "<workspace>/testing/parallel-test-work/run-test.sh 4 --"
```

Build the branch into an install folder of your own
(`dotnet build Legacy/Legacy.csproj -c Release -p:Magnetar=<folder>`). Job
`i` uses DS port `port-base + i` and Steam port `port-base + 1000 + i`.
`--cases` takes comma separated globs of case ids. `--essentials-src <Essentials
working copy>` compiles Essentials from there as a DevFolder instead of taking
MagnetarHub's; the cases that need an unreleased Essentials (`cmd-reload`,
`cmd-fixship`, `cmd-fixshipmodid`) run only with it. The results are in
`<work>/results.md` and `results.json`, and each case folder keeps its logs.
A full run takes about 1.5 minutes per case and job.

## Running under Proton

`--platform proton-interim` or `proton-legacy` with `--install` pointing at
a Windows package (`MagnetarInterim.exe`, `MagnetarLegacy.exe`, `Libraries`)
and `--prefix` at one Proton prefix per job (comma separated):
servers in one prefix share its wineserver, and stopping one case could take
the others down. Set the prefixes up as "Running under Proton or Wine" in
`Docs/Usage.md` describes: a runtime for the launcher, the VC++ registry key
and a display. Run a headless Weston with Xwayland and pass its X display with
`--display`. `STEAM_COMPAT_PROTON` stays out of the environment unless
`--steam-compat-proton` is given; Magnetar reports `Platform: Proton` either
way. Signals don't apply to Windows, so those cases only run on Linux.
