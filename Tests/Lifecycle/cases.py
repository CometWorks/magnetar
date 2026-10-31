"""The lifecycle matrix: one case per command arriving in one server state.

Steps run in order after the server starts:
  ("ready", n)        wait for the n-th world load seen by the probe
  ("loading",)        wait until the DS is loading the world, before the first "ready"
  ("probe", line...)  send probe commands (Probe/LifecycleProbe.cs lists them)
  ("signal", "TERM")  send a POSIX signal to the Magnetar process
  ("sleep", seconds)
The first step that sends something marks the time the world must be saved after.

Expectations:
  exit       exit code the server ends with; None = still running at the end; "any"
  restarts   world loads after the first one, counted when the case settles
  saved      whether the world file was written after the command; None = not checked
  terminating  ServerControl.Terminating kinds the plugins saw, in order; None = not checked
  reload     start the server again afterwards and expect the world to load
  replies    text each of which some reply to the probe's chat commands contains
  essentials_src  the case needs an Essentials newer than MagnetarHub's (run.py --essentials-src)
  jit_profile  the runtime's JIT profile must land in the data folder and not change in the DS folder
"""

from dataclasses import dataclass, field

ALL = ("linux", "proton-interim", "proton-legacy")
LINUX = ("linux",)


@dataclass
class Case:
    id: str
    state: str
    command: str
    steps: list
    exit: object = None
    restarts: int = 0
    saved: object = None
    terminating: object = None
    reload: bool = False
    replies: list = field(default_factory=list)
    essentials_src: bool = False
    jit_profile: bool = False
    platforms: tuple = ALL
    essentials: str = "none"
    auto_restart: bool = False
    settle: int = 15
    notes: str = ""


READY = ("ready", 1)
# A save that takes 10 s, so the next command arrives while it runs.
SAVING = [("probe", "slowsave 10", "worker sdk SaveWorld"), ("sleep", 2)]
RESTARTING = [("probe", "slowsave 10", "worker sdk SaveAndRestart"), ("sleep", 2)]
STOPPING = [("probe", "slowsave 10", "worker sdk SaveAndQuit"), ("sleep", 2)]


def _running():
    return [
        Case("run-save-chat", "running", "Magnetar `!save`", [READY, ("probe", "chat !save")], saved=True),
        Case("run-save-ess", "running", "Essentials `!ess save`", [READY, ("probe", "chat !ess save")], saved=True),
        Case(
            "run-save-sdk", "running", "PluginSdk `SaveWorld`", [READY, ("probe", "worker sdk SaveWorld")], saved=True
        ),
        Case(
            "run-restart-chat",
            "running",
            "Magnetar `!restart`",
            [READY, ("probe", "chat !restart")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "run-restart-ess",
            "running",
            "Essentials `!ess restart` (console: immediate)",
            [READY, ("probe", "chat !ess restart")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "run-restart-ess-auto",
            "running",
            "Essentials timed auto command `Restart` step",
            [READY],
            restarts=2,
            saved=True,
            terminating=["Restart", "Restart"],
            essentials="restart",
        ),
        Case(
            "run-restart-ds-auto",
            "running",
            "DS timed auto-restart (`AutoRestatTimeInMin` 1)",
            [READY],
            restarts=2,
            saved=True,
            terminating=["Restart", "Restart"],
            auto_restart=True,
        ),
        Case(
            "run-restart-sdk",
            "running",
            "PluginSdk `SaveAndRestart`, game thread",
            [READY, ("probe", "sdk SaveAndRestart")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "run-restart-sdk-worker",
            "running",
            "PluginSdk `SaveAndRestart`, worker thread",
            [READY, ("probe", "worker sdk SaveAndRestart")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "run-restart-nosave",
            "running",
            "PluginSdk `RestartWithoutSaving`",
            [READY, ("probe", "worker sdk RestartWithoutSaving")],
            restarts=1,
            saved=False,
            terminating=["Restart"],
        ),
        Case(
            "run-stop-chat",
            "running",
            "Magnetar `!stop`",
            [READY, ("probe", "chat !stop")],
            exit=0,
            saved=True,
            terminating=["Shutdown"],
        ),
        Case(
            "run-quit-chat",
            "running",
            "Magnetar `!quit`",
            [READY, ("probe", "chat !quit")],
            exit=0,
            saved=False,
            terminating=["Shutdown"],
        ),
        Case(
            "run-stop-ess",
            "running",
            "Essentials `!ess stop` (console: immediate)",
            [READY, ("probe", "chat !ess stop")],
            exit=0,
            saved=True,
            terminating=["Shutdown"],
        ),
        Case(
            "run-stop-ess-auto",
            "running",
            "Essentials timed auto command `Stop` step",
            [READY],
            exit=0,
            saved=True,
            terminating=["Shutdown"],
            essentials="stop",
        ),
        Case(
            "run-stop-sdk",
            "running",
            "PluginSdk `SaveAndQuit`",
            [READY, ("probe", "worker sdk SaveAndQuit")],
            exit=0,
            saved=True,
            terminating=["Shutdown"],
        ),
        Case(
            "run-quit-sdk",
            "running",
            "PluginSdk `QuitWithoutSaving`",
            [READY, ("probe", "worker sdk QuitWithoutSaving")],
            exit=0,
            saved=False,
            terminating=["Shutdown"],
        ),
        Case(
            "run-exit-ds",
            "running",
            "DS's own exit (`MySandboxGame.ExitThreadSafe`, also console Ctrl+C)",
            [READY, ("probe", "exit")],
            exit=0,
            saved=True,
            terminating=["Shutdown"],
        ),
        Case(
            "run-update-ds",
            "running",
            "DS's own stop for Keen's auto-update",
            [READY, ("probe", "update")],
            exit=0,
            saved=True,
            terminating=["Shutdown"],
            notes="Without a lifecycle provider it exits, so an updater can run",
        ),
        Case(
            "run-sigterm",
            "running",
            "SIGTERM",
            [READY, ("signal", "TERM")],
            exit=0,
            saved=True,
            terminating=[],
            platforms=LINUX,
        ),
        Case(
            "run-sigint",
            "running",
            "SIGINT (Ctrl+C in a Linux terminal)",
            [READY, ("signal", "INT")],
            exit=0,
            saved=True,
            terminating=[],
            platforms=LINUX,
        ),
        Case(
            "run-sighup",
            "running",
            "SIGHUP (save and reload config)",
            [READY, ("signal", "HUP")],
            saved=True,
            platforms=LINUX,
        ),
        Case(
            "run-environment-exit",
            "running",
            "`Environment.Exit` from a plugin, the only exit where .NET writes the JIT profile on Linux",
            [READY, ("probe", "worker envexit 3")],
            exit="any",
            jit_profile=True,
            reload=True,
            notes="Exits 139 after the exit handlers saw code 3: native teardown crashes on a loaded session",
            platforms=LINUX,
        ),
        Case(
            "run-kill",
            "running",
            "SIGKILL / process killed",
            [READY, ("signal", "KILL")],
            exit="killed",
            saved=False,
            reload=True,
        ),
        Case(
            "run-crash",
            "running",
            "Unhandled exception on a worker thread (crash)",
            [READY, ("probe", "crash")],
            exit="nonzero",
            saved=False,
            reload=True,
        ),
        Case(
            "run-hang",
            "running",
            "Game thread hangs for 30 s",
            [READY, ("probe", "hang 30"), ("sleep", 30)],
            saved=False,
        ),
    ]


def _starting():
    return [
        Case(
            "start-sigterm",
            "starting",
            "SIGTERM 2 s after launch, before the DS loads anything",
            [("sleep", 2), ("signal", "TERM")],
            exit="any",
            platforms=LINUX,
            settle=5,
            notes="Before Magnetar installs its handlers the default disposition applies",
        ),
        Case(
            "load-sigterm",
            "loading world",
            "SIGTERM while the DS loads the world",
            [("loading",), ("signal", "TERM")],
            exit=0,
            saved=True,
            reload=True,
            platforms=LINUX,
            notes="The save waits for the game thread, so it saves the loaded world",
        ),
        Case(
            "load-kill",
            "loading world",
            "SIGKILL while the DS loads the world",
            [("loading",), ("signal", "KILL")],
            exit="killed",
            reload=True,
        ),
    ]


def _saving():
    return [
        Case(
            "save-save",
            "saving",
            "PluginSdk `SaveWorld` while a save runs",
            [READY, *SAVING, ("probe", "worker sdk SaveWorld"), ("sleep", 10)],
            saved=True,
        ),
        Case(
            "save-restart",
            "saving",
            "PluginSdk `SaveAndRestart` while a save runs",
            [READY, *SAVING, ("probe", "worker sdk SaveAndRestart")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "save-restart-chat",
            "saving",
            "Magnetar `!restart` while a save runs",
            [READY, *SAVING, ("probe", "chat !restart")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "save-stop-chat",
            "saving",
            "Magnetar `!stop` while a save runs",
            [READY, *SAVING, ("probe", "chat !stop")],
            exit=0,
            saved=True,
            terminating=["Shutdown"],
        ),
        Case(
            "save-sigterm",
            "saving",
            "SIGTERM while a save runs",
            [READY, *SAVING, ("signal", "TERM")],
            exit=0,
            saved=True,
            platforms=LINUX,
        ),
        Case(
            "save-quit",
            "saving",
            "Magnetar `!quit` while a save runs",
            [READY, *SAVING, ("probe", "chat !quit")],
            exit=0,
            reload=True,
            terminating=["Shutdown"],
            notes="Quits without waiting for the save in progress; the world must still load",
        ),
        Case(
            "save-kill",
            "saving",
            "SIGKILL while a save runs",
            [READY, *SAVING, ("signal", "KILL")],
            exit="killed",
            reload=True,
        ),
    ]


def _restarting():
    return [
        Case(
            "restart-restart",
            "restarting",
            "PluginSdk `SaveAndRestart` while a restart saves",
            [READY, *RESTARTING, ("probe", "worker sdk SaveAndRestart")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "restart-stop-chat",
            "restarting",
            "Magnetar `!stop` while a restart saves",
            [READY, *RESTARTING, ("probe", "chat !stop")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
            notes="The first termination wins",
        ),
        Case(
            "restart-stop-sdk",
            "restarting",
            "PluginSdk `SaveAndQuit` while a restart saves",
            [READY, *RESTARTING, ("probe", "worker sdk SaveAndQuit")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "restart-sigterm",
            "restarting",
            "SIGTERM while a restart saves",
            [READY, *RESTARTING, ("signal", "TERM")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
            platforms=LINUX,
            notes="The first termination wins; the signal is consumed",
        ),
        Case(
            "restart-kill",
            "restarting",
            "SIGKILL while a restart saves",
            [READY, *RESTARTING, ("signal", "KILL")],
            exit="killed",
            reload=True,
        ),
        Case(
            "restart-sigterm-loading",
            "restarting",
            "SIGTERM while the restarted server loads the world",
            [READY, ("probe", "worker sdk SaveAndRestart"), ("sleep", 3), ("loading",), ("signal", "TERM")],
            exit=0,
            terminating=["Restart"],
            platforms=LINUX,
        ),
    ]


def _stopping():
    return [
        Case(
            "stop-restart",
            "stopping",
            "PluginSdk `SaveAndRestart` while a stop saves",
            [READY, *STOPPING, ("probe", "worker sdk SaveAndRestart")],
            exit=0,
            saved=True,
            terminating=["Shutdown"],
        ),
        Case(
            "stop-sigterm",
            "stopping",
            "SIGTERM while a stop saves",
            [READY, *STOPPING, ("signal", "TERM")],
            exit=0,
            saved=True,
            platforms=LINUX,
        ),
        Case(
            "stop-kill",
            "stopping",
            "SIGKILL while a stop saves",
            [READY, *STOPPING, ("signal", "KILL")],
            exit="killed",
            reload=True,
        ),
    ]


def _double():
    return [
        Case(
            "double-restart",
            "running",
            "Two `SaveAndRestart` calls at once",
            [READY, ("probe", "worker sdk SaveAndRestart", "worker sdk SaveAndRestart")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "double-restart-chat",
            "running",
            "Magnetar `!restart` and Essentials `!ess restart` at once",
            [READY, ("probe", "chat !restart", "chat !ess restart")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
        Case(
            "double-sigterm",
            "running",
            "SIGTERM twice",
            [READY, ("signal", "TERM"), ("sleep", 0.3), ("signal", "TERM")],
            exit=0,
            saved=True,
            platforms=LINUX,
        ),
    ]


def _hung():
    return [
        Case(
            "hang-sigterm",
            "hung",
            "SIGTERM while the game thread hangs for 20 s",
            [READY, ("probe", "hang 20"), ("sleep", 2), ("signal", "TERM")],
            exit=0,
            saved=True,
            platforms=LINUX,
            notes="The save waits for the game thread",
        ),
        Case(
            "hang-restart",
            "hung",
            "PluginSdk `SaveAndRestart` while the game thread hangs for 20 s",
            [READY, ("probe", "worker sdk SaveAndRestart", "hang 20")],
            restarts=1,
            saved=True,
            terminating=["Restart"],
        ),
    ]


def _command_lines():
    # How the dispatcher reads a command line (DIS-0017, SE1-0187): a root's default
    # command takes arguments only when it has parameters for them.
    return [
        Case(
            "cmd-restart-help",
            "running",
            "Magnetar `!restart help`",
            [READY, ("probe", "chat !restart help")],
            replies=["Usage: !restart"],
        ),
        Case(
            "cmd-restart-seconds",
            "running",
            "Magnetar `!restart 30` (Magnetar's `!restart` takes no delay)",
            [READY, ("probe", "chat !restart 30")],
            replies=["Unknown command"],
        ),
        Case(
            "cmd-stop-typo",
            "running",
            "Magnetar `!stop` with a stray word",
            [READY, ("probe", "chat !stop nwo")],
            replies=["Unknown command"],
        ),
        # Essentials' top-level short forms (DIS-0017), as the console
        Case(
            "cmd-reload",
            "running",
            "Essentials `!reload`",
            [READY, ("probe", "chat !reload")],
            replies=["Configuration reloaded."],
            essentials_src=True,
        ),
        Case(
            "cmd-fixship",
            "running",
            "Essentials `!fixship` (the console has no grids)",
            [READY, ("probe", "chat !fixship")],
            replies=["Console has no Grids"],
            essentials_src=True,
        ),
        Case(
            "cmd-fixshipmodid",
            "running",
            "Essentials `!fixshipmodid 123` (an argument to a short form)",
            [READY, ("probe", "chat !fixshipmodid 123")],
            replies=["Grid not found"],
            essentials_src=True,
        ),
    ]


CASES = _command_lines() + _running() + _starting() + _saving() + _restarting() + _stopping() + _double() + _hung()
