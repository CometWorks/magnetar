#!/usr/bin/env python3
"""Assemble module docs, TOC.md and Index.md from the structured summaries and
the reference graph. Deterministic (no AI): every fact comes from the per-file
descriptions or the data files produced earlier."""
import json
import os
import re
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DOCS = os.path.join(ROOT, "Docs")
DATA = os.path.join(DOCS, "data")
DESC = os.path.join(DOCS, "descriptions")
MODS = os.path.join(DOCS, "modules")

modules = json.load(open(os.path.join(DATA, "modules.json")))
summaries = {s["module"]: s for s in json.load(open(os.path.join(DATA, "module-summaries.json")))}
medges = json.load(open(os.path.join(DATA, "module-edges.json")))
manifest = [json.loads(l) for l in open(os.path.join(DATA, "manifest.jsonl")) if l.strip()]

KNOWN_MODULES = set(modules)
PROJECT_OF = {m: info["project"] for m, info in modules.items()}

# reverse module edges (used-by)
used_by_mod = defaultdict(set)
for m, outs in medges.items():
    for o in outs:
        used_by_mod[o].add(m)


def first_summary_sentence(rel):
    """Pull the first sentence of the '## Summary' section of a description."""
    p = os.path.join(DESC, rel + ".md")
    if not os.path.exists(p):
        return ""
    txt = open(p, encoding="utf-8").read()
    m = re.search(r"## Summary\s*\n(.+?)(?:\n#|\n## |\Z)", txt, re.S)
    if not m:
        return ""
    body = " ".join(m.group(1).split())
    # first sentence (up to '. ' followed by capital, or whole if short)
    sent = re.split(r"(?<=\.)\s+(?=[A-Z`])", body)[0]
    return sent.strip()


def is_external(dep):
    # internal if it mentions any known module token
    for km in KNOWN_MODULES:
        if km in dep:
            return False
    # PluginSdk.Tools / PluginSdk.Paths sub-namespaces count as internal
    if dep.startswith("PluginSdk.") or dep.startswith("Legacy.") or dep.startswith("Shared."):
        return False
    return True


def rel_link(text, target_abs, from_dir):
    # Always emit forward slashes so the generator is deterministic across
    # platforms (os.path.relpath yields backslashes on Windows) and the links
    # render correctly on both Windows and Linux.
    rel = os.path.relpath(target_abs, from_dir).replace(os.sep, "/")
    return f"[{text}]({rel})"


# ---------- module docs ----------
file_lines = {r["path"]: r["lines"] for r in manifest}
for m, info in modules.items():
    s = summaries.get(m, {})
    files = info["files"]
    total_lines = info["total_lines"]
    out = []
    out.append(f"# Module: {m}\n")
    out.append(f"**Project:** `{info['project']}` · **Files:** {len(files)} · "
               f"**Source lines:** {total_lines}\n")
    out.append("## Purpose\n")
    out.append((s.get("purpose") or "_n/a_") + "\n")
    out.append("## Role in Magnetar\n")
    out.append((s.get("role") or "_n/a_") + "\n")

    kt = s.get("key_types") or []
    if kt:
        out.append("## Key types\n")
        out.append("| Type | Kind | Defined in | Summary |")
        out.append("| ---- | ---- | ---------- | ------- |")
        for t in kt:
            f = t.get("file", "")
            link = rel_link(f"`{f}`", os.path.join(DESC, f + ".md"), MODS) if f in file_lines else f"`{f}`"
            out.append(f"| `{t.get('name','')}` | {t.get('kind','')} | {link} | {t.get('summary','').replace('|','\\|')} |")
        out.append("")

    out.append("## Files\n")
    out.append("| File | Lines | Summary |")
    out.append("| ---- | ----- | ------- |")
    for f in files:
        link = rel_link(f"`{f}`", os.path.join(DESC, f + ".md"), MODS)
        summ = first_summary_sentence(f).replace("|", "\\|")
        out.append(f"| {link} | {file_lines.get(f,'')} | {summ} |")
    out.append("")

    api = s.get("public_api") or []
    if api:
        out.append("## Public API surface\n")
        for a in api:
            out.append(f"- `{a}`")
        out.append("")

    out.append("## Dependencies\n")
    uses = sorted(medges.get(m, []))
    ub = sorted(used_by_mod.get(m, []))
    if uses:
        out.append("**Uses modules:** " + ", ".join(rel_link(u, os.path.join(MODS, u + ".md"), MODS) for u in uses) + "  ")
    else:
        out.append("**Uses modules:** _none_  ")
    if ub:
        out.append("**Used by modules:** " + ", ".join(rel_link(u, os.path.join(MODS, u + ".md"), MODS) for u in ub) + "  ")
    else:
        out.append("**Used by modules:** _none_  ")
    ext = sorted({d for d in (s.get("depends_on") or []) if is_external(d)})
    if ext:
        out.append("**External systems:** " + "; ".join(ext))
    out.append("")
    out.append("---")
    out.append(rel_link("◀ Back to TOC", os.path.join(DOCS, "TOC.md"), MODS) +
               " · " + rel_link("Full file index", os.path.join(DOCS, "Index.md"), MODS))
    out.append("")
    open(os.path.join(MODS, m + ".md"), "w", encoding="utf-8").write("\n".join(out))

print(f"wrote {len(modules)} module docs")

# ---------- Index.md ----------
idx = ["# Magnetar — Full File Index\n",
       "Every documented source file, grouped by module. "
       f"{len(manifest)} files across {len(modules)} modules.\n",
       rel_link("◀ Back to TOC", os.path.join(DOCS, "TOC.md"), DOCS) + "\n"]
by_mod = defaultdict(list)
for r in manifest:
    by_mod[r["module"]].append(r)
for m in sorted(modules):
    idx.append(f"## {m}  ·  " + rel_link("module doc", os.path.join(MODS, m + ".md"), DOCS) + "\n")
    idx.append("| File | Lines | Tier | Description |")
    idx.append("| ---- | ----- | ---- | ----------- |")
    for r in sorted(by_mod[m], key=lambda x: x["path"]):
        link = rel_link(f"`{r['path']}`", os.path.join(DESC, r["path"] + ".md"), DOCS)
        summ = first_summary_sentence(r["path"]).replace("|", "\\|")
        idx.append(f"| {link} | {r['lines']} | {r['tier']} | {summ} |")
    idx.append("")
open(os.path.join(DOCS, "Index.md"), "w", encoding="utf-8").write("\n".join(idx))
print("wrote Index.md")
