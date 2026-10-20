#!/usr/bin/env python3
"""Kills every process whose command line or environment names the given work folder, e.g. after an interrupted run."""

import os
import signal
import sys
from pathlib import Path

work = str(Path(sys.argv[1]).resolve())
markers = (work, "Z:" + work.replace("/", "\\"))
for pid in filter(str.isdigit, os.listdir("/proc")):
    try:
        cmd = Path(f"/proc/{pid}/cmdline").read_bytes().replace(b"\0", b" ").decode(errors="replace")
        env = Path(f"/proc/{pid}/environ").read_bytes().decode(errors="replace")
    except OSError:
        continue
    # Quasar names its install root only in its environment (QUASAR_INSTALL_DIR).
    if int(pid) != os.getpid() and any(m in cmd or m in env for m in markers) and "cleanup.py" not in cmd:
        print(pid, cmd[:120])
        os.kill(int(pid), signal.SIGKILL)
