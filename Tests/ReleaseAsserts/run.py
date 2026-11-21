#!/usr/bin/env python3
"""Release assert test (SE-1800). See README.md next to this file.

Starts isolated Magnetar servers on a world with two grids sharing an entity id
and the release assert probe, one per case, and checks that each one logs the
assert, comes back from it and keeps running.

Case: label=<Magnetar install>,<linux-compat working copy or "hub">
"""

import argparse
import json
import os
import re
import shutil
import signal
import subprocess
import sys
import threading
import time
import xml.etree.ElementTree as ET
from pathlib import Path

HERE = Path(__file__).resolve().parent
GRID_ID = 118000000000000001
BLOCK_ID = 118000000000000002

GRID = """\
    <MyObjectBuilder_EntityBase xsi:type="MyObjectBuilder_CubeGrid">
      <SubtypeName />
      <EntityId>{grid_id}</EntityId>
      <PersistentFlags>CastShadows InScene</PersistentFlags>
      <PositionAndOrientation>
        <Position x="{x}" y="0" z="200" />
        <Forward x="0" y="0" z="-1" />
        <Up x="0" y="1" z="0" />
      </PositionAndOrientation>
      <LocalPositionAndOrientation xsi:nil="true" />
      <GridSizeEnum>Small</GridSizeEnum>
      <CubeBlocks>
        <MyObjectBuilder_CubeBlock xsi:type="MyObjectBuilder_CubeBlock">
          <SubtypeName>SmallBlockArmorBlock</SubtypeName>
        </MyObjectBuilder_CubeBlock>
        <MyObjectBuilder_CubeBlock xsi:type="MyObjectBuilder_BatteryBlock">
          <SubtypeName>SmallBlockBatteryBlock</SubtypeName>
          <EntityId>{block_id}</EntityId>
          <Min x="1" y="0" z="0" />
          <CustomName>Dup Battery</CustomName>
          <ShowOnHUD>false</ShowOnHUD>
          <Enabled>true</Enabled>
          <CurrentStoredPower>0.04</CurrentStoredPower>
          <ProducerEnabled>true</ProducerEnabled>
          <MaxStoredPower>0.04</MaxStoredPower>
        </MyObjectBuilder_CubeBlock>
      </CubeBlocks>
      <IsStatic>true</IsStatic>
      <DisplayName>DupGrid {name}</DisplayName>
      <DestructibleBlocks>true</DestructibleBlocks>
      <IsRespawnGrid>false</IsRespawnGrid>
      <LocalCoordSys>0</LocalCoordSys>
      <TargetingTargets />
      <NPCGridClaimElapsed xsi:nil="true" />
    </MyObjectBuilder_EntityBase>
"""


def make_world(ds: Path, world: Path) -> None:
    """Empty World plus two grids that share their grid and block entity ids."""
    shutil.copytree(ds / "Content/CustomWorlds/Empty World", world)
    sector = world / "SANDBOX_0_0_0_.sbs"
    text = sector.read_text(encoding="utf-8-sig")
    grids = "".join(
        GRID.format(grid_id=GRID_ID, block_id=BLOCK_ID, x=x, name=name)
        for x, name in ((0, "A"), (20, "B"))
    )
    if "<SectorObjects />" in text:
        text = text.replace("<SectorObjects />", f"<SectorObjects>\n{grids}  </SectorObjects>")
    else:
        text = text.replace("</SectorObjects>", grids + "  </SectorObjects>", 1)
    assert text.count("DupGrid") == 2, "no SectorObjects in the template world"
    sector.write_text(text, encoding="utf-8")
    for b5 in world.glob("*.sbsB5"):
        b5.unlink()


def prepare(args, case_dir: Path, linux_compat: str, index: int) -> None:
    shutil.rmtree(case_dir, ignore_errors=True)
    instance = case_dir / "magnetar"
    data = case_dir / "data"
    (instance / "Profiles").mkdir(parents=True)
    (instance / "Sources").mkdir()
    if args.github_cache:
        shutil.copytree(args.github_cache, instance / "GitHub")

    sources = ET.Element("SourcesConfig")
    ET.SubElement(sources, "ShowWarning").text = "false"
    hub = ET.SubElement(ET.SubElement(sources, "RemoteHubSources"), "RemoteHub")
    for key, value in (
        ("Name", "MagnetarHub"),
        ("Repo", "CometWorks/magnetar-hub"),
        ("Branch", "main"),
        ("Enabled", "true"),
        ("Trusted", "true"),
    ):
        ET.SubElement(hub, key).text = value
    local = ET.SubElement(sources, "LocalPluginSources")
    profile = ET.Element("Profile")
    ET.SubElement(profile, "Name").text = "Current"
    dev = ET.SubElement(profile, "DevFolder")
    plugins = [("release-assert-probe", HERE / "Probe", "ReleaseAssertProbe.xml")]
    if linux_compat != "hub":
        plugins.append(("linux-compat", Path(linux_compat), "ServerPlugin/ServerPlugin.xml"))
    if args.dotnet_compat:
        plugins.append(("dotnet-compat", args.dotnet_compat, "ServerPlugin/ServerPlugin.xml"))
    for name, folder, file in plugins:
        entry = ET.SubElement(local, "LocalPlugin")
        for key, value in (
            ("Name", name),
            ("Folder", str(folder)),
            ("File", file),
            ("Enabled", "true"),
        ):
            ET.SubElement(entry, key).text = value
        ET.SubElement(ET.SubElement(dev, "LocalFolderConfig"), "Id").text = name
    ET.ElementTree(sources).write(instance / "Sources/sources.xml")
    ET.ElementTree(profile).write(instance / "Profiles/Current.xml")

    world = data / "Saves/ReleaseAsserts"
    make_world(args.ds, world)
    cfg = ET.parse(args.ds_cfg)
    for key, value in {
        "IP": "127.0.0.1",
        "ServerPort": args.port_base + index,
        "SteamPort": args.port_base + 1000 + index,
        "IgnoreLastSession": "true",
        "RemoteApiEnabled": "false",
        "PauseGameWhenEmpty": "false",
        "AutoRestartEnabled": "false",
        "LoadWorld": world,
        "ServerName": f"ReleaseAsserts {case_dir.name}",
    }.items():
        setting = cfg.getroot().find(key)
        if setting is None:
            setting = ET.SubElement(cfg.getroot(), key)
        setting.text = str(value)
    cfg.write(data / "SpaceEngineers-Dedicated.cfg", encoding="utf-8")


def game_log(data: Path) -> str:
    logs = sorted(data.glob("SpaceEngineersDedicated*.log"), key=lambda p: p.stat().st_mtime)
    return logs[-1].read_text(errors="replace") if logs else ""


def run(args, label: str, install: Path, case_dir: Path, results: dict) -> None:
    data = case_dir / "data"
    out = case_dir / "magnetar.out"
    command = [
        *args.wrap.split(),
        install / "MagnetarInterim.bin",
        "-multiInstance", "-stableLogs", "-noprompt", "-noimplicitmod",
        "-consent", "deny",
        "-config", case_dir / "magnetar",
        "-ds64", args.ds / "DedicatedServer64",
        "-path", data,
    ]
    outcome = "timeout"
    with out.open("w") as output:
        process = subprocess.Popen(
            list(map(str, command)), cwd=install, stdout=output,
            stderr=subprocess.STDOUT, stdin=subprocess.DEVNULL, start_new_session=True,
        )
        deadline = time.monotonic() + args.timeout
        try:
            while time.monotonic() < deadline:
                if process.poll() is not None:
                    outcome = f"exited {process.returncode}"
                    break
                log = game_log(data)
                if "ReleaseAssertProbe: returned" in log:
                    time.sleep(5)  # the server must keep running after the assert
                    outcome = "running" if process.poll() is None else f"exited {process.returncode}"
                    break
                time.sleep(2)
        finally:
            if process.poll() is None:
                os.killpg(process.pid, signal.SIGTERM)
                try:
                    process.wait(timeout=20)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait()
    log = game_log(data) + out.read_text(errors="replace")
    result = {
        "outcome": outcome,
        "dropped_duplicates": len(re.findall(r"Dropping entity with duplicated id", log)),
        "probe_fired": "ReleaseAssertProbe: firing" in log,
        "assert_logged": "ASSERT: ReleaseAssertProbe assert" in log,
        "probe_returned": "ReleaseAssertProbe: returned" in log,
        "winforms_error": "System.Windows.Forms" in log,
    }
    result["pass"] = (
        outcome == "running"
        and result["dropped_duplicates"] > 0
        and result["assert_logged"]
        and result["probe_returned"]
    )
    results[label] = result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("cases", nargs="+", help="label=<install>,<linux-compat dir or hub>")
    parser.add_argument("--work", type=Path, required=True)
    parser.add_argument("--ds", type=Path, required=True, help="SpaceEngineersDedicatedServer folder")
    parser.add_argument("--ds-cfg", type=Path, required=True, help="template SpaceEngineers-Dedicated.cfg")
    parser.add_argument("--dotnet-compat", type=Path, help="dotnet-compat working copy (DevFolder)")
    parser.add_argument("--github-cache", type=Path, help="a Magnetar GitHub/ folder to start from")
    parser.add_argument("--port-base", type=int, default=28100)
    parser.add_argument("--timeout", type=int, default=420)
    parser.add_argument("--wrap", default="", help="command prefix, e.g. a RAM guard")
    args = parser.parse_args()

    results: dict = {}
    threads = []
    for index, case in enumerate(args.cases):
        label, spec = case.split("=", 1)
        install, linux_compat = spec.split(",", 1)
        case_dir = args.work / label
        prepare(args, case_dir, linux_compat, index)
        thread = threading.Thread(target=run, args=(args, label, Path(install), case_dir, results))
        thread.start()
        threads.append(thread)
        time.sleep(5)
    for thread in threads:
        thread.join()

    for label in (case.split("=", 1)[0] for case in args.cases):
        result = results[label]
        print(f"{'PASS' if result['pass'] else 'FAIL'} {label}: {json.dumps(result)}")
    (args.work / "results.json").write_text(json.dumps(results, indent=2))
    return 0 if all(r["pass"] for r in results.values()) else 1


if __name__ == "__main__":
    sys.exit(main())
