#!/usr/bin/env python3
"""Runs the lifecycle matrix (cases.py) against one Magnetar build on one platform.

Example, Linux:
  Tests/Lifecycle/run.py --platform linux --install ~/.se-test/x-magnetar \
      --ds64 ~/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64 \
      --template ~/.se-test/lifecycle-template --work ~/.se-test/lifecycle-run --jobs 4

Writes <work>/results.json and <work>/results.md. Exit code 0 when every case passed.
"""

import argparse
import concurrent.futures
import dataclasses
import fnmatch
import json
import queue
import re
import sys
import threading
import time
import traceback
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from cases import CASES  # noqa: E402
import rig  # noqa: E402
from rig import Platform, Server  # noqa: E402

READY_TIMEOUT = 300


def mem_available_gib():
    for line in Path("/proc/meminfo").read_text().splitlines():
        if line.startswith("MemAvailable:"):
            return int(line.split()[1]) / 2**20
    return 0


class RamGate:
    """Starts one server at a time, and only while enough RAM stays free."""

    def __init__(self, floor, need):
        self.floor, self.need = floor, need
        self.lock = threading.Lock()
        self.last = 0.0

    def __enter__(self):
        self.lock.acquire()
        # A server started a moment ago is still growing.
        while time.time() - self.last < 20 or mem_available_gib() - self.need < self.floor:
            time.sleep(5)
        self.last = time.time()

    def __exit__(self, *exc):
        self.lock.release()


def steam_timeout(work):
    """The DS's Steam game server logon timed out, which happens when many servers log on at once."""
    return any(
        "Error: No IP assigned." in p.read_text(errors="replace") for p in work.rglob("SpaceEngineersDedicated*.log")
    )


def run_steps(server, case, log, result):
    marked = None
    sent = 0  # when the last command went out
    for step in case.steps:
        kind = step[0]
        if kind == "ready":
            n = step[1]
            if not server.wait(lambda: len(server.ready_runs()) >= n or not server.alive(), READY_TIMEOUT):
                raise TimeoutError(f"no world load #{n}")
            if len(server.ready_runs()) < n:
                raise RuntimeError(f"server ended before world load #{n}")
            continue
        if kind == "loading":
            seen = len(server.ready_runs())

            # After a restart the old process' DS log is there until the new one starts its own.
            def loading():
                return (
                    "Loading session:" in server.ds_log()
                    and len(server.ready_runs()) == seen
                    and server.ds_log_started() >= int(sent)
                )

            if not server.wait(loading, READY_TIMEOUT, 0.2):
                raise TimeoutError("did not see the world loading")
            continue
        if kind == "sleep":
            time.sleep(step[1])
            continue
        if marked is None:
            marked = (time.time(), server.world_mtime())
            result["sent"] = True
        sent = time.time()
        if kind == "probe":
            server.probe(*step[1:])
        elif kind == "signal":
            server.signal(step[1])
        log.append(f"{time.strftime('%H:%M:%S')} {step}")
    return marked


def outcome_reached(server, case):
    if case.exit is not None:
        return not server.alive()
    return len(server.ready_runs()) >= 1 + case.restarts


def exit_codes(server, platform):
    if platform.windows:
        codes = server.windows_exit_codes()
        codes += [int(m) for m in re.findall(r"process-exit code=(-?\d+)", "\n".join(e[2] for e in server.events()))]
        return codes
    code = server.exit_code if server.exit_code is not None else server.proc.returncode
    if code is not None and code < 0:
        code = 128 - code
    return [code]


def check_exit(expected, code):
    if expected == "any":
        return True
    if expected == "nonzero":
        return code not in (0, None)
    if expected == "killed":
        return code not in (0, None)
    return code == expected


def run_case(case, platform, args, port, gate):
    work = Path(args.work) / case.id
    server = Server(work, platform, args.template, port, case.essentials, case.auto_restart)
    log, result = [], {"id": case.id, "state": case.state, "command": case.command, "platform": platform.name}
    failures = []
    ds_profile = platform.ds64 / "ProfileOptimization/Startup.profile"
    ds_profile_before = ds_profile.exists() and ds_profile.stat().st_mtime_ns
    try:
        server.setup()
        with gate:
            server.start()
        marked = run_steps(server, case, log, result)
        timeout = READY_TIMEOUT * (1 + case.restarts) if case.exit is None else 240
        if not server.wait(lambda: outcome_reached(server, case), timeout):
            failures.append("expected outcome not reached in time")
        time.sleep(case.settle)

        runs = server.ready_runs()
        alive = server.alive()
        result["world_loads"] = len(runs)
        result["pids"] = server.distinct_pids()
        result["processes"] = server.cmdlines
        result["ready_pids"] = [pid for pid, _ in runs]
        result["alive"] = alive
        terminating = [m.split()[1] for _, _, m in server.events() if m.startswith("terminating ")]
        result["terminating"] = terminating
        if marked:
            result["saved"] = server.world_mtime() > marked[1]
        result["save_calls"] = sum(1 for _, _, m in server.events() if m.startswith("save from"))

        if case.exit is None:
            if not alive:
                failures.append("server is not running")
        else:
            if alive:
                failures.append("server still running")
            server.wait(lambda: server.exit_code is not None or platform.windows, 30)
            codes = exit_codes(server, platform)
            result["exit_codes"] = codes
            if not codes or not check_exit(case.exit, codes[-1]):
                failures.append(f"exit code {codes}, expected {case.exit}")
        if max(len(runs) - 1, 0) != case.restarts:
            failures.append(f"{len(runs) - 1} restarts, expected {case.restarts}")
        if case.saved is not None and marked and result["saved"] != case.saved:
            failures.append(f"saved={result['saved']}, expected {case.saved}")
        if case.jit_profile:
            if not (server.data / "ProfileOptimization/Startup.profile").exists():
                failures.append("no JIT profile in the data folder")
            if (ds_profile.exists() and ds_profile.stat().st_mtime_ns) != ds_profile_before:
                failures.append("the JIT profile changed in the DS folder")
        replies = [m[len("reply ") :] for _, _, m in server.events() if m.startswith("reply ")]
        result["replies"] = replies
        for text in case.replies:
            if not any(text in reply for reply in replies):
                failures.append(f"no reply with {text!r}, got {replies}")
        if case.terminating is not None and terminating != case.terminating:
            failures.append(f"terminating {terminating}, expected {case.terminating}")
        if platform.windows:
            result["overlap_s"] = round(server.overlap_seconds(), 1)
            if result["overlap_s"] > 15:
                failures.append(f"two processes ran together for {result['overlap_s']} s")
            if case.restarts and len(set(result["ready_pids"])) != len(runs):
                failures.append("a restart kept the PID on Windows")
        else:
            result["max_processes"] = server.max_concurrent()
            if server.max_concurrent() > 1:
                failures.append(f"{server.max_concurrent()} processes at once")
            if case.restarts and len(set(result["ready_pids"])) != 1:
                failures.append("a restart changed the PID on Linux")
        if "Restart failed" in (work / "run.log").read_text(errors="replace"):
            failures.append("Restart failed on stderr")
        if "Error disposing plugins" in server.info_logs():
            failures.append("Error disposing plugins in the info log")
        racing = [m for _, _, m in server.events() if m.startswith("dispose updates=") and m != "dispose updates=0"]
        if racing:
            failures.append(f"plugins updated during disposal: {racing}")

        server.stop()
        if case.reload:
            server.exit_code = None
            server.samples.clear()
            before = len(server.ready_runs())
            with gate:
                server.start()
            loaded = server.wait(lambda: len(server.ready_runs()) > before or not server.alive(), READY_TIMEOUT)
            result["reloaded"] = loaded and len(server.ready_runs()) > before
            if not result["reloaded"]:
                failures.append("the world did not load again")
            server.stop()
    except Exception as e:
        failures.append(f"{type(e).__name__}: {e}")
        log.append(traceback.format_exc())
    finally:
        try:
            server.stop()
        except Exception as e:
            failures.append(f"cleanup: {e}")
    result["failures"] = failures
    result["passed"] = not failures
    result["log"] = log
    (work / "result.json").write_text(json.dumps(result, indent=2))
    return result


def markdown(results, platform):
    lines = [f"| Case | State | Command | {platform} | Notes |", "|---|---|---|---|---|"]
    for r in results:
        verdict = "pass" if r["passed"] else "**FAIL**"
        detail = []
        if "exit_codes" in r:
            detail.append(f"exit {r['exit_codes']}")
        if r.get("world_loads", 0) > 1:
            detail.append(f"{r['world_loads'] - 1} restarts")
        if "saved" in r:
            detail.append("saved" if r["saved"] else "not saved")
        if r.get("reloaded"):
            detail.append("reloads")
        detail += r["failures"]
        lines.append(f"| {r['id']} | {r['state']} | {r['command']} | {verdict} | {'; '.join(detail)} |")
    return "\n".join(lines) + "\n"


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--platform", choices=["linux", "proton-interim", "proton-legacy"], required=True)
    ap.add_argument("--install", required=True, help="Magnetar install folder")
    ap.add_argument("--ds64", required=True, help="DedicatedServer64 folder")
    ap.add_argument("--template", required=True, help="folder with data/ and magnetar/, see README.md")
    ap.add_argument("--work", required=True, help="one subfolder per case goes here")
    ap.add_argument("--jobs", type=int, default=2)
    ap.add_argument("--port-base", type=int, default=28100, help="DS ServerPort of job 0; SteamPort is +1000")
    ap.add_argument("--cases", default="*", help="comma separated glob patterns of case ids")
    ap.add_argument("--wrap", default="", help="command prefix, e.g. 'run-test.sh 4 --'")
    ap.add_argument("--ram-floor-gib", type=float, default=10)
    ap.add_argument("--proton", default=str(Path.home() / ".steam/steam/steamapps/common/Proton - Experimental"))
    ap.add_argument(
        "--prefix",
        help="STEAM_COMPAT_DATA_PATH of the Proton prefix; comma separated, one per job, "
        "because parallel servers in one prefix share its wineserver",
    )
    ap.add_argument("--display", default=":1")
    ap.add_argument(
        "--essentials-src",
        help="Essentials working copy to compile as a DevFolder instead of the MagnetarHub version; "
        "the cases marked essentials_src need it",
    )
    args = ap.parse_args()
    rig.ESSENTIALS_SRC = Path(args.essentials_src).expanduser() if args.essentials_src else None

    platform = Platform(
        args.platform,
        Path(args.install).expanduser(),
        Path(args.ds64).expanduser(),
        args.wrap.split(),
        Path(args.proton),
        None,
        args.display,
    )
    prefixes = [Path(p).expanduser() for p in args.prefix.split(",")] if args.prefix else [None]
    if platform.windows and len(prefixes) < args.jobs:
        ap.error("--prefix needs one prefix per job")
    patterns = args.cases.split(",")
    cases = [c for c in CASES if platform.name in c.platforms and any(fnmatch.fnmatch(c.id, p) for p in patterns)]
    if not args.essentials_src:
        skipped = [c.id for c in cases if c.essentials_src]
        if skipped:
            print(f"skipped without --essentials-src: {', '.join(skipped)}", flush=True)
        cases = [c for c in cases if not c.essentials_src]
    Path(args.work).mkdir(parents=True, exist_ok=True)

    slots = queue.Queue()
    for i in range(args.jobs):
        slots.put((args.port_base + i, prefixes[i % len(prefixes)]))
    gate = RamGate(args.ram_floor_gib, 4)

    def job(case):
        port, prefix = slots.get()
        try:
            r = run_case(case, dataclasses.replace(platform, prefix=prefix), args, port, gate)
            if not r["passed"] and (not r.get("sent") or steam_timeout(Path(args.work) / case.id)):
                # The server failed before the case sent anything, or its Steam logon timed out:
                # that says nothing about the case, run it again.
                first = r["failures"]
                r = run_case(case, dataclasses.replace(platform, prefix=prefix), args, port, gate)
                r["retried_after"] = first
        finally:
            slots.put((port, prefix))
        print(f"{time.strftime('%H:%M:%S')} {'PASS' if r['passed'] else 'FAIL'} {case.id} {r['failures']}", flush=True)
        return r

    with concurrent.futures.ThreadPoolExecutor(args.jobs) as pool:
        results = list(pool.map(job, cases))

    work = Path(args.work)
    (work / "results.json").write_text(json.dumps(results, indent=2))
    (work / "results.md").write_text(markdown(results, platform.name))
    failed = [r["id"] for r in results if not r["passed"]]
    print(f"{len(results) - len(failed)}/{len(results)} passed" + (f", failed: {', '.join(failed)}" if failed else ""))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
