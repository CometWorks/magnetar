#!/usr/bin/env python3
"""Check that the PluginSdkContract version matches the public API change.

Compares PluginSdk/PublicAPI.*.txt and the PluginSdkContract attribute of the
working tree with those of a base ref, by default the latest release tag:

- API lines only added: the minor must have gone up (or the major)
- any API line removed or changed: the major must have gone up
- no API change: the contract may stay; a major bump for an incompatible
  behavior change is the PR author's call

Usage: Build/check-plugin-sdk-contract.py [base-ref]
Needs the base ref in the local clone (CI checks out with fetch-depth 0).
"""

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
API_FILES = ("PluginSdk/PublicAPI.Shipped.txt", "PluginSdk/PublicAPI.Unshipped.txt")
CONTRACT_FILE = "PluginSdk/Properties/Contract.cs"
CONTRACT_RE = re.compile(
    r'AssemblyMetadata\(\s*"PluginSdkContract"\s*,\s*"(\d+)\.(\d+)"\s*\)'
)


def git(*args):
    result = subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True)
    return result.stdout if result.returncode == 0 else None


def read(ref, path):
    """File content at ref, or in the working tree when ref is None; None if missing."""
    if ref is None:
        file = ROOT / path
        return file.read_text(encoding="utf-8") if file.exists() else None
    return git("show", f"{ref}:{path}")


def contract(ref):
    text = read(ref, CONTRACT_FILE)
    match = CONTRACT_RE.search(text) if text else None
    return (int(match[1]), int(match[2])) if match else None


def api(ref):
    """Declared public API: shipped plus unshipped, minus the *REMOVED* entries."""
    lines, removed = set(), set()
    for path in API_FILES:
        for line in (read(ref, path) or "").splitlines():
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            if line.startswith("*REMOVED*"):
                removed.add(line[len("*REMOVED*") :])
            else:
                lines.add(line)
    return lines - removed


def latest_release_tag():
    tags = (git("tag", "--list", "v[0-9]*", "--sort=-v:refname") or "").split()
    return next((t for t in tags if "-" not in t), None)


def main():
    base = sys.argv[1] if len(sys.argv) > 1 else latest_release_tag()
    if base is None:
        print("No release tag found; nothing to compare against.")
        return 0

    head_contract = contract(None)
    if head_contract is None:
        print(
            f"ERROR: no PluginSdkContract attribute in {CONTRACT_FILE}", file=sys.stderr
        )
        return 1
    base_contract = contract(base)
    if base_contract is None:
        print(f"{base} declares no PluginSdkContract; nothing to compare against.")
        return 0

    base_api, head_api = api(base), api(None)
    removed = sorted(base_api - head_api)
    added = sorted(head_api - base_api)

    def show(version):
        return "%d.%d" % version

    print(
        f"PluginSdkContract {show(base_contract)} at {base}, {show(head_contract)} now; "
        f"{len(added)} API line(s) added, {len(removed)} removed or changed."
    )
    for line in removed:
        print(f"  - {line}")
    for line in added:
        print(f"  + {line}")

    if head_contract < base_contract:
        error = "the contract version went down"
    elif removed and head_contract[0] <= base_contract[0]:
        error = "public API was removed or changed, so the major version must go up"
    elif added and head_contract == base_contract:
        error = "public API was added, so the minor version must go up"
    else:
        print("OK")
        return 0
    print(f"ERROR: {error} (PluginSdkContract in {CONTRACT_FILE}).", file=sys.stderr)
    return 1


if __name__ == "__main__":
    sys.exit(main())
