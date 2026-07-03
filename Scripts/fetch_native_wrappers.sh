#!/usr/bin/env bash
# fetch_native_wrappers.sh
#
# Downloads the prebuilt Linux native wrapper shared libraries
# (libHavok.so, libRecastDetour.so, libVRageNative.so) from a
# CometWorks/linux-native-wrappers GitHub release and installs them into a
# local native/ cache folder that build.sh stages next to the apphost.
#
# Source: https://github.com/CometWorks/linux-native-wrappers
#
# The wrappers used to be compiled from se-linux-compat sources (a Docker build
# in CI, a sibling checkout locally). They are now built once by that repo's CI
# and published as a linux-native-wrappers.tar.gz release asset; Magnetar and
# Pulsar both consume the same artifact so the binaries are guaranteed identical.
# The asset also carries libD3DCompiler.so, which Magnetar does not bundle — it
# is simply left unused in the cache folder.
#
# Caching (under the gitignored build/ folder of this repo):
#
#   build/
#   ├── native/                          the downloaded lib*.so files
#   └── linux-native-wrappers.stamp      release tag last staged (cache key)
#
# When the stamp matches the resolved release tag AND all three needed .so
# outputs are present, the download is skipped. If the release API is
# unreachable but a cached copy is already present, that copy is reused.
#
# Usage:
#   ./fetch_native_wrappers.sh           Download (or no-op if cached).
#   ./fetch_native_wrappers.sh --clean   Force a fresh download.
#
# Env-var overrides (defaults shown):
#   NATIVE_WRAPPERS_REPO = CometWorks/linux-native-wrappers
#   NATIVE_WRAPPERS_TAG  = ""     (empty = latest release; set to pin a tag,
#                                  e.g. v1.0.2 — recommended for reproducible CI)
#   BUILD_DIR            = <repo>/build
#   LINUXCOMPAT_NATIVE   = $BUILD_DIR/native   (where the .so files are staged)
#   GH_TOKEN / GITHUB_TOKEN       (optional; used only to raise the GitHub API
#                                  rate limit when resolving the latest tag)
#
# Requirements: curl, tar.

set -euo pipefail

# ---- top-of-file knobs ------------------------------------------------------

NATIVE_WRAPPERS_REPO="${NATIVE_WRAPPERS_REPO:-CometWorks/linux-native-wrappers}"
NATIVE_WRAPPERS_TAG="${NATIVE_WRAPPERS_TAG:-}"
ASSET_NAME="linux-native-wrappers.tar.gz"

# ---- configuration ----------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BUILD_DIR_DEFAULT="$REPO_DIR/build"

BUILD_DIR="${BUILD_DIR:-$BUILD_DIR_DEFAULT}"
NATIVE_DIR="${LINUXCOMPAT_NATIVE:-$BUILD_DIR/native}"
STAMP_FILE="$BUILD_DIR/linux-native-wrappers.stamp"

# The wrappers Magnetar bundles (the asset also ships libD3DCompiler.so, which
# only Pulsar for Linux needs).
EXPECTED_LIBS=(
    libHavok.so
    libRecastDetour.so
    libVRageNative.so
)

CLEAN=0
for arg in "$@"; do
    case "$arg" in
        --clean)   CLEAN=1 ;;
        -h|--help) sed -n '2,42p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        *) echo "ERROR: unknown arg: $arg" >&2; exit 2 ;;
    esac
done

# ---- preflight --------------------------------------------------------------

for tool in curl tar; do
    command -v "$tool" >/dev/null 2>&1 || {
        echo "ERROR: required tool not found in PATH: $tool" >&2
        exit 1
    }
done

mkdir -p "$BUILD_DIR" "$NATIVE_DIR"

# ---- resolve the release tag ------------------------------------------------
# An explicit NATIVE_WRAPPERS_TAG pins the release; otherwise ask the API for
# the latest one. A token (if present) only lifts the anonymous rate limit.

gh_api() {
    local url="$1"
    local -a auth=()
    local tok="${GH_TOKEN:-${GITHUB_TOKEN:-}}"
    [ -n "$tok" ] && auth=(-H "Authorization: Bearer $tok")
    curl -fsSL "${auth[@]}" -H "Accept: application/vnd.github+json" "$url"
}

TAG="$NATIVE_WRAPPERS_TAG"
if [ -z "$TAG" ]; then
    echo "==> Resolving latest release of $NATIVE_WRAPPERS_REPO"
    TAG="$(gh_api "https://api.github.com/repos/$NATIVE_WRAPPERS_REPO/releases/latest" \
             | grep -oP '"tag_name"\s*:\s*"\K[^"]+' | head -1 || true)"
fi

# ---- cache check ------------------------------------------------------------

ALL_LIBS_PRESENT=1
for lib in "${EXPECTED_LIBS[@]}"; do
    [ -f "$NATIVE_DIR/$lib" ] || ALL_LIBS_PRESENT=0
done

if [ "$CLEAN" != "1" ] && [ "$ALL_LIBS_PRESENT" = "1" ] && [ -f "$STAMP_FILE" ]; then
    STAMPED="$(cat "$STAMP_FILE")"
    if [ -z "$TAG" ]; then
        # API unreachable: trust the already-staged copy rather than failing.
        echo "==> Could not resolve latest tag; reusing cached wrappers ($STAMPED)"
        exit 0
    fi
    if [ "$STAMPED" = "$TAG" ]; then
        echo "==> Cached wrappers match release $TAG; skipping download"
        ( cd "$NATIVE_DIR" && ls -1 "${EXPECTED_LIBS[@]}" )
        exit 0
    fi
fi

if [ -z "$TAG" ]; then
    echo "ERROR: could not resolve a release tag for $NATIVE_WRAPPERS_REPO" >&2
    echo "       and no cached copy is staged in $NATIVE_DIR." >&2
    echo "       Check network access or pin NATIVE_WRAPPERS_TAG." >&2
    exit 1
fi

# ---- download + stage -------------------------------------------------------

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

URL="https://github.com/$NATIVE_WRAPPERS_REPO/releases/download/$TAG/$ASSET_NAME"
echo "==> Downloading $URL"
curl -fSL "$URL" -o "$TMP_DIR/$ASSET_NAME"

echo "==> Extracting native wrappers"
tar -xzf "$TMP_DIR/$ASSET_NAME" -C "$TMP_DIR"

echo "==> Staging native wrappers into $NATIVE_DIR"
for lib in "${EXPECTED_LIBS[@]}"; do
    if [ ! -f "$TMP_DIR/$lib" ]; then
        echo "ERROR: release $TAG asset $ASSET_NAME is missing $lib" >&2
        exit 1
    fi
    install -m 0755 "$TMP_DIR/$lib" "$NATIVE_DIR/$lib"
done

printf '%s\n' "$TAG" > "$STAMP_FILE"

echo
echo "==> Staged native wrappers ($TAG) into $NATIVE_DIR:"
( cd "$NATIVE_DIR" && ls -1 "${EXPECTED_LIBS[@]}" )
