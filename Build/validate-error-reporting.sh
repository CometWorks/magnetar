#!/usr/bin/env bash
# Compile both plugin targets without deploying. Pass source checkout and optional output directory.
set -euo pipefail
REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PLUGIN="$(realpath "${1:?Pass the error-reporting source checkout}")"
OUT="$(realpath -m "${2:-$REPO/Build/validation/plugin}")"
SDK="$REPO/PluginSdk/bin/Release/netstandard2.0"
mkdir -p "$OUT"
dotnet build "$REPO/PluginSdk/PluginSdk.csproj" -c Release --nologo -v q
for framework in net48 net10.0; do
    dotnet build "$PLUGIN/ServerPlugin/ServerPlugin.csproj" -c Release \
        "-p:TargetFramework=$framework" "-p:TargetFrameworks=$framework" \
        -p:RunPostBuildEvent=Never "-p:MagnetarBin=$SDK" "-p:OutputPath=$OUT/$framework/bin/" \
        --artifacts-path "$OUT/$framework" --nologo -v q
done
python3 - "$REPO" "$PLUGIN" "$SDK" "$OUT" <<'PY'
import hashlib, json, subprocess, sys
from pathlib import Path
repo, plugin, sdk, out = map(Path, sys.argv[1:])
def identity(path):
    def git(*args):
        return subprocess.check_output(['git', '-C', str(path), *args], text=True).strip()
    return dict(commit=git('rev-parse', 'HEAD'), sourceDirty=bool(git('status', '--porcelain')))
def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()
artifacts = {str(p.relative_to(out)): digest(p) for p in out.rglob('ErrorReporting.*')
             if p.suffix in ('.dll', '.pdb') and 'bin' in p.relative_to(out).parts}
assert sum(name.endswith('.dll') for name in artifacts) == 2
(out / 'validation.json').write_text(json.dumps(dict(magnetar=identity(repo),
    errorReporting=identity(plugin), pluginSdkSha256=digest(sdk / 'PluginSdk.dll'),
    compileTargets=['net48', 'net10.0'], artifacts=artifacts), indent=2) + '\n')
print('Both plugin compile targets passed; exact artifacts recorded in', out / 'validation.json')
PY
