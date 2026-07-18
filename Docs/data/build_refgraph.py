#!/usr/bin/env python3
"""Build a file-level reference graph for Magnetar via static C# analysis.

For each source file, find which repo-defined types it references, then derive
'uses' / 'used by' edges. High precision over recall: a type name that is
defined in more than one *logical* type (true collisions across projects, e.g.
`LogFile`, `Tools`) is treated as ambiguous and skipped so we don't invent
edges. Partial classes split across files are unified into one logical type.

Outputs Docs/data/reference-graph.json and patches the 'Used by' line of every
description file under Docs/descriptions/.
"""
import json
import os
import re
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DATA = os.path.join(ROOT, "Docs", "data")
DESC = os.path.join(ROOT, "Docs", "descriptions")

DECL_RE = re.compile(
    r"\b(?:public|internal|private|protected|static|sealed|abstract|partial|\s)*"
    r"(?:class|struct|interface|enum|record)\s+([A-Z][A-Za-z0-9_]+)")
# Match a PascalCase identifier that is NOT preceded by a word char or a dot,
# so namespace/member qualifiers (e.g. `VRage.Game`, `x.Tools`) don't count as
# a reference to a same-named repo type.
IDENT_RE = re.compile(r"(?<![\w.])([A-Z][A-Za-z0-9_]*)")

modules = json.load(open(os.path.join(DATA, "modules.json")))
file_module = {}
file_project = {}
for m, info in modules.items():
    for f in info["files"]:
        file_module[f] = m
        file_project[f] = info["project"]

# Allowed compile-time project reference direction (from the .csproj files).
# An edge A -> B is only real if B's project is reachable from A's project;
# this kills name collisions that would otherwise invent impossible edges
# (e.g. the leaf `Compiler` "referencing" `Shared`).
ALLOWED_PROJ = {
    "Compiler": {"Compiler"},
    "PluginSdk": {"PluginSdk"},
    "Shared": {"Shared", "Compiler"},
    "Legacy": {"Legacy", "Shared", "PluginSdk", "Compiler"},
    "PluginSdkTests": {"PluginSdkTests", "PluginSdk"},
    "MagnetarMod": {"MagnetarMod"},  # in-game mod; references SE ModAPI only
    "ConfigTerminal": {"ConfigTerminal"},  # standalone TUI; no repo project refs
    "ConfigTerminalTests": {"ConfigTerminalTests", "ConfigTerminal"},
}


def edge_allowed(a, b):
    pa, pb = file_project.get(a), file_project.get(b)
    return pb in ALLOWED_PROJ.get(pa, {pa})

rels = sorted(file_module)
text = {}
defined = defaultdict(set)   # type name -> set(files that declare it)
for rel in rels:
    src = open(os.path.join(ROOT, rel), encoding="utf-8", errors="replace").read()
    # strip line + block comments cheaply to cut false hits
    src_nc = re.sub(r"//.*", "", src)
    src_nc = re.sub(r"/\*.*?\*/", "", src_nc, flags=re.S)
    text[rel] = src_nc
    for name in DECL_RE.findall(src_nc):
        defined[name].add(rel)

# Unique type names (defined in exactly one file) are safe to match on.
# Partial classes: same name in multiple files of the SAME module count as one
# logical type owned by all those files; still usable as a reference target set.
unique_types = {}      # name -> defining file (only truly unique)
partial_types = {}     # name -> set(files) when all in one module (partial)
for name, files in defined.items():
    if len(files) == 1:
        unique_types[name] = next(iter(files))
    else:
        mods = {file_module.get(f) for f in files}
        if len(mods) == 1:
            partial_types[name] = files  # partial within a module

uses = defaultdict(set)
for rel in rels:
    idents = set(IDENT_RE.findall(text[rel]))
    for name in idents:
        if name in unique_types:
            tgt = unique_types[name]
            if tgt != rel and edge_allowed(rel, tgt):
                uses[rel].add(tgt)
        elif name in partial_types:
            for tgt in partial_types[name]:
                if tgt != rel and edge_allowed(rel, tgt):
                    uses[rel].add(tgt)

used_by = defaultdict(set)
for a, targets in uses.items():
    for b in targets:
        used_by[b].add(a)

graph = {
    "uses": {k: sorted(v) for k, v in sorted(uses.items())},
    "used_by": {k: sorted(v) for k, v in sorted(used_by.items())},
}
json.dump(graph, open(os.path.join(DATA, "reference-graph.json"), "w"), indent=2)

# module-level edges (cross-check / for TOC diagram)
medges = defaultdict(set)
for a, targets in uses.items():
    ma = file_module.get(a)
    for b in targets:
        mb = file_module.get(b)
        if ma and mb and ma != mb:
            medges[ma].add(mb)
json.dump({k: sorted(v) for k, v in sorted(medges.items())},
          open(os.path.join(DATA, "module-edges.json"), "w"), indent=2)

# Patch 'Used by' lines in description files.
patched = 0
for rel in rels:
    dpath = os.path.join(DESC, rel + ".md")
    if not os.path.exists(dpath):
        continue
    content = open(dpath, encoding="utf-8").read()
    # Sort for a deterministic, canonical order (set iteration order varies per
    # run with hash randomization, which would otherwise churn every file).
    users = sorted(used_by.get(rel, []))
    if users:
        # relative link from the description file to sibling description files
        links = ", ".join(
            f"[{os.path.basename(u)}]({os.path.relpath(os.path.join(DESC, u + '.md'), os.path.dirname(dpath))})"
            for u in users)
        repl = f"- **Used by:** {links}"
    else:
        repl = "- **Used by:** _none within the repository_"
    new = re.sub(r"- \*\*Used by:\*\*.*", repl, content, count=1)
    if new != content:
        open(dpath, "w", encoding="utf-8").write(new)
        patched += 1

print(f"files={len(rels)} unique_types={len(unique_types)} partial_types={len(partial_types)}")
print(f"use_edges={sum(len(v) for v in uses.values())} patched_descriptions={patched}")
print("module_edges:")
for k, v in sorted(medges.items()):
    print(f"  {k:22s} -> {', '.join(v)}")
