#!/usr/bin/env bash
# package_magnetar_for_linux.sh
#
# Builds a distributable Linux bundle for Magnetar - the Space Engineers
# Dedicated Server plugin loader.
#
# Magnetar is headless: no GUI, no DXVK, no Steam overlay, no XDG menu
# entry. The bundle just deploys the Interim apphost + managed deps +
# libsteam_api.so into the user's XDG data dir, and the user invokes it
# from their DS launch script.
#
# Output: dist/MagnetarForLinux.<YYYYMMDD>.<8-hex-git-hash>.7z
#
# Bundle layout (Magnetar/ is the staging tree; install.sh deploys it
# split between ~/.local/share/Magnetar/ for binaries and
# ~/.config/Magnetar/ for user-editable state, following XDG conventions):
#
#   MagnetarForLinux/
#   ├── install.sh              Copies Magnetar/{Interim, Bin/, *.dll, *.so}
#   │                           into ~/.local/share/Magnetar/. Warns if
#   │                           .NET 10 runtime is not installed.
#   ├── uninstall.sh            Removes ~/.local/share/Magnetar/ entirely
#   │                           and removes ~/.config/Magnetar/ contents
#   │                           EXCEPT user state:
#   │                             config.xml, Sources/, Local/, Profiles/.
#   ├── README.txt
#   └── Magnetar/               Staging tree, see install.sh.
#       ├── Magnetar               Convenience bash launcher (cd + exec
#       │                          Interim). Deploys to
#       │                          ~/.local/share/Magnetar/Magnetar
#       └── Bin/                   Framework-dependent publish output
#                                  (Interim apphost + managed deps +
#                                  Steamworks.NET.dll + libsteam_api.so).
#                                  Deploys to ~/.local/share/Magnetar/Bin/
#
# Usage:
#   ./package_magnetar_for_linux.sh [output_dir]
#
# Env-var overrides (defaults shown):
#   MAGNETAR_REPO_DIR=$HOME/dev/se1/Magnetar  (auto-detected from script location)
#   BUILD_DIR=$MAGNETAR_REPO_DIR/build        (gitignored staging area)
#   OUTPUT_DIR=$MAGNETAR_REPO_DIR/dist        (first positional arg overrides)
#
# Requirements: dotnet (.NET 10 SDK), 7z, git.

set -euo pipefail

# ---- configuration ----------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MAGNETAR_REPO_DIR="${MAGNETAR_REPO_DIR:-$(cd "$SCRIPT_DIR/.." && pwd)}"
BUILD_DIR="${BUILD_DIR:-$MAGNETAR_REPO_DIR/build}"
OUTPUT_DIR="${1:-${OUTPUT_DIR:-$MAGNETAR_REPO_DIR/dist}}"

MAGNETAR_CSPROJ="$MAGNETAR_REPO_DIR/Legacy/Legacy.csproj"
MAGNETAR_PUBLISH_DIR="$MAGNETAR_REPO_DIR/Legacy/bin/Release/net10.0/publish"
LIBRARIES_DIR="$BUILD_DIR/Libraries"

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
    echo "       Run ./build.sh first to stage Steamworks.NET.dll + libsteam_api.so." >&2
    exit 1
fi

mkdir -p "$BUILD_DIR" "$OUTPUT_DIR"

# ---- version info -----------------------------------------------------------

BUILD_DATE="$(date +%Y%m%d)"
GIT_HASH="$(cd "$MAGNETAR_REPO_DIR" && git rev-parse --short=8 HEAD)"

echo "==> Magnetar repo : $MAGNETAR_REPO_DIR (hash $GIT_HASH)"
echo "==> Build dir     : $BUILD_DIR"
echo "==> Output dir    : $OUTPUT_DIR"

# ---- build & publish --------------------------------------------------------
# Framework-dependent publish. The host must have .NET 10 installed; the
# bundle's apphost (Bin/Interim) discovers it via the standard
# FrameworkResolver search path. This keeps the bundle small and lets
# users debug/profile with their stock dotnet install.

echo
echo "############################################################"
echo "# publish: Legacy (framework-dependent)"
echo "############################################################"
rm -rf "$MAGNETAR_PUBLISH_DIR"
dotnet publish "$MAGNETAR_CSPROJ" \
    -c Release \
    --no-self-contained \
    -p:DebugType=None \
    -p:DebugSymbols=false

# Sanity check the publish output
for required in Interim Interim.dll Interim.deps.json Interim.runtimeconfig.json libsteam_api.so Steamworks.NET.dll; do
    if [ ! -e "$MAGNETAR_PUBLISH_DIR/$required" ]; then
        echo "ERROR: missing $required in $MAGNETAR_PUBLISH_DIR" >&2
        exit 1
    fi
done

# ---- stage ------------------------------------------------------------------
# Wipe the previous staging tree wholesale so leftover files can never
# end up in the .7z.

PKG_ROOT="$BUILD_DIR/MagnetarForLinux"
MAGNETAR_ROOT="$PKG_ROOT/Magnetar"
rm -rf "$PKG_ROOT"
mkdir -p "$MAGNETAR_ROOT/Bin"

echo
echo "==> Staging publish output -> Magnetar/Bin/"
cp -a "$MAGNETAR_PUBLISH_DIR/." "$MAGNETAR_ROOT/Bin/"

# ---- generate Magnetar/Magnetar launcher -----------------------------------
# Lives at ~/.local/share/Magnetar/Magnetar. Sets the working directory
# to the Bin dir (so libsteam_api.so is found via $ORIGIN / cwd) and
# exec's the apphost. The host's stock .NET 10 runtime is discovered by
# the apphost's normal FrameworkResolver search path.

cat > "$MAGNETAR_ROOT/Magnetar" <<'EOF'
#!/usr/bin/env bash
# Magnetar - convenience launcher for the dedicated-server plugin loader.
# Use this from your dedicated-server launch script in place of
# SpaceEngineersDedicated. The Interim apphost auto-detects the DS install
# (see DS64 env var / Steam library scan) and applies plugin patches
# before booting the server.
#
# Usage: ~/.local/share/Magnetar/Magnetar [extra Interim args]
#
# Env-var overrides honoured by Interim:
#   DS64                 Explicit path to DedicatedServer64
#   XDG_CONFIG_HOME      Overrides ~/.config base
#   MAGNETAR_SAFE_MODE   Set to 1 to skip preloader patches

set -euo pipefail

PKG_DIR="$(cd "$(dirname "$0")" && pwd)"
INTERIM="$PKG_DIR/Bin/Interim"

if [ ! -x "$INTERIM" ]; then
    echo "ERROR: Magnetar Interim binary not found at $INTERIM" >&2
    echo "Hint: run install.sh from the extracted MagnetarForLinux archive first." >&2
    exit 1
fi

cd "$PKG_DIR/Bin"
exec "$INTERIM" "$@"
EOF
chmod +x "$MAGNETAR_ROOT/Magnetar"

# ---- generate install.sh ----------------------------------------------------

cat > "$PKG_ROOT/install.sh" <<'EOF'
#!/usr/bin/env bash
# install.sh - Deploys the bundled Magnetar/ tree into
# ~/.local/share/Magnetar/ (Bin/ and Magnetar launcher). Warns (does not
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

if pgrep -x Interim >/dev/null 2>&1; then
    echo "ERROR: Interim is running. Stop it before deploying (pkill -x Interim)." >&2
    exit 1
fi

# ---- .NET 10 detection (host requirement) ---------------------------------
if ! command -v dotnet >/dev/null 2>&1; then
    echo "WARNING: 'dotnet' not in PATH. Magnetar requires .NET 10 runtime" >&2
    echo "         installed system-wide (Microsoft.NETCore.App 10.x)." >&2
elif ! dotnet --list-runtimes 2>/dev/null | grep -q '^Microsoft.NETCore.App 10\.'; then
    echo "WARNING: .NET 10 runtime not detected in 'dotnet --list-runtimes'." >&2
    echo "         Install Microsoft.NETCore.App 10.x before launching Magnetar." >&2
fi

# ---- copy binaries -> $DATA_DST -------------------------------------------
mkdir -p "$DATA_DST"
echo "==> Deploying binaries to $DATA_DST"

if [ -d "$SRC/Bin" ]; then
    rm -rf "$DATA_DST/Bin"
    cp -a "$SRC/Bin" "$DATA_DST/Bin"
    echo "  Replaced $DATA_DST/Bin"
fi

cp -f "$SRC/Magnetar" "$DATA_DST/Magnetar"
chmod +x "$DATA_DST/Magnetar"
echo "  Updated  $DATA_DST/Magnetar"

echo
echo "Done. Launch the dedicated server through Magnetar with:"
echo "    $DATA_DST/Magnetar"
EOF
chmod +x "$PKG_ROOT/install.sh"

# ---- generate uninstall.sh -------------------------------------------------

cat > "$PKG_ROOT/uninstall.sh" <<'EOF'
#!/usr/bin/env bash
# uninstall.sh - Wipes ~/.local/share/Magnetar/ entirely and scrubs the
# non-user-managed parts of ~/.config/Magnetar/. PRESERVES the user state:
#   - config.xml
#   - Sources/      (PluginHub source defs, cached builds)
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

if pgrep -x Interim >/dev/null 2>&1; then
    echo "ERROR: Interim is running. Stop it before uninstalling (pkill -x Interim)." >&2
    exit 1
fi

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
Server on Linux. This bundle ships the Interim apphost as a
framework-dependent .NET 10 publish; the .NET 10 runtime is required to
be installed system-wide on the host.

Prerequisites
-------------
- Space Engineers Dedicated Server installed (via Steam or steamcmd).
- .NET 10 runtime installed system-wide (Microsoft.NETCore.App 10.x).
- Outbound HTTPS to GitHub on first launch if you want PluginHub-listed
  plugins to be fetched and compiled automatically.

Quick start
-----------
1. Extract:
       7z x MagnetarForLinux.$BUILD_DATE.$GIT_HASH.7z
2. Deploy:
       cd MagnetarForLinux
       ./install.sh
3. Launch the dedicated server through Magnetar in place of
   SpaceEngineersDedicated:
       ~/.local/share/Magnetar/Magnetar -console

Magnetar auto-detects the DS install (DS64 env var override, Steam
client launch args, or Steam library scan). User state lives under
~/.config/Magnetar/ (config.xml, plugin profiles, caches).

To remove the bundle while keeping your profiles and side-loaded
plugins, run ./uninstall.sh - it wipes ~/.local/share/Magnetar/ but
preserves config.xml, Sources/, Local/, and Profiles/ under
~/.config/Magnetar/.

Files
-----
  install.sh        Deploys binaries to ~/.local/share/Magnetar/.
  uninstall.sh      Removes binaries; preserves user state.
  README.txt        This file.
  Magnetar/         Staging source tree:
    Magnetar           Bash launcher (cd + exec Bin/Interim).
                       Deploys to ~/.local/share/Magnetar/Magnetar.
    Bin/               Framework-dependent publish output (Interim
                       apphost, managed deps, Steamworks.NET.dll,
                       libsteam_api.so).
                       Deploys to ~/.local/share/Magnetar/Bin/.
EOF

# ---- pack -------------------------------------------------------------------

ARCHIVE_NAME="MagnetarForLinux.$BUILD_DATE.$GIT_HASH.7z"
ARCHIVE_PATH="$OUTPUT_DIR/$ARCHIVE_NAME"

rm -f "$ARCHIVE_PATH"

echo
echo "==> Packing $ARCHIVE_NAME"
# -snl: store symlinks AS symlinks (the publish output may contain
# soname symlinks for libsteam_api.so in future SDK drops; preserve them
# so dlopen()'s inode-dedup doesn't load multiple copies at runtime).
( cd "$BUILD_DIR" && 7z a -t7z -snl -mx=9 -bso0 -bsp1 "$ARCHIVE_PATH" "MagnetarForLinux" >/dev/null )

echo
echo "Done: $ARCHIVE_PATH"
ls -lh "$ARCHIVE_PATH"
