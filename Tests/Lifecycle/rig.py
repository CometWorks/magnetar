"""One isolated Magnetar server for the lifecycle suite: set up, start, drive, observe.

Linux runs MagnetarInterim.bin directly. Proton runs the Windows launchers
(MagnetarInterim.exe or MagnetarLegacy.exe) in a prefix of the caller's own.
See README.md for the template folder and the prefix.
"""

import os
import re
import shutil
import signal
import subprocess
import threading
import time
from dataclasses import dataclass, field
from pathlib import Path

HERE = Path(__file__).resolve().parent
PROBE = HERE / "Probe"
WORLD = "LifecycleTest"
ESSENTIALS_ID = "DF09925B-6D53-41F2-899C-CA6214B5DD6F"
# An Essentials working copy to compile as a DevFolder instead of the MagnetarHub
# version (run.py --essentials-src), for cases that need an unreleased Essentials.
ESSENTIALS_SRC = None
LAUNCHERS = ("magnetarinterim.bin", "magnetarinterim.exe", "magnetarlegacy.exe")


@dataclass
class Platform:
    """Where and how Magnetar runs."""

    name: str  # "linux", "proton-interim" or "proton-legacy"
    install: Path  # Magnetar install folder
    ds64: Path  # DedicatedServer64 folder
    wrap: list = field(default_factory=list)  # command prefix, e.g. run-test.sh 4 --
    proton: Path | None = None  # Proton folder
    prefix: Path | None = None  # STEAM_COMPAT_DATA_PATH
    display: str = ":1"

    @property
    def windows(self):
        return self.name.startswith("proton")

    @property
    def launcher(self):
        if self.name == "linux":
            return "MagnetarInterim.bin"
        return "MagnetarLegacy.exe" if self.name == "proton-legacy" else "MagnetarInterim.exe"

    def path(self, p):
        """The path as the server sees it."""
        p = str(Path(p).resolve())
        return "Z:" + p.replace("/", "\\") if self.windows else p


def _cmdline(pid):
    try:
        raw = Path(f"/proc/{pid}/cmdline").read_bytes()
    except OSError:
        return []
    return [a.decode(errors="replace") for a in raw.split(b"\0") if a]


def _parent(pid):
    try:
        return int(Path(f"/proc/{pid}/stat").read_text().rsplit(")", 1)[1].split()[1])
    except (OSError, IndexError, ValueError):
        return 0


class Server:
    """A Magnetar server with its own config folder, DS data folder and ports."""

    def __init__(self, root, platform, template, port, essentials="none", auto_restart=False):
        self.root = Path(root)
        self.platform = platform
        self.template = Path(template)
        self.port = port
        self.essentials = essentials  # "none", "restart" or "stop": an auto command every minute
        self.auto_restart = auto_restart  # the DS timed auto-restart, 1 minute
        self.config = self.root / "magnetar"
        self.data = self.root / "data"
        self.world = self.data / "Saves" / WORLD
        self.proc = None
        self.exit_code = None
        self.samples = []  # (time, [pids]) whenever the process set changes
        self.cmdlines = {}  # pid: command line of every Magnetar process seen
        self._watch = None
        self._stop_watch = threading.Event()

    # --- setup -------------------------------------------------------------

    def setup(self):
        if self.root.exists():
            shutil.rmtree(self.root)
        shutil.copytree(self.template, self.root, symlinks=True)
        self._write_ds_config()
        self._write_essentials_config()
        self._register_plugins()

    def _write_ds_config(self):
        cfg = self.data / "SpaceEngineers-Dedicated.cfg"
        text = cfg.read_text(encoding="utf-8-sig")
        values = {
            "IP": "127.0.0.1",
            "ServerPort": str(self.port),
            "SteamPort": str(self.port + 1000),
            "IgnoreLastSession": "true",
            "RemoteApiEnabled": "false",
            "AutoRestartEnabled": "true" if self.auto_restart else "false",
            "AutoRestatTimeInMin": "1" if self.auto_restart else "0",
            "LoadWorld": self.platform.path(self.world),
        }
        for key, value in values.items():
            text, n = re.subn(rf"<{key}>[^<]*</{key}>|<{key} />", lambda _: f"<{key}>{value}</{key}>", text)
            if n == 0:
                text = text.replace("</MyConfigDedicated>", f"<{key}>{value}</{key}></MyConfigDedicated>")
        cfg.write_text(text, encoding="utf-8")

    def _write_essentials_config(self):
        if self.essentials == "none":
            return
        action = {"restart": "Restart", "stop": "Stop"}[self.essentials]
        (self.data / "Essentials.cfg").write_text(
            f"""<?xml version="1.0" encoding="utf-8"?>
<PluginConfig>
  <AutoCommands>
    <AutoCommand>
      <Name>lifecycle-{self.essentials}</Name>
      <Trigger>Timed</Trigger>
      <Interval>00:01:00</Interval>
      <Steps>
        <CommandStep>
          <Delay>00:00:00</Delay>
          <Action>{action}</Action>
        </CommandStep>
      </Steps>
    </AutoCommand>
  </AutoCommands>
</PluginConfig>
""",
            encoding="utf-8",
        )

    def _register_plugins(self):
        # Under Proton there is no git, so Magnetar compiles every .cs file of
        # the DevFolder: give it a copy that holds nothing else.
        probe = self.root / "probe"
        shutil.copytree(PROBE, probe, ignore=shutil.ignore_patterns("bin", "obj"))
        local = (
            "<LocalPlugin><Name>lifecycle-probe</Name>"
            f"<Folder>{self.platform.path(probe)}</Folder><File>LifecycleProbe.xml</File>"
            "<Enabled>true</Enabled></LocalPlugin>"
        )
        essentials_hub = f"<GitHubPluginConfig><Id>{ESSENTIALS_ID}</Id></GitHubPluginConfig>"
        essentials_dev = ""
        if ESSENTIALS_SRC:
            essentials = self.root / "essentials"
            shutil.copytree(ESSENTIALS_SRC, essentials, ignore=shutil.ignore_patterns("bin", "obj", ".git*", "*.user"))
            local += (
                "<LocalPlugin><Name>essentials</Name>"
                f"<Folder>{self.platform.path(essentials)}</Folder><File>Essentials.xml</File>"
                "<Enabled>true</Enabled></LocalPlugin>"
            )
            essentials_hub = ""
            essentials_dev = f"<LocalFolderConfig><Id>{ESSENTIALS_ID}</Id><DebugBuild>false</DebugBuild></LocalFolderConfig>"
        sources = self.config / "Sources" / "sources.xml"
        local = f"<LocalPluginSources>{local}</LocalPluginSources>"
        text = re.sub(
            r"<LocalPluginSources\s*/>|<LocalPluginSources>.*?</LocalPluginSources>",
            lambda _: local,
            sources.read_text(encoding="utf-8-sig"),
            flags=re.S,
        )
        sources.write_text(text, encoding="utf-8")
        profiles = self.config / "Profiles"
        profiles.mkdir(exist_ok=True)
        (profiles / "Current.xml").write_text(
            '<?xml version="1.0" encoding="utf-8"?>\n'
            '<Profile xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" '
            'xmlns:xsd="http://www.w3.org/2001/XMLSchema"><Name>Current</Name>'
            f"<GitHub>{essentials_hub}</GitHub>"
            "<DevFolder><LocalFolderConfig><Id>lifecycle-probe</Id><DebugBuild>false</DebugBuild>"
            f"</LocalFolderConfig>{essentials_dev}</DevFolder><Local /><Mods /></Profile>",
            encoding="utf-8",
        )

    # --- process -----------------------------------------------------------

    def args(self):
        p = self.platform
        return [
            "-multiInstance",
            "-stableLogs",
            "-consent",
            "deny",
            "-config",
            p.path(self.config),
            "-ds64",
            p.path(p.ds64),
            "-path",
            p.path(self.data),
        ]

    def start(self):
        p = self.platform
        log = open(self.root / "run.log", "ab")
        if p.windows:
            bat = self.root / "run.bat"
            bat.write_text(f"@echo off\r\n%*\r\necho exit %ERRORLEVEL% >> {p.path(self.root / 'exit.log')}\r\n")
            env = dict(
                os.environ,
                DISPLAY=p.display,
                STEAM_COMPAT_DATA_PATH=str(p.prefix),
                STEAM_COMPAT_CLIENT_INSTALL_PATH=str(Path.home() / ".steam/steam"),
                SteamAppId="298740",
                SteamGameId="298740",
            )
            env.pop("WAYLAND_DISPLAY", None)
            # STEAM_COMPAT_PROTON makes Magnetar report Platform: Proton and stop (SE1-0186).
            env.pop("STEAM_COMPAT_PROTON", None)
            # Proton kills its caller's process group, so give it its own session and wait on it.
            cmd = [
                "setsid",
                "-w",
                str(p.proton / "proton"),
                "run",
                p.path(bat),
                p.path(p.install / p.launcher),
                *self.args(),
            ]
        else:
            env = dict(os.environ)
            cmd = [str(p.install / p.launcher), *self.args()]
        # A run started under nohup would pass SIGHUP on as ignored, and the server keeps that.
        self.proc = subprocess.Popen(
            [*p.wrap, *cmd],
            cwd=self.root,
            stdin=subprocess.DEVNULL,
            stdout=log,
            stderr=subprocess.STDOUT,
            env=env,
            start_new_session=True,
            preexec_fn=lambda: signal.signal(signal.SIGHUP, signal.SIG_DFL),
        )
        self._stop_watch.clear()
        self._watch = threading.Thread(target=self._sample, daemon=True)
        self._watch.start()

    def pids(self):
        """Magnetar processes of this server, by the config folder on their command line."""
        marker = self.platform.path(self.config)
        found = []
        for entry in os.listdir("/proc"):
            if not entry.isdigit():
                continue
            argv = _cmdline(entry)
            if not argv or not any(marker in a for a in argv):
                continue
            exe = re.split(r"[\\/]", argv[0])[-1].lower()
            if exe in LAUNCHERS:
                found.append(int(entry))
                self.cmdlines.setdefault(int(entry), f"ppid={_parent(entry)} " + " ".join(argv)[:400])
        if not self.platform.windows:
            # A child Magnetar forks for a helper (git, the compiler) still shows Magnetar's
            # command line until it execs. A second Linux server is never a child of the first.
            found = [pid for pid in found if _parent(pid) not in found]
        return sorted(found)

    def _sample(self):
        last = None
        while not self._stop_watch.is_set():
            pids = self.pids()
            if pids != last:
                self.samples.append((time.time(), pids))
                last = pids
            if self.proc.poll() is not None and not pids:
                self.exit_code = self.proc.returncode
                self.samples.append((time.time(), []))
                return
            time.sleep(0.25)

    def alive(self):
        # Until the first Magnetar process shows up, the launch (RAM guard, Proton) counts as alive.
        if self.pids():
            return True
        return self.proc is not None and self.proc.poll() is None and not any(p for _, p in self.samples)

    def max_concurrent(self):
        return max((len(p) for _, p in self.samples), default=0)

    def overlap_seconds(self):
        """How long more than one Magnetar process of this server ran at once."""
        total, since = 0.0, None
        for t, pids in self.samples:
            if len(pids) > 1 and since is None:
                since = t
            elif len(pids) <= 1 and since is not None:
                total, since = total + t - since, None
        return total + (time.time() - since if since else 0)

    def distinct_pids(self):
        seen = []
        for _, pids in self.samples:
            seen += [p for p in pids if p not in seen]
        return seen

    def signal(self, name):
        for pid in self.pids():
            os.kill(pid, getattr(signal, "SIG" + name))

    def kill(self):
        for pid in self.pids():
            try:
                os.kill(pid, signal.SIGKILL)
            except ProcessLookupError:
                pass

    def stop(self, timeout=90):
        """Ends the server whatever its state: a graceful stop, then a kill."""
        if self.alive():
            if self.platform.windows:
                self.probe("sdk SaveAndQuit")
            else:
                self.signal("TERM")
            self.wait(lambda: not self.alive(), timeout)
        self.kill()
        if self.proc:
            try:
                self.proc.wait(30)
            except subprocess.TimeoutExpired:
                os.killpg(self.proc.pid, signal.SIGKILL)
                self.proc.wait(10)
        if self._watch:
            self._watch.join(5)
        self._stop_watch.set()

    # --- probe and logs ----------------------------------------------------

    def probe(self, *lines):
        tmp = self.data / "probe-cmd.tmp"
        tmp.write_text("\n".join(lines) + "\n", encoding="utf-8")
        tmp.rename(self.data / "probe-cmd.txt")

    def events(self):
        """probe.log as (time, pid, message) tuples."""
        out = []
        try:
            text = (self.data / "probe.log").read_text(encoding="utf-8", errors="replace")
        except OSError:
            return out
        for line in text.splitlines():
            m = re.match(r"(\S+) pid=(\d+) (.*)", line)
            if m:
                out.append((m.group(1), int(m.group(2)), m.group(3)))
        return out

    def ready_runs(self):
        return [(pid, msg.split("=", 1)[1]) for _, pid, msg in self.events() if msg.startswith("ready run=")]

    def ds_log_started(self):
        """When the DS log was started, from its first line; 0 without one."""
        m = re.match(r"(\d{4}-\d\d-\d\d \d\d:\d\d:\d\d)", self.ds_log())
        return time.mktime(time.strptime(m.group(1), "%Y-%m-%d %H:%M:%S")) if m else 0

    def ds_log(self):
        try:
            return (self.data / "SpaceEngineersDedicated.log").read_text(errors="replace")
        except OSError:
            return ""

    def info_logs(self):
        return "".join(p.read_text(errors="replace") for p in sorted(self.config.glob("info*.log")))

    def windows_exit_codes(self):
        """Exit codes run.bat recorded for the first process and the probe for later ones."""
        codes = []
        try:
            codes += [int(x) for x in re.findall(r"exit (-?\d+)", (self.root / "exit.log").read_text())]
        except OSError:
            pass
        return codes

    def world_mtime(self):
        # Only a session save writes these; loading a world may rewrite the .sbsB5 cache.
        try:
            return max((self.world / f).stat().st_mtime_ns for f in ("Sandbox.sbc", "SANDBOX_0_0_0_.sbs"))
        except OSError:
            return 0

    @staticmethod
    def wait(predicate, timeout, interval=0.5):
        end = time.time() + timeout
        while time.time() < end:
            if predicate():
                return True
            time.sleep(interval)
        return predicate()
