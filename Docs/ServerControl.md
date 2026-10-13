# Stopping and restarting the server

Magnetar has one implementation of the server's lifecycle operations,
`ServerControl` in the launcher. Chat commands, plugins, signals and the
dedicated server's own exit path all end in one of four operations:

| Operation | Saves first | Result |
|---|---|---|
| `SaveAndQuit` | Yes | Disposes the plugins and exits with code 0 |
| `QuitWithoutSaving` | No | Disposes the plugins and exits with code 0 |
| `SaveAndRestart` | Yes | Disposes the plugins and restarts Magnetar |
| `RestartWithoutSaving` | No | Disposes the plugins and restarts Magnetar |

Plugins call these through `PluginSdk.ServerControl`. Before a restart, and
before `!quit` or `!stop`, plugins get the `Terminating` event with the kind
`Restart` or `Shutdown`. `SaveAndQuit` doesn't raise it.

## How a restart works

A restart reproduces the original launch: the same arguments, working
directory and environment, captured at the top of `Main`. It runs the launcher
binary itself (`Environment.ProcessPath`), so `MagnetarInterim.dll` is never
started directly. If the `dotnet` command ran Magnetar, the `.dll` is passed to
it again as the first argument.

- **Linux.** Magnetar replaces itself with `execve`. The PID, stdin, stdout,
  stderr and terminal stay the same, which suits systemd and tmux. The pid
  file is written again and a new `info.log` starts.
- **Windows (both launchers).** Magnetar starts a new process and the old one
  exits with code 0. The new process has a new PID.

The info log gets `Restarting: <path of the launcher>` before the plugins are
disposed, because disposing them closes the log file. If the restart itself
fails, the error goes to stderr and Magnetar exits with code 1.

## Commands

The columns are where Magnetar runs: on Linux on its own (or under systemd and
the like), on Linux under Quasar, and on Windows as MagnetarInterim or
MagnetarLegacy. Cells marked *(tested)* were run in a test; MagnetarInterim on
Windows was tested under Proton. The rest comes from reading the code.

### Restart

| Command | Linux | Linux, under Quasar | Windows, MagnetarInterim | Windows, MagnetarLegacy |
|---|---|---|---|---|
| Essentials `Restart` auto command step, `!ess restart` (`SaveAndRestart`) | Restarts in place, same PID *(tested)* | Restarts in place, then Quasar restarts it again, see [Quasar](#quasar) *(tested)* | New process, the old one exits 0 *(tested)* | New process, the old one exits 0 |
| Magnetar `!restart` (`SaveAndRestart`) | Restarts in place, same PID | Not reached: Quasar.Agent's own `!restart` saves, quits and lets Quasar start the server again | New process, the old one exits 0 | New process, the old one exits 0 |
| Quasar's restart (UI, Discord, schedule, maximum uptime, health policy) | n/a | Quasar asks the agent to save and quit, kills the process if it is still running 30 s later, then starts a new one *(health policy tested)* | n/a | n/a |
| The dedicated server's timed auto-restart (`SaveAndRestart`) | Restarts in place, same PID *(tested)* | Restarts in place, then Quasar restarts it again, see [Quasar](#quasar) | New process, the old one exits 0 *(tested)* | New process, the old one exits 0 |
| `RestartWithoutSaving` (plugin API only) | Like `SaveAndRestart`, without the save | Like `SaveAndRestart`, without the save | Like `SaveAndRestart`, without the save | Like `SaveAndRestart`, without the save |

`!ess restart` restarts at once when it comes from the console, when it is
given `now`, or when Essentials has no `OnRestartSequence`. Otherwise it runs
that auto command, which normally counts down and ends in a `Restart` step. In
a cluster, Magnetar's `!restart` asks for a coordinated node restart instead.

The dedicated server's timed auto-restart is on when `AutoRestartEnabled` is
true and `AutoRestatTimeInMin` (spelled that way in
`SpaceEngineers-Dedicated.cfg`) is above 0. The server announces it 10, 5 and
1 minutes ahead, then exits through `MySandboxGame.ExitThreadSafe`, which
Magnetar turns into a restart.

### Stop

| Command | Linux | Linux, under Quasar | Windows, both launchers |
|---|---|---|---|
| Magnetar `!stop` (save, then `QuitWithoutSaving`) | Exits 0 | Not reached: Quasar.Agent's own `!stop` tells Quasar to keep the server off, then saves and quits | Exits 0 |
| Magnetar `!quit` (`QuitWithoutSaving`) | Exits 0 without saving | Not reached: Quasar.Agent's own `!quit` tells Quasar to keep the server off, then quits without saving | Exits 0 without saving |
| Essentials `Stop` step, `!ess stop` (`SaveAndQuit`) | Exits 0 | Exits 0, and Quasar starts it again as crash recovery | Exits 0 |
| SIGTERM, SIGINT (`SaveAndQuit`) | Exits 0 *(SIGTERM tested)* | Quasar sends no signals; one from outside exits 0 and Quasar starts it again | n/a |
| Ctrl+C in the console | Same as SIGINT | Same as SIGINT | Saves and exits 0 |
| The dedicated server's own exit (`MySandboxGame.ExitThreadSafe`) other than its timed auto-restart, such as its stop for Keen's auto-update | Saves and exits 0 | Exits 0, and Quasar starts it again as crash recovery | Saves and exits 0 |

`SIGHUP` on Linux saves the world and reloads the dedicated server config
without stopping.

## Quasar

Quasar runs its own restarts as a stop followed by a new launch. It doesn't
know about a restart that Magnetar does itself, such as Essentials' `Restart`
step:

- **Linux.** Quasar.Agent's connection drops during the in-place restart.
  With health monitoring on (the default outside development) Quasar then
  marks the server unhealthy, kills it 30 s later and starts it again. The
  world was saved before the restart, but anything that happens in those 30 s
  is lost, and the world loads twice.
- **Windows.** The old process exits 0. Quasar takes that for a crash and
  starts a second server next to the one Magnetar started; it doesn't adopt
  the new PID.

With its default `RestartOnCrash`, Quasar also treats any exit it didn't ask
for as a crash, whatever the exit code. So it starts the server again after the
Essentials `Stop` step or the dedicated server's own exit.

The dedicated server's timed auto-restart is a self-restart too, and Quasar
config profiles can turn it on. On servers that Quasar runs, use Quasar's own
restart schedule or maximum uptime instead of it or of Essentials' `Restart`
step until Quasar handles this. A self-restart also gives Quasar no chance to
apply plugin updates before the server starts again.
