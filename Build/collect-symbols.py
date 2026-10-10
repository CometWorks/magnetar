#!/usr/bin/env python3
"""Keep release PDBs and matching binary hashes outside portable runtime bundles."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys

def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify(target, runtime):
    manifest = json.loads((target / 'manifest.json').read_text())
    if not manifest['files']:
        raise ValueError('No release symbols found')
    for entry in manifest['files']:
        assert sha256(target / entry['pdb']) == entry['pdbSha256'], entry['pdb']
        for binary in entry['runtime']:
            assert sha256(runtime / binary) == entry['binarySha256'], binary
    print(f"Verified {len(manifest['files'])} PDBs against exact staged binaries")


def collect(target, runtime):
    root = Path(__file__).resolve().parents[1]
    target.mkdir(parents=True, exist_ok=True)
    if any(target.iterdir()):
        raise ValueError('Symbols output must be empty to prevent stale release evidence')
    shipped = {}
    for path in runtime.rglob('*'):
        if path.is_file() and path.suffix.lower() in ('.dll', '.exe'):
            shipped.setdefault(sha256(path), []).append(path.relative_to(runtime).as_posix())
    entries = []
    for path in sorted(root.rglob('*.pdb')):
        if target in path.parents:
            continue
        relative = path.relative_to(root)
        if 'bin' not in relative.parts or 'Release' not in relative.parts:
            continue
        binary = next((p for p in (path.with_suffix('.dll'), path.with_suffix('.exe')) if p.is_file()), None)
        if binary is None or sha256(binary) not in shipped:
            continue
        destination = target / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, destination)
        entries.append(dict(pdb=relative.as_posix(), pdbSha256=sha256(path),
                            binary=binary.relative_to(root).as_posix(), binarySha256=sha256(binary),
                            runtime=sorted(shipped[sha256(binary)])))
    commit = subprocess.check_output(['git', '-C', str(root), 'rev-parse', 'HEAD'], text=True).strip()
    dirty = bool(subprocess.check_output(['git', '-C', str(root), 'status', '--porcelain'], text=True).strip())
    (target / 'manifest.json').write_text(json.dumps(dict(schemaVersion=1, commit=commit,
        sourceDirty=dirty, files=entries), indent=2) + '\n')
    verify(target, runtime)


if __name__ == '__main__':
    args = sys.argv[1:]
    verifying = args[:1] == ['--verify']
    if verifying:
        args = args[1:]
    if len(args) != 2:
        raise SystemExit('Usage: collect-symbols.py [--verify] SYMBOLS_DIR STAGED_RUNTIME_DIR')
    (verify if verifying else collect)(*(Path(arg).resolve() for arg in args))
