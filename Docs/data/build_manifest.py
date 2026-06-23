#!/usr/bin/env python3
"""Build the documentation manifest for Magnetar.

Programmatically enumerates tracked C# source files, records size / line-count /
SHA256, and assigns each file a module and a processing tier using path + size
heuristics (no AI inference). Re-runnable: the SHA256 is the cache key, so a
later run can diff against a prior manifest and only re-process changed files.

All inputs are C# source (text), so every file is normalized to LF before
measuring size and hashing: the size and SHA256 become independent of newline
style. This keeps the manifest stable across platforms and git core.autocrlf
checkout settings (a CRLF working-tree copy hashes identically to its LF blob).
Binary inputs would be hashed by their exact bytes, but the enumeration here is
text-only so normalization always applies.

Description output paths are made unique on case-insensitive filesystems
(Windows, macOS): paths that would differ only in letter casing are detected
while the mapping is built and disambiguated deterministically, so a re-run on a
case-sensitive (Linux) source tree never emits two outputs that collide on
checkout. The chosen names are recorded in path-mapping.json so they stay stable
across runs.

Outputs (all under Docs/data/):
  manifest.jsonl     one JSON object per source file
  modules.json       module -> {files, project, model, dominant_tier}
  path-mapping.json  source path -> description path (mirror layout)
"""
import hashlib
import json
import os
import subprocess
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DATA = os.path.join(ROOT, "Docs", "data")


def normalize_newlines(data):
    """Normalize CRLF and lone CR to LF so size/hash are newline-independent."""
    return data.replace(b"\r\n", b"\n").replace(b"\r", b"\n")


def ensure_case_insensitive_unique(path_map):
    """Guarantee the description paths are unique on case-insensitive filesystems.

    The description tree mirrors the source tree, so a source repo authored on a
    case-sensitive (Linux) filesystem can produce outputs that collide once
    checked out on Windows or macOS. Detect those collisions while the mapping is
    built and disambiguate deterministically; the chosen names are written to
    path-mapping.json so they remain stable across runs. Mutates and returns the
    given mapping. A no-op when no collisions exist.

      * Directory-component collisions (e.g. 'Net/' vs 'net/') cannot be fixed by
        renaming a single output file; they require resolving the clash in the
        source tree. Fail loudly so the breakage is never produced silently.
      * File-leaf collisions are disambiguated by appending a short, stable hash
        of the source path before the extension.
    """
    # Detect directory-component case collisions across every output path.
    canonical = {}  # case-folded directory prefix -> first actual spelling seen
    for rel in sorted(path_map):
        parts = path_map[rel].split("/")
        for i in range(1, len(parts)):  # directory prefixes only (exclude leaf)
            key = "/".join(p.lower() for p in parts[:i])
            actual = "/".join(parts[:i])
            if canonical.setdefault(key, actual) != actual:
                raise ValueError(
                    "Case-insensitive directory collision in generated paths: "
                    f"{canonical[key]!r} vs {actual!r}. Resolve the clash in the "
                    "source tree (rename one) before regenerating."
                )

    # Disambiguate file leaves whose full paths differ only by letter casing.
    by_lower = defaultdict(list)
    for rel, desc in path_map.items():
        by_lower[desc.lower()].append(rel)
    for _, colliding in sorted(by_lower.items()):
        if len(colliding) < 2:
            continue
        for rel in sorted(colliding):
            root, ext = os.path.splitext(path_map[rel])
            suffix = hashlib.sha1(rel.encode("utf-8")).hexdigest()[:8]
            path_map[rel] = f"{root}__{suffix}{ext}"
    return path_map


def module_of(rel):
    parts = rel.split("/")
    top = parts[0]
    if top == "Compiler":
        return "Compiler", "Compiler"
    if top == "Legacy":
        if rel == "Legacy/Program.cs" or parts[1] == "Launcher":
            return "Legacy.Launcher", "Legacy"
        if parts[1] == "Loader":
            return "Legacy.Loader", "Legacy"
        if parts[1] == "Patch":
            return "Legacy.Patch", "Legacy"
        if parts[1] == "Commands":
            return "Legacy.Commands", "Legacy"
        return "Legacy.Integration", "Legacy"  # Compiler/, Extensions/, Paths/
    if top == "Shared":
        if len(parts) > 1 and parts[1] in ("Config", "Data", "Network", "Votes"):
            return "Shared." + parts[1], "Shared"
        return "Shared.Core", "Shared"
    if top == "PluginSdk":
        if len(parts) > 1 and parts[1] in ("Commands", "Config", "Logging", "Stats"):
            return "PluginSdk." + parts[1], "PluginSdk"
        return "PluginSdk.Runtime", "PluginSdk"  # Paths/, Tools/, ServerControl.cs
    if top == "PluginSdkTests":
        return "PluginSdkTests", "PluginSdkTests"
    if top == "MagnetarMod":
        return "MagnetarMod", "MagnetarMod"  # companion in-game world mod
    return "Other", top


# Files that warrant the strong model regardless of raw size (high complexity /
# central role). Everything else is tiered by line count.
TIER1_FORCE = {
    "Legacy/Program.cs",
    "Legacy/Launcher/ServerControl.cs",
    "Legacy/Loader/PluginLoader.cs",
    "Legacy/Loader/PluginInstance.cs",
    "Legacy/Loader/NativeLibraryPreloader.cs",
    "Shared/Preloader.cs",
    "Shared/PluginList.cs",
    "Shared/Data/PluginData.cs",
    "Shared/Data/GitHubPlugin.cs",
    "Compiler/RoslynCompiler.cs",
    "Compiler/Publicizer.cs",
    "PluginSdk/Config/ConfigSchema.cs",
    "PluginSdk/Config/TypeSerialization.cs",
    "PluginSdk/Config/PluginConfig.cs",
    "PluginSdk/Commands/CommandDispatcher.cs",
}


def tier_of(rel, lines):
    if rel.startswith("PluginSdkTests/"):
        return 2  # tests: medium model, lighter treatment
    if rel in TIER1_FORCE:
        return 1
    if lines <= 25:
        return 3
    if lines >= 200:
        return 1
    return 2


def main():
    rels = subprocess.check_output(
        ["git", "-C", ROOT, "ls-files", "*.cs"], text=True
    ).split()
    records = []
    path_map = {}
    for rel in sorted(rels):
        ap = os.path.join(ROOT, rel)
        with open(ap, "rb") as f:
            data = normalize_newlines(f.read())
        lines = data.count(b"\n") + (1 if data and not data.endswith(b"\n") else 0)
        module, project = module_of(rel)
        tier = tier_of(rel, lines)
        desc = "Docs/descriptions/" + rel + ".md"
        path_map[rel] = desc
        records.append({
            "path": rel,
            "project": project,
            "module": module,
            "type": "csharp",
            "size": len(data),
            "lines": lines,
            "sha256": hashlib.sha256(data).hexdigest(),
            "tier": tier,
            "desc": desc,
            "status": "pending",
        })

    # Keep description paths safe on case-insensitive filesystems, then sync the
    # (possibly disambiguated) names back into the manifest records.
    ensure_case_insensitive_unique(path_map)
    for r in records:
        r["desc"] = path_map[r["path"]]

    with open(os.path.join(DATA, "manifest.jsonl"), "w") as f:
        for r in records:
            f.write(json.dumps(r) + "\n")

    mods = defaultdict(list)
    for r in records:
        mods[r["module"]].append(r)
    # Strong-model modules: those containing any tier-1 file or large total.
    strong = {"Legacy.Launcher", "Legacy.Loader", "Shared.Core", "Shared.Data",
              "Compiler", "PluginSdk.Config"}
    modules = {}
    for m, items in sorted(mods.items()):
        tiers = [i["tier"] for i in items]
        dominant = min(tiers)  # lowest tier number = highest complexity present
        modules[m] = {
            "project": items[0]["project"],
            "files": [i["path"] for i in sorted(items, key=lambda x: x["path"])],
            "file_count": len(items),
            "total_lines": sum(i["lines"] for i in items),
            "model": "opus" if m in strong else "sonnet",
            "dominant_tier": dominant,
        }
    with open(os.path.join(DATA, "modules.json"), "w") as f:
        json.dump(modules, f, indent=2)
    with open(os.path.join(DATA, "path-mapping.json"), "w") as f:
        json.dump(path_map, f, indent=2)

    print(f"files={len(records)} modules={len(modules)}")
    for m, info in modules.items():
        print(f"  {m:24s} files={info['file_count']:2d} lines={info['total_lines']:5d} "
              f"model={info['model']:6s} tier={info['dominant_tier']}")


if __name__ == "__main__":
    main()
