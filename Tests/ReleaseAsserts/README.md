# Release assert test

Covers `Legacy/Patch/Patch_ReleaseAsserts.cs` (SE-1800).

The game's release asserts (`MyDebug.FailRelease`, `MyDebug.AssertRelease`)
go to a listener in `VRage.Platform.Windows`. It logs `ASSERT: ...` and then
shows a WinForms Abort/Retry/Ignore box. On .NET 10 `System.Windows.Forms`
does not load, so the listener throws and the server dies. Magnetar now swaps
in a listener that only logs, which is what pressing Ignore does. On Linux
LinuxCompat's preloader fixes the game's listener too, so each fix covers the
server on its own.

## Pieces

- `Probe/` is a test plugin, loaded as a DevFolder plugin. Once the world
  runs it calls `MyDebug.FailRelease("ReleaseAssertProbe assert")` on the game
  thread and logs `ReleaseAssertProbe: firing` and `ReleaseAssertProbe:
  returned` around the call. On a client it fires at the main menu instead.
- `run.py` starts one isolated server per case on a copy of the DS's Empty
  World with two grids that share their grid and block entity ids (the
  reported save had two ships like that). A case passes when the server drops
  the duplicate, logs the assert, comes back from it and keeps running.

## Running

```bash
Tests/ReleaseAsserts/run.py --work <work> \
    --ds <steamapps>/common/SpaceEngineersDedicatedServer \
    --ds-cfg <a SpaceEngineers-Dedicated.cfg> \
    --dotnet-compat <dotnet-compat working copy> --port-base 28100 \
    --wrap "<workspace>/testing/parallel-test-work/run-test.sh 4 --" \
    fixed=<Magnetar install>,hub \
    lc-only=<Magnetar main install>,<linux-compat working copy>
```

A case is `label=<Magnetar install>,<linux-compat>`, where `<linux-compat>`
is a working copy loaded as a DevFolder or `hub` for MagnetarHub's build. The
linux-compat DevFolder needs `linux-dependencies/dist/se1-dependencies.tar.gz`
and `linux-native-wrappers/build` two levels up, as its `ServerPlugin.xml`
says. Case `i` uses DS port `port-base + i`. `--github-cache` takes a
Magnetar `GitHub/` folder from an earlier run, so the cases don't download and
compile the hub plugins again. Results go to `<work>/results.json`; the exit
code is 0 when every case passed. A run takes about a minute.
