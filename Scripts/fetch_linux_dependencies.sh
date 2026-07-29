#!/usr/bin/env bash
# fetch_linux_dependencies.sh
#
# Downloads the prebuilt Linux library dependencies from a
# CometWorks/linux-dependencies GitHub release into a local cache folder that
# build.sh stages next to the apphost.
#
# Source: https://github.com/CometWorks/linux-dependencies
#
# Magnetar needs three things from that release:
#
#   Steamworks.NET.dll            managed binding, referenced by Shared.csproj
#   libsteam_api.so               Steamworks SDK runtime
#   libEOSSDK-Linux-Shipping.so   Epic Online Services SDK runtime, needed
#                                 because MySteamService.UpdateNetworkThread
#                                 drives MyEOSNetworking even under Steam-only
#                                 networking
#
# plus the licence texts, which are staged into Libraries/LICENSES/ so the
# attribution travels with the bundle.
#
# Steamworks.NET used to be compiled here by Scripts/build_steamworks_net.sh,
# and the two proprietary .so files had to be supplied by hand — dropped into
# Vendor/ locally, or downloaded in CI from the VENDOR_ARCHIVE_URL secret. All
# three now come from the linux-dependencies release, which Pulsar consumes
# too, so the binaries are guaranteed identical and a clean clone builds with
# no manual steps.
#
# The asset also carries the FFmpeg and DXVK libraries, which Magnetar does not
# bundle — the dedicated server is headless. They are simply left unused in the
# cache folder.
#
# Caching (under the gitignored build/ folder of this repo):
#
#   build/
#   ├── linux-deps/                 the extracted archive
#   └── linux-dependencies.stamp    release tag last staged (cache key)
#
# When the stamp matches the resolved release tag AND all needed outputs are
# present, the download is skipped. If the release API is unreachable but a
# cached copy is already present, that copy is reused.
#
# Usage:
#   ./fetch_linux_dependencies.sh           Download (or no-op if cached).
#   ./fetch_linux_dependencies.sh --clean   Force a fresh download.
#
# Env-var overrides (defaults shown):
#   LINUX_DEPENDENCIES_REPO = CometWorks/linux-dependencies
#   LINUX_DEPENDENCIES_TAG  = ""    (empty = latest release; set to pin a tag,
#                                    e.g. v1.0.1 — recommended for reproducible CI)
#   BUILD_DIR               = <repo>/build
#   LINUX_DEPS_DIR          = $BUILD_DIR/linux-deps  (where the archive lands)
#   GH_TOKEN / GITHUB_TOKEN          (optional; used only to raise the GitHub API
#                                    rate limit when resolving the latest tag)
#
# Requirements: curl, tar.

set -euo pipefail

# ---- top-of-file knobs ------------------------------------------------------

LINUX_DEPENDENCIES_REPO="${LINUX_DEPENDENCIES_REPO:-CometWorks/linux-dependencies}"
LINUX_DEPENDENCIES_TAG="${LINUX_DEPENDENCIES_TAG:-}"
ASSET_NAME="linux-dependencies.tar.gz"

# ---- configuration ----------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
BUILD_DIR_DEFAULT="$REPO_DIR/build"

BUILD_DIR="${BUILD_DIR:-$BUILD_DIR_DEFAULT}"
LINUX_DEPS_DIR="${LINUX_DEPS_DIR:-$BUILD_DIR/linux-deps}"
STAMP_FILE="$BUILD_DIR/linux-dependencies.stamp"

# Only the files Magnetar actually stages. The archive contains more (FFmpeg,
# DXVK); those are ignored rather than asserted, so a client-only change
# upstream cannot fail the server build.
EXPECTED_FILES=(
    Steamworks.NET.dll
    libsteam_api.so
    libEOSSDK-Linux-Shipping.so
    LICENSES/Steam-NOTICE.txt
    LICENSES/Steamworks.NET-LICENSE.txt
)

CLEAN=0
for arg in "$@"; do
    case "$arg" in
        --clean)   CLEAN=1 ;;
        -h|--help) sed -n '2,56p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
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

mkdir -p "$BUILD_DIR" "$LINUX_DEPS_DIR"

# ---- resolve the release tag ------------------------------------------------
# An explicit LINUX_DEPENDENCIES_TAG pins the release; otherwise ask the API
# for the latest one. A token (if present) only lifts the anonymous rate limit.

gh_api() {
    local url="$1"
    local -a auth=()
    local tok="${GH_TOKEN:-${GITHUB_TOKEN:-}}"
    [ -n "$tok" ] && auth=(-H "Authorization: Bearer $tok")
    curl -fsSL "${auth[@]+"${auth[@]}"}" -H "Accept: application/vnd.github+json" "$url"
}

TAG="$LINUX_DEPENDENCIES_TAG"
if [ -z "$TAG" ]; then
    echo "==> Resolving latest release of $LINUX_DEPENDENCIES_REPO"
    TAG="$(gh_api "https://api.github.com/repos/$LINUX_DEPENDENCIES_REPO/releases/latest" \
             | grep -oP '"tag_name"\s*:\s*"\K[^"]+' | head -1 || true)"
fi

# ---- cache check ------------------------------------------------------------

ALL_FILES_PRESENT=1
for f in "${EXPECTED_FILES[@]}"; do
    [ -e "$LINUX_DEPS_DIR/$f" ] || ALL_FILES_PRESENT=0
done

if [ "$CLEAN" != "1" ] && [ "$ALL_FILES_PRESENT" = "1" ] && [ -f "$STAMP_FILE" ]; then
    STAMPED="$(cat "$STAMP_FILE")"
    if [ -z "$TAG" ]; then
        # API unreachable: trust the already-cached copy rather than failing.
        echo "==> Could not resolve latest tag; reusing cached dependencies ($STAMPED)"
        exit 0
    fi
    if [ "$STAMPED" = "$TAG" ]; then
        echo "==> Cached dependencies match release $TAG; skipping download"
        ( cd "$LINUX_DEPS_DIR" && ls -1 "${EXPECTED_FILES[@]}" )
        exit 0
    fi
fi

if [ -z "$TAG" ]; then
    echo "ERROR: could not resolve a release tag for $LINUX_DEPENDENCIES_REPO" >&2
    echo "       and no cached copy is present in $LINUX_DEPS_DIR." >&2
    echo "       Check network access or pin LINUX_DEPENDENCIES_TAG." >&2
    exit 1
fi

# ---- download + extract -----------------------------------------------------

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT

URL="https://github.com/$LINUX_DEPENDENCIES_REPO/releases/download/$TAG/$ASSET_NAME"
echo "==> Downloading $URL"
curl -fSL "$URL" -o "$TMP_DIR/$ASSET_NAME"

# Wipe the cache dir rather than overlaying. tar only adds, so a release that
# renames or drops a file would otherwise leave the old one here, and build.sh
# copies every LICENSES/*.txt it finds into the bundle - meaning a licence text
# removed upstream would ship forever. Nothing else lives in this dir, so a
# plain wipe is safe (the native wrappers have their own cache dir).
echo "==> Extracting dependencies into $LINUX_DEPS_DIR"
rm -rf "${LINUX_DEPS_DIR:?}"/*
tar -xzf "$TMP_DIR/$ASSET_NAME" -C "$LINUX_DEPS_DIR"

MISSING=0
for f in "${EXPECTED_FILES[@]}"; do
    if [ ! -e "$LINUX_DEPS_DIR/$f" ]; then
        echo "ERROR: release $TAG asset $ASSET_NAME is missing $f" >&2
        MISSING=1
    fi
done
if [ "$MISSING" = "1" ]; then
    exit 1
fi

printf '%s\n' "$TAG" > "$STAMP_FILE"

echo
echo "==> Staged linux-dependencies ($TAG) into $LINUX_DEPS_DIR:"
( cd "$LINUX_DEPS_DIR" && ls -1 "${EXPECTED_FILES[@]}" )
