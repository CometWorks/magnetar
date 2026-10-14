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

Plugins call these through `PluginSdk.ServerControl`. Before a stop or restart
that a command, a plugin or the dedicated server itself asks for, plugins get
the `Terminating` event with the kind `Restart` or `Shutdown`. Signals don't
raise it: they come from outside, mostly from a host shutting down, and a
supervisor should start the server again after one.

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
  exits with code 0. The new process has a new PID. It waits for the old one
  to be gone before it starts up (60 s at most), because the old process keeps
  the patched game assemblies in `<config>\Preloader` open while it exits, and
  the new one's preloader writes them again. MagnetarLegacy starts it
  without passing on its handles: on .NET Framework the dedicated server's game
  socket is inheritable, and a new server holding the old one's socket can't
  bind the game port.

The info log gets `Restarting: <path of the launcher>` before the plugins are
disposed, because disposing them closes the log file. If the restart itself
fails, the error goes to stderr and Magnetar exits with code 1.

## Commands

The columns are where Magnetar runs: on Linux on its own (or under systemd and
the like), on Linux under Quasar, and on Windows as MagnetarInterim or
MagnetarLegacy. Cells marked *(tested)* were run by the lifecycle test suite
(`Tests/Lifecycle`, and `Tests/Lifecycle` in the quasar repository for Quasar);
both Windows launchers were tested under Proton, MagnetarLegacy on .NET
Framework 4.8. The rest comes from reading the code.

### Restart

| Command | Linux | Linux, under Quasar | Windows, MagnetarInterim | Windows, MagnetarLegacy |
|---|---|---|---|---|
| Essentials `Restart` auto command step, `!ess restart` (`SaveAndRestart`) | Restarts in place, same PID *(tested)* | Saves and quits, Quasar starts it again, see [Quasar](#quasar) *(tested)* | New process, the old one exits 0 *(tested)* | New process, the old one exits 0 *(tested)* |
| Magnetar `!restart` (`SaveAndRestart`) | Restarts in place, same PID *(tested)* | Not reached: Quasar.Agent's own `!restart` counts down 10 s, saves, quits and lets Quasar start the server again *(tested)* | New process, the old one exits 0 *(tested)* | New process, the old one exits 0 *(tested)* |
| Quasar's restart (UI, Discord, schedule, maximum uptime, health policy) | n/a | Quasar asks the agent to save and quit, kills the process if it is still running 30 s later, then starts a new one *(UI and health policy tested)* | n/a | n/a |
| The dedicated server's timed auto-restart (`SaveAndRestart`) | Restarts in place, same PID *(tested)* | Saves and quits, Quasar starts it again, see [Quasar](#quasar) *(tested)* | New process, the old one exits 0 *(tested)* | New process, the old one exits 0 *(tested)* |
| `RestartWithoutSaving` (plugin API only) | Like `SaveAndRestart`, without the save *(tested)* | Quits without saving, Quasar starts it again *(tested)* | Like `SaveAndRestart`, without the save *(tested)* | Like `SaveAndRestart`, without the save *(tested)* |

`!ess restart` restarts at once when it comes from the console, when it is
given `now`, or when Essentials has no `OnRestartSequence`. Otherwise it runs
that auto command, which normally counts down and ends in a `Restart` step. In
a cluster, Magnetar's `!restart` asks for a coordinated node restart instead.

The dedicated server's timed auto-restart is on when `AutoRestartEnabled` is
true and `AutoRestatTimeInMin` (spelled that way in
`SpaceEngineers-Dedicated.cfg`) is above 0. The server announces it 10, 5 and
1 minutes ahead and would then just exit. Magnetar replaces that exit with
`PluginSdk.ServerControl.SaveAndRestart`, the same call plugins make, so a
lifecycle provider such as a cluster handles it when one is registered.

### Stop

| Command | Linux | Linux, under Quasar | Windows, both launchers |
|---|---|---|---|
| Magnetar `!stop` (save, then `QuitWithoutSaving`) | Exits 0 *(tested)* | Not reached: Quasar.Agent's own `!stop` tells Quasar to keep the server off, then saves and quits *(tested)* | Exits 0 *(tested)* |
| Magnetar `!quit` (`QuitWithoutSaving`) | Exits 0 without saving *(tested)* | Not reached: Quasar.Agent's own `!quit` tells Quasar to keep the server off, then quits without saving *(tested)* | Exits 0 without saving *(tested)* |
| Essentials `Stop` step, `!ess stop` (`SaveAndQuit`) | Exits 0 *(tested)* | Quasar.Agent tells Quasar to keep the server off *(tested)* | Exits 0 *(tested)* |
| `SaveAndQuit`, `QuitWithoutSaving` from a plugin | Exits 0, saved or not *(tested)* | Quasar.Agent tells Quasar to keep the server off *(tested)* | Exits 0, saved or not *(tested)* |
| SIGTERM, SIGINT (`SaveAndQuit`) | Exits 0 *(tested)* | Quasar sends no signals; one from outside exits 0 and Quasar starts it again *(tested)* | n/a |
| Ctrl+C in the console | Same as SIGINT | Same as SIGINT | Saves and exits 0, through the dedicated server's own exit |
| Closing the console window (`SaveAndQuit`) | n/a | n/a | Saves and exits 0, if that fits in the 5 s Windows allows, see below *(tested in a conhost window)* |
| Windows shutting down | n/a | n/a | Saves and exits 0 like a signal, without `Terminating`, in the same 5 s *(not tested)* |
| The dedicated server's own exit (`MySandboxGame.ExitThreadSafe`): its `/stop`, the Remote API stop, a scenario's stop | Saves and exits 0 *(tested)* | Quasar.Agent tells Quasar to keep the server off *(tested)* | Saves and exits 0 *(tested)* |
| The dedicated server's stop for Keen's auto-update (`AutoUpdateEnabled`) | Saves and exits 0, so an updater can run *(tested)* | Restarts through Quasar like a plugin's restart; Quasar's launch update installs the new build *(tested)* | Saves and exits 0 *(tested)* |

A second stop or restart while one is under way is ignored: the first one
wins, also against a signal *(tested)*. A stop or restart while the game thread
hangs waits for it, then saves *(tested)*. SIGTERM while the world is still
loading saves once the load is done, then exits 0 *(tested)*.

Windows ends a console program 5 s after its window is closed, and there is
no way to ask for more. Magnetar saves first, then disposes the plugins and
exits. A world with a 141 MB `SANDBOX_0_0_0_.sbs` took 3.3 s to save with
MagnetarInterim and 4.4 s with MagnetarLegacy, which Windows then ended at
the 5 s mark with the save already on disk. A bigger world may not make it. A
save cut short while it writes leaves the previous save in place, because the
dedicated server writes the files into the world's `.new` folder first. Only
the last step, copying them over the old files one by one, can leave a mix of
old and new files when it is cut off. Stop a server with a big world some other
way. The same handler runs when Windows shuts down *(not tested)*. Logging off
isn't handled: a server running as a service gets that event whenever any user
logs off. Windows ends the process without a save then.

`SIGHUP` on Linux saves the world and reloads the dedicated server config
without stopping.

## Quasar

Quasar runs its own restarts as a stop followed by a new launch. Quasar.Agent
registers as the PluginSdk lifecycle provider (CometWorks/quasar#218), so a
restart that a plugin or the dedicated server asks for through
`ServerControl` reaches the agent instead of Magnetar. The agent tells Quasar
it is an in-game restart, then saves and quits, quits without saving for
`RestartWithoutSaving`, and Quasar starts the server again: a new process,
restart cause `InGame`, no crash counted *(tested on Linux and, for
MagnetarInterim, under Proton)*. Plugins see `Terminating` with the kind
`Shutdown`, because the server does quit. Keen's auto-update stop takes the
same way when a lifecycle provider is registered, outside a cluster.

An older agent doesn't intercept. Magnetar then restarts itself: in place on
Linux, where the agent's connection drops and Quasar's health policy kills and
relaunches the server 30 s later; as a new process on Windows, which Quasar
takes for a crash and starts a second server next to it.

With its default `RestartOnCrash`, Quasar treats any exit it didn't ask for
and wasn't told about as a crash, whatever the exit code. Quasar.Agent tells it
about every `Terminating(Shutdown)` it didn't ask for itself, so a plugin's
stop, the agent's `!stop` and `!quit` and the dedicated server's own exit keep
the server off. A SIGTERM or SIGINT from outside raises no `Terminating`, and
Quasar starts the server again as crash recovery.

A self-restart gives Quasar no chance to apply plugin updates before the
server starts again; the restart through Quasar does.
