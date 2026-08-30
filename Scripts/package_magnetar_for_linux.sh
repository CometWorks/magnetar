#!/usr/bin/env bash
# package_magnetar_for_linux.sh
#
# Builds a distributable Linux bundle for Magnetar - the Space Engineers
# Dedicated Server plugin loader.
#
# Magnetar is headless: no GUI, no DXVK, no Steam overlay, no XDG menu
# entry. The bundle mirrors the install layout the MSBuild Deploy targets
# produce (the same layout Pulsar uses), plus the MagnetarConfig terminal UI:
#
#   MagnetarForLinux/
#   ├── install.sh              Replaces ~/.local/share/Magnetar/ with the
#   │                           bundled Magnetar/ tree. Warns if the .NET 10
#   │                           runtime is not installed.
#   ├── uninstall.sh            Removes ~/.local/share/Magnetar/ entirely
#   │                           and removes ~/.config/Magnetar/ contents
#   │                           EXCEPT user state:
#   │                             config.xml, Sources/, Local/, Profiles/.
#   ├── README.txt
#   └── Magnetar/               Install tree (deployed verbatim):
#       ├── MagnetarInterim.bin    The launcher apphost (run this in place of
#       │                          SpaceEngineersDedicated). Framework-dependent;
#       │                          needs the system .NET 10 runtime.
#       ├── MagnetarInterim.{dll,deps.json,runtimeconfig.json}
#       ├── MagnetarConfig         Bash shim (cd Config + exec MagnetarConfig)
#       │                          for the terminal configuration UI.
#       ├── LICENSE, README.md
#       ├── Libraries/
#       │   ├── MagnetarInterim/   Managed deps (Pulsar.Shared, PluginSdk,
#       │   │                      Harmony, ...) + Steamworks.NET.dll and the
#       │   │                      native .so set staged by ./build.sh.
#       │   └── Compiler/          The out-of-process Roslyn compiler
#       │                          (Compiler.bin + Roslyn + deps).
#       └── Config/                Framework-dependent publish output for
#                                  MagnetarConfig (apphost + Terminal.Gui).
#
# Usage:
#   ./package_magnetar_for_linux.sh [output_dir]
#
# Env-var overrides (defaults shown):
#   MAGNETAR_REPO_DIR=<repo root>              (auto-detected from script location)
#   BUILD_DIR=$MAGNETAR_REPO_DIR/build         (gitignored staging area)
#   OUTPUT_DIR=$MAGNETAR_REPO_DIR/dist         (first positional arg overrides)
#
# Requirements: dotnet (.NET 10 SDK), 7z, git.

set -euo pipefail

# ---- configuration ----------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MAGNETAR_REPO_DIR="${MAGNETAR_REPO_DIR:-$(cd "$SCRIPT_DIR/.." && pwd)}"
BUILD_DIR="${BUILD_DIR:-$MAGNETAR_REPO_DIR/build}"
OUTPUT_DIR="${1:-${OUTPUT_DIR:-$MAGNETAR_REPO_DIR/dist}}"

MAGNETAR_CSPROJ="$MAGNETAR_REPO_DIR/Legacy/Legacy.csproj"
CONFIG_CSPROJ="$MAGNETAR_REPO_DIR/ConfigTerminal/ConfigTerminal.csproj"
CONFIG_PUBLISH_DIR="$MAGNETAR_REPO_DIR/ConfigTerminal/bin/Release/net10.0/publish"
# Honour the override build.sh exports and documents; hard-coding it here made
# LIBRARIES_DIR=<dir> ./build.sh stage into one place and package from another.
LIBRARIES_DIR="${LIBRARIES_DIR:-$BUILD_DIR/Libraries}"

# ---- preflight --------------------------------------------------------------

require_tool() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "ERROR: required tool not found on PATH: $1" >&2
        exit 1
    fi
}

require_tool dotnet
require_tool 7z
require_tool git

if [ ! -f "$MAGNETAR_CSPROJ" ]; then
    echo "ERROR: $MAGNETAR_CSPROJ not found." >&2
    exit 1
fi

if [ ! -d "$LIBRARIES_DIR" ]; then
    echo "ERROR: $LIBRARIES_DIR is missing." >&2
    echo "       Run ./build.sh first to stage Steamworks.NET.dll + the native .so set." >&2
    exit 1
fi

if [ ! -f "$MAGNETAR_REPO_DIR/Pulsar/Shared/Shared.csproj" ]; then
    echo "ERROR: the Pulsar submodule is not checked out." >&2
    echo "       Run: git submodule update --init" >&2
    exit 1
fi

mkdir -p "$BUILD_DIR" "$OUTPUT_DIR"

# ---- version info -----------------------------------------------------------

BUILD_DATE="$(date +%Y%m%d)"
GIT_HASH="$(cd "$MAGNETAR_REPO_DIR" && git rev-parse --short=8 HEAD)"

echo "==> Magnetar repo : $MAGNETAR_REPO_DIR (hash $GIT_HASH)"
echo "==> Build dir     : $BUILD_DIR"
echo "==> Output dir    : $OUTPUT_DIR"

# ---- stage ------------------------------------------------------------------
# Wipe the previous staging tree wholesale so leftover files can never
# end up in the .7z.

PKG_ROOT="$BUILD_DIR/MagnetarForLinux"
MAGNETAR_ROOT="$PKG_ROOT/Magnetar"
rm -rf "$PKG_ROOT"
mkdir -p "$MAGNETAR_ROOT"

# ---- build & deploy ---------------------------------------------------------
# The Legacy project's Deploy target (and the Pulsar Compiler project's own
# Deploy target, which receives the same deployment root) stage the complete
# install tree into $MAGNETAR_ROOT: launcher apphost + Libraries/. Framework-
# dependent: the host must have the .NET 10 runtime installed; the apphost
# discovers it via the standard FrameworkResolver search path.

echo
echo "############################################################"
echo "# build + deploy: Legacy -> $MAGNETAR_ROOT"
echo "############################################################"
dotnet build "$MAGNETAR_CSPROJ" \
    -c Release \
    -p:Magnetar="$MAGNETAR_ROOT" \
    -p:Steamworks="$LIBRARIES_DIR" \
    -p:DebugType=None \
    -p:DebugSymbols=false

# Sanity check the deployed tree
for required in \
    MagnetarInterim.bin \
    MagnetarInterim.dll \
    MagnetarInterim.deps.json \
    MagnetarInterim.runtimeconfig.json \
    LICENSE \
    Libraries/MagnetarInterim/Pulsar.Shared.dll \
    Libraries/MagnetarInterim/Pulsar.Protocol.dll \
    Libraries/MagnetarInterim/PluginSdk.dll \
    Libraries/MagnetarInterim/Steamworks.NET.dll \
    Libraries/MagnetarInterim/libsteam_api.so \
    Libraries/MagnetarInterim/libEOSSDK-Linux-Shipping.so \
    Libraries/MagnetarInterim/libHavok.so \
    Libraries/Compiler/Compiler.bin \
    Libraries/Compiler/Microsoft.CodeAnalysis.CSharp.dll \
; do
    if [ ! -e "$MAGNETAR_ROOT/$required" ]; then
        echo "ERROR: missing $required in $MAGNETAR_ROOT" >&2
        exit 1
    fi
done

# ---- publish: ConfigTerminal (MagnetarConfig TUI, framework-dependent) ------
# Ships next to MagnetarInterim so operators can configure the instance from
# the same install. Its apphost is MagnetarConfig, launched via the shim below.

echo
echo "############################################################"
echo "# publish: ConfigTerminal / MagnetarConfig (framework-dependent)"
echo "############################################################"
rm -rf "$CONFIG_PUBLISH_DIR"
dotnet publish "$CONFIG_CSPROJ" \
    -c Release \
    -f net10.0 \
    --no-self-contained \
    -p:DebugType=None \
    -p:DebugSymbols=false

for required in MagnetarConfig MagnetarConfig.dll MagnetarConfig.deps.json MagnetarConfig.runtimeconfig.json Terminal.Gui.dll NStack.dll; do
    if [ ! -e "$CONFIG_PUBLISH_DIR/$required" ]; then
        echo "ERROR: missing $required in $CONFIG_PUBLISH_DIR" >&2
        exit 1
    fi
done

echo "==> Staging ConfigTerminal output -> Magnetar/Config/"
mkdir -p "$MAGNETAR_ROOT/Config"
cp -a "$CONFIG_PUBLISH_DIR/." "$MAGNETAR_ROOT/Config/"

# ---- generate Magnetar/MagnetarConfig launcher -----------------------------
# Sits next to MagnetarInterim.bin (~/.local/share/Magnetar/MagnetarConfig).
# Runs the ConfigTerminal TUI from Config/ so its managed deps resolve locally.

cat > "$MAGNETAR_ROOT/MagnetarConfig" <<'EOF'
#!/usr/bin/env bash
# MagnetarConfig - terminal UI to configure and operate the Magnetar-managed
# Space Engineers Dedicated Server instance (DS config, worlds, plugins, mods,
# profiles, start/stop, logs). Same instance folders as MagnetarInterim.
#
# Usage: ~/.local/share/Magnetar/MagnetarConfig [args]
#   e.g. MagnetarConfig -path <DS data dir> -config <Magnetar config dir>
#        MagnetarConfig -diag        headless read-only status report
#        MagnetarConfig -help

set -euo pipefail

PKG_DIR="$(cd "$(dirname "$0")" && pwd)"
CONFIG="$PKG_DIR/Config/MagnetarConfig"

if [ ! -x "$CONFIG" ]; then
    echo "ERROR: MagnetarConfig binary not found at $CONFIG" >&2
    echo "Hint: run install.sh from the extracted MagnetarForLinux archive first." >&2
    exit 1
fi

cd "$PKG_DIR/Config"
exec "$CONFIG" "$@"
EOF
chmod +x "$MAGNETAR_ROOT/MagnetarConfig"

# ---- generate install.sh ----------------------------------------------------

cat > "$PKG_ROOT/install.sh" <<'EOF'
#!/usr/bin/env bash
# install.sh - Replaces ~/.local/share/Magnetar/ with the bundled Magnetar/
# tree (launcher apphost, Libraries/, Config/, MagnetarConfig shim). All user
# state lives under ~/.config/Magnetar/ and is untouched. Warns (does not
# fail) if the host doesn't appear to have .NET 10 installed.
#
# Usage:   ./install.sh
# Env-var overrides:
#   MAGNETAR_DATA_DIR  target dir for binaries (default: ~/.local/share/Magnetar)
#   XDG_DATA_HOME      base for the default target dir

set -euo pipefail

ARCHIVE_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC="$ARCHIVE_DIR/Magnetar"
DATA_DST="${MAGNETAR_DATA_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/Magnetar}"

if [ ! -d "$SRC" ]; then
    echo "ERROR: $SRC not found - run install.sh from the extracted archive." >&2
    exit 1
fi

# Refuse to overwrite the install tree while either the server (MagnetarInterim)
# or the config TUI (MagnetarConfig) is running out of it — the .NET host would
# fault on the next not-yet-loaded assembly, and MagnetarConfig could be mid-write
# to a config file.
for proc in MagnetarInterim MagnetarConfig; do
    if pgrep -x "$proc" >/dev/null 2>&1; then
        echo "ERROR: $proc is running. Stop it before deploying (pkill -x $proc)." >&2
        exit 1
    fi
done

# ---- .NET 10 detection (host requirement) ---------------------------------
if ! command -v dotnet >/dev/null 2>&1; then
    echo "WARNING: 'dotnet' not in PATH. Magnetar requires .NET 10 runtime" >&2
    echo "         installed system-wide (Microsoft.NETCore.App 10.x)." >&2
elif ! dotnet --list-runtimes 2>/dev/null | grep -q '^Microsoft.NETCore.App 10\.'; then
    echo "WARNING: .NET 10 runtime not detected in 'dotnet --list-runtimes'." >&2
    echo "         Install Microsoft.NETCore.App 10.x before launching Magnetar." >&2
fi

# ---- replace the install tree ----------------------------------------------
# The whole directory is binaries owned by this bundle (user state lives in
# ~/.config/Magnetar), so replace it wholesale; this also cleans up the
# Bin/-based layout of pre-2.0 bundles.
echo "==> Deploying to $DATA_DST"
rm -rf "$DATA_DST"
mkdir -p "$(dirname "$DATA_DST")"
cp -a "$SRC" "$DATA_DST"

echo
echo "Done. Launch the dedicated server through Magnetar with:"
echo "    $DATA_DST/MagnetarInterim.bin"
echo "Configure it any time with the terminal UI:"
echo "    $DATA_DST/MagnetarConfig"
EOF
chmod +x "$PKG_ROOT/install.sh"

# ---- generate uninstall.sh -------------------------------------------------

cat > "$PKG_ROOT/uninstall.sh" <<'EOF'
#!/usr/bin/env bash
# uninstall.sh - Wipes ~/.local/share/Magnetar/ entirely and scrubs the
# non-user-managed parts of ~/.config/Magnetar/. PRESERVES the user state:
#   - config.xml
#   - Sources/      (plugin source definitions, cached hub catalogs)
#   - Local/        (user-side-loaded plugin DLLs)
#   - Profiles/     (plugin profiles)
#
# Usage:   ./uninstall.sh
# Env-var overrides:
#   MAGNETAR_DATA_DIR  binary install dir (default: ~/.local/share/Magnetar)
#   MAGNETAR_DIR       user-state dir     (default: ~/.config/Magnetar)

set -euo pipefail

DATA_DST="${MAGNETAR_DATA_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/Magnetar}"
DST="${MAGNETAR_DIR:-${XDG_CONFIG_HOME:-$HOME/.config}/Magnetar}"

# Don't wipe the install tree out from under a running server or config TUI.
for proc in MagnetarInterim MagnetarConfig; do
    if pgrep -x "$proc" >/dev/null 2>&1; then
        echo "ERROR: $proc is running. Stop it before uninstalling (pkill -x $proc)." >&2
        exit 1
    fi
done

if [ -d "$DATA_DST" ]; then
    echo "==> Removing $DATA_DST"
    rm -rf "$DATA_DST"
else
    echo "==> $DATA_DST not present - skipping"
fi

if [ -d "$DST" ]; then
    echo "==> Cleaning $DST (preserving config.xml, Sources/, Local/, Profiles/)"
    shopt -s dotglob nullglob
    for entry in "$DST"/*; do
        name="$(basename "$entry")"
        case "$name" in
            config.xml|Sources|Local|Profiles)
                echo "    keep  $name"
                ;;
            *)
                rm -rf "$entry"
                echo "    rm    $name"
                ;;
        esac
    done
    shopt -u dotglob nullglob
else
    echo "==> $DST not present - skipping"
fi

echo
echo "Done."
EOF
chmod +x "$PKG_ROOT/uninstall.sh"

# ---- leak check ------------------------------------------------------------

echo
echo "==> Verifying staged tree has no build-machine path references"
LEAK_PATTERNS=(
    "$MAGNETAR_REPO_DIR"
    "$HOME/.nuget"
    "$HOME/.dotnet"
)
LEAK_HITS=""
for pat in "${LEAK_PATTERNS[@]}"; do
    [ -z "$pat" ] && continue
    [ "$pat" = "/" ] && continue
    if hits="$(grep -rlIF -- "$pat" "$PKG_ROOT" 2>/dev/null)"; then
        if [ -n "$hits" ]; then
            LEAK_HITS+=$'\n'"  pattern: $pat"$'\n'"$(printf '    %s\n' $hits)"
        fi
    fi
done
if [ -n "$LEAK_HITS" ]; then
    echo "ERROR: build-tree paths leaked into the staged bundle (text files):" >&2
    echo "$LEAK_HITS" >&2
    exit 1
fi

# ---- README -----------------------------------------------------------------

cat > "$PKG_ROOT/README.txt" <<EOF
MagnetarForLinux ($BUILD_DATE.$GIT_HASH)
========================================

Magnetar is a plugin and mod loader for the Space Engineers Dedicated
Server on Linux, built on Pulsar (the game-client plugin loader). This
bundle ships the MagnetarInterim launcher together with MagnetarConfig,
a terminal UI to configure and operate the server (step 4 below) — both
as framework-dependent .NET 10 builds; the .NET 10 runtime must be
installed system-wide on the host.

Prerequisites
-------------
- Space Engineers Dedicated Server installed (via Steam or steamcmd).
- .NET 10 runtime installed system-wide (Microsoft.NETCore.App 10.x).
- Outbound HTTPS to GitHub on first launch if you want MagnetarHub-listed
  plugins to be fetched and compiled automatically.

Quick start
-----------
1. Extract:
       7z x MagnetarForLinux.7z
2. Deploy:
       cd MagnetarForLinux
       ./install.sh
3. Launch the dedicated server through Magnetar in place of
   SpaceEngineersDedicated:
       ~/.local/share/Magnetar/MagnetarInterim.bin -console
4. Configure and operate the instance from the terminal UI:
       ~/.local/share/Magnetar/MagnetarConfig
   (edit DS/world settings, plugins, mods, profiles; start/stop; read logs.
    Add -diag for a headless status report, or -help for options.)

Magnetar auto-detects the DS install (-ds64 / Steam client launch args /
Steam library scan). User state lives under ~/.config/Magnetar/
(config.xml, plugin profiles, sources, caches, logs).

To remove the bundle while keeping your profiles and side-loaded
plugins, run ./uninstall.sh - it wipes ~/.local/share/Magnetar/ but
preserves config.xml, Sources/, Local/, and Profiles/ under
~/.config/Magnetar/.

Files
-----
  install.sh        Replaces ~/.local/share/Magnetar/ with Magnetar/.
  uninstall.sh      Removes binaries; preserves user state.
  README.txt        This file.
  Magnetar/         Install tree (deployed verbatim):
    MagnetarInterim.bin  The launcher apphost - run this in place of
                         SpaceEngineersDedicated.
    MagnetarConfig       Bash shim for the terminal configuration UI.
    Libraries/           Managed + native dependencies
                         (MagnetarInterim/ and the Compiler/ subprocess).
    Config/              MagnetarConfig publish output (Terminal.Gui).
EOF

# ---- pack -------------------------------------------------------------------

ARCHIVE_NAME="MagnetarForLinux.7z"
ARCHIVE_PATH="$OUTPUT_DIR/$ARCHIVE_NAME"

rm -f "$ARCHIVE_PATH"

echo
echo "==> Packing $ARCHIVE_NAME"
# -snl: store symlinks AS symlinks (the native library set may contain
# soname symlinks in future drops; preserve them so dlopen()'s inode-dedup
# doesn't load multiple copies at runtime).
( cd "$BUILD_DIR" && 7z a -t7z -snl -mx=9 -bso0 -bsp1 "$ARCHIVE_PATH" "MagnetarForLinux" >/dev/null )

echo
echo "Done: $ARCHIVE_PATH"
ls -lh "$ARCHIVE_PATH"
