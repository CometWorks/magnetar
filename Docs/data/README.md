# Docs/data — generation working set

Everything in this folder is **generated/working data** for the Magnetar code
handbook (the `structured-documentation` skill). The published handbook lives in
the parent `Docs/` folder (`TOC.md`, `Index.md`, `modules/`, `descriptions/`).
You can delete this `data/` folder without harming the handbook — it only makes
re-runs cheap and incremental. It may be safely git-ignored.

## Files

| File | Purpose |
| ---- | ------- |
| `manifest.jsonl` | One record per source file: path, project, module, size, **SHA256**, line count, tier, description path. The SHA256 is the cache key for incremental re-runs. Every source file is normalized to LF (`\r\n`/`\r` → `\n`) before its size and SHA256 are measured, so the hash is **platform-independent**: a checkout under Windows CRLF (or any `git core.autocrlf` setting) hashes identically to the same file under Linux LF. This is required because the repo is cloned on both Linux and Windows — without it, newline differences would invalidate the cache and force a needless full re-describe on the other platform. |
| `modules.json` | Module → files, project, total lines, chosen model, dominant tier. |
| `module-summaries.json` | Per-module structured summary returned by the description agents (purpose, role, key types, public API, dependencies). |
| `reference-graph.json` | File-level `uses` / `used_by` edges from static C# type-reference analysis. |
| `module-edges.json` | Module-level `uses` edges, constrained to the project-reference DAG. |
| `path-mapping.json` | Source path → description path (mirror layout). |
| `module-summaries-raw.json` | Raw workflow output (kept for provenance). |

## Scripts (re-run in this order)

```sh
python3 Docs/data/build_manifest.py     # 1. enumerate + LF-normalize + hash + tier + module
# 2. (AI) regenerate per-file descriptions for CHANGED files only — see below
python3 Docs/data/build_refgraph.py     # 3. reference graph + propagate "Used by"
python3 Docs/data/generate_docs.py      # 4. module docs + Index.md
python3 Docs/data/verify_links.py       # 5. verify all cross-references
```

`magnetar-describe.js` is the multi-agent workflow that writes the per-file
descriptions. On a re-run, diff `manifest.jsonl` against the previous run by
SHA256 and only re-describe files whose hash changed (then re-run steps 3–5).
Because the SHA256 is taken over LF-normalized content, re-cloning the repo on
the other platform does not change any hash and therefore re-describes nothing.
`TOC.md` is authored once and updated by hand when module boundaries change.
