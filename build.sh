#!/usr/bin/env bash
# build.sh
#
# Magnetar Linux build orchestrator. Populates build/Libraries/ with the
# managed and native dependencies that Legacy.csproj's AfterBuild and
# AfterPublish targets copy next to the MagnetarInterim apphost.
#
# Nothing is compiled here. Every artefact is downloaded from a GitHub release
# of the repo that builds it, so the binaries are byte-for-byte the same ones
# Pulsar for Linux ships.
#
# Magnetar targets the Space Engineers Dedicated Server (headless), so it
# bundles:
#   * Steamworks.NET.dll              - managed Steamworks binding
#   * libsteam_api.so                 - Linux Steamworks SDK shared library
#   * libEOSSDK-Linux-Shipping.so     - Epic Online Services SDK; needed
#                                       because MySteamService.UpdateNetwork-
#                                       Thread drives MyEOSNetworking even
#                                       under Steam-only networking
#                                     all three from the
#                                     CometWorks/linux-dependencies release
#   * libHavok.so / libRecastDetour.so / libVRageNative.so
#                                     - PE-loader replacements for the
#                                       Windows native DLLs Keen ships;
#                                       downloaded from the
#                                       CometWorks/linux-native-wrappers release
#
# The linux-dependencies release also carries FFmpeg and DXVK for the game
# client; the dedicated server is headless and simply ignores them.
#
# Every library keeps a per-file environment override (see the staging section
# below), so a developer can still point the build at a locally supplied .so
# without touching either release.
#
# After this script runs, build:
#   dotnet build  -c Release Magnetar.sln
#   dotnet publish -c Release Legacy/Legacy.csproj -r linux-x64 --self-contained false
#
# Usage:
#   ./build.sh                  Build/refresh build/Libraries/ AND package
#                              dist/MagnetarForLinux.7z.
#   ./build.sh --deps-only      Build/refresh build/Libraries/ only.
#   ./build.sh --skip-deps      Skip dep staging; just package.
#   ./build.sh --clean          Wipe caches and rebuild from scratch.
#                              (Combine freely, e.g. `--clean --deps-only`.)
#
# Env-var overrides (defaults shown):
#   MAGNETAR_REPO_DIR = <dir of this script>
#   BUILD_DIR         = $MAGNETAR_REPO_DIR/build
#   LIBRARIES_DIR     = $BUILD_DIR/Libraries
#   OUTPUT_DIR        = $MAGNETAR_REPO_DIR/dist
#   LINUX_DEPS_DIR    = $BUILD_DIR/linux-deps  (extracted linux-dependencies)
#   LINUXCOMPAT_NATIVE = $BUILD_DIR/native     (extracted native wrappers)
#   DS64              = $HOME/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64
#
# Per-library overrides (full path to a .so / .dll; wins over the fetched
# release): STEAMWORKS_NET_DLL, LIBSTEAM_API_SO, LIBEOSSDK_SO, LIBHAVOK_SO,
# LIBRECASTDETOUR_SO, LIBVRAGENATIVE_SO
#
# To pin exact upstream releases (recommended for reproducible CI), set
# LINUX_DEPENDENCIES_TAG and NATIVE_WRAPPERS_TAG; see the fetch scripts.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPTS_DIR="$SCRIPT_DIR/Scripts"

MAGNETAR_REPO_DIR="${MAGNETAR_REPO_DIR:-$SCRIPT_DIR}"
BUILD_DIR="${BUILD_DIR:-$MAGNETAR_REPO_DIR/build}"
LIBRARIES_DIR="${LIBRARIES_DIR:-$BUILD_DIR/Libraries}"
OUTPUT_DIR="${OUTPUT_DIR:-$MAGNETAR_REPO_DIR/dist}"
DS64="${DS64:-$HOME/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64}"

export MAGNETAR_REPO_DIR BUILD_DIR LIBRARIES_DIR OUTPUT_DIR

CLEAN_ARGS=()
DO_DEPS=1
DO_PACKAGE=1
for arg in "$@"; do
    case "$arg" in
        --clean)      CLEAN_ARGS+=("--clean") ;;
        --deps-only)  DO_PACKAGE=0 ;;
        --skip-deps)  DO_DEPS=0 ;;
        -h|--help)    sed -n '2,61p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        *) echo "ERROR: unknown arg: $arg" >&2; exit 2 ;;
    esac
done

if [ "$DO_DEPS" = "1" ]; then

mkdir -p "$LIBRARIES_DIR/LICENSES"

# ---- 1. fetch the prebuilt dependencies ------------------------------------
#
# Two GitHub releases supply everything Magnetar bundles. Neither is compiled
# here; see the header comment for what each carries.

echo
echo "############################################################"
echo "# build: fetch linux-dependencies release"
echo "############################################################"
bash "$SCRIPTS_DIR/fetch_linux_dependencies.sh" "${CLEAN_ARGS[@]+"${CLEAN_ARGS[@]}"}"

echo
echo "############################################################"
echo "# build: fetch linux-native-wrappers release"
echo "############################################################"
bash "$SCRIPTS_DIR/fetch_native_wrappers.sh" "${CLEAN_ARGS[@]+"${CLEAN_ARGS[@]}"}"

LINUX_DEPS_DIR="${LINUX_DEPS_DIR:-$BUILD_DIR/linux-deps}"
LINUXCOMPAT_NATIVE="${LINUXCOMPAT_NATIVE:-$BUILD_DIR/native}"

# ---- 2. stage the libraries next to the apphost ----------------------------
#
# Every file is probed in the same order, most specific first:
#
#   1. its own env override        explicit path, wins over everything
#   2. <repo>/Vendor/<name>        a locally dropped file; Vendor/ is not
#                                  committed any more but the probe stays so a
#                                  developer can override without env vars
#   3. the fetched release cache   the normal path
#   4. any extra fallbacks passed by the caller (e.g. $DS64 for libsteam_api)
#
# The proprietary Steamworks and EOS runtimes used to be committed under
# Vendor/ and Steamworks.NET.dll used to be built by build_steamworks_net.sh.
# All three now arrive in the linux-dependencies release.

echo
echo "############################################################"
echo "# build: staging libraries -> $LIBRARIES_DIR"
echo "############################################################"

stage_file() {
    # stage_file <name> <mode> <override var name> [extra fallbacks...]
    local name="$1" mode="$2" env_name="$3"
    shift 3
    local env_override="${!env_name:-}"
    local src=""
    for candidate in "$env_override" "$MAGNETAR_REPO_DIR/Vendor/$name" "$@"; do
        if [ -n "$candidate" ] && [ -f "$candidate" ]; then
            src="$candidate"
            break
        fi
    done
    if [ -z "$src" ]; then
        echo "ERROR: $name not found." >&2
        echo "       Set $env_name=/path/to/$name, or drop the file at one of:" >&2
        echo "         $MAGNETAR_REPO_DIR/Vendor/$name" >&2
        for c in "$@"; do echo "         $c" >&2; done
        exit 1
    fi
    install -m "$mode" "$src" "$LIBRARIES_DIR/$name"
    echo "  copied $name from $src"
}

stage_file Steamworks.NET.dll 0644 STEAMWORKS_NET_DLL \
    "$LINUX_DEPS_DIR/Steamworks.NET.dll"

stage_file libsteam_api.so 0755 LIBSTEAM_API_SO \
    "$LINUX_DEPS_DIR/libsteam_api.so" \
    "$DS64/libsteam_api.so"

stage_file libEOSSDK-Linux-Shipping.so 0755 LIBEOSSDK_SO \
    "$LINUX_DEPS_DIR/libEOSSDK-Linux-Shipping.so"

stage_file libHavok.so 0755 LIBHAVOK_SO \
    "$LINUXCOMPAT_NATIVE/libHavok.so"

stage_file libRecastDetour.so 0755 LIBRECASTDETOUR_SO \
    "$LINUXCOMPAT_NATIVE/libRecastDetour.so"

stage_file libVRageNative.so 0755 LIBVRAGENATIVE_SO \
    "$LINUXCOMPAT_NATIVE/libVRageNative.so"

# ---- 3. Licenses ------------------------------------------------------------
#
# The licence texts ship inside the linux-dependencies archive, next to the
# binaries they cover, so they arrive with the fetch rather than being
# committed here. Redistributing the bundle without them is a licence
# violation, so a missing LICENSES/ is a hard error rather than a skip.
#
# Only the notices covering what Magnetar actually stages are copied. The
# archive is shared with Pulsar and also carries FFmpeg, DXVK and OpenAL
# texts; copying those wholesale would ship attribution for libraries that
# are not in the bundle, and LICENSES/README.txt (the archive's own index)
# would list files that are not there. STAGED_LICENSES is therefore derived
# from the staging list above, and a generated README explains the subset.

if [ ! -d "$LINUX_DEPS_DIR/LICENSES" ]; then
    echo "ERROR: $LINUX_DEPS_DIR/LICENSES is missing." >&2
    echo "       The linux-dependencies archive should carry the third-party" >&2
    echo "       licence texts; re-run with --clean to re-fetch it." >&2
    exit 1
fi

# Steamworks.NET.dll -> MIT text; libsteam_api.so -> Valve notice;
# libEOSSDK-Linux-Shipping.so -> Epic notice. The three native wrappers are
# MIT and carry their own text in the linux-native-wrappers release.
STAGED_LICENSES=(
    Steamworks.NET-LICENSE.txt
    Steam-NOTICE.txt
    EOS-NOTICE.txt
)

echo
echo "############################################################"
echo "# build: licenses (linux-deps/LICENSES/ -> Libraries/LICENSES/)"
echo "############################################################"
for name in "${STAGED_LICENSES[@]}"; do
    src="$LINUX_DEPS_DIR/LICENSES/$name"
    if [ ! -f "$src" ]; then
        echo "ERROR: $src is missing from the linux-dependencies archive." >&2
        echo "       Magnetar ships the library this notice covers, so the" >&2
        echo "       bundle cannot be built without it." >&2
        exit 1
    fi
    install -m 0644 "$src" "$LIBRARIES_DIR/LICENSES/$name"
    echo "  copied $name"
done

# Drop any notice a previous build staged that is no longer in the list, so
# trimming the set does not leave stale attribution behind.
shopt -s nullglob
for f in "$LIBRARIES_DIR/LICENSES"/*.txt; do
    keep=0
    for name in "${STAGED_LICENSES[@]}" README.txt; do
        [ "$(basename "$f")" = "$name" ] && keep=1
    done
    if [ "$keep" = "0" ]; then
        rm -f "$f"
        echo "  removed $(basename "$f") (covers a library Magnetar does not ship)"
    fi
done
shopt -u nullglob

cat > "$LIBRARIES_DIR/LICENSES/README.txt" <<'LICENSES_README'
Third-party licences for the binaries shipped in ../
====================================================

Magnetar is MIT-licensed. The libraries bundled alongside it are each
governed by their own licence, collected here.

    Steamworks.NET-LICENSE.txt MIT licence covering Steamworks.NET.dll
    Steam-NOTICE.txt           Attribution for libsteam_api.so
                               (proprietary, Valve Corporation)
    EOS-NOTICE.txt             Attribution for libEOSSDK-Linux-Shipping.so
                               (proprietary, Epic Games)

The native wrapper libraries (libHavok.so, libRecastDetour.so,
libVRageNative.so) are MIT-licensed and published by the
CometWorks/linux-native-wrappers repository.

Magnetar is the headless dedicated server and does not bundle the FFmpeg,
DXVK or OpenAL libraries that the shared CometWorks/linux-dependencies
release also carries, so their licence texts are deliberately absent.
LICENSES_README
echo "  wrote README.txt"

# ---- 4. final assertion ----------------------------------------------------

EXPECTED_FILES=(
    Steamworks.NET.dll
    libsteam_api.so
    libEOSSDK-Linux-Shipping.so
    libHavok.so
    libRecastDetour.so
    libVRageNative.so
    # Attribution for the proprietary runtimes and the managed binding.
    LICENSES/EOS-NOTICE.txt
    LICENSES/Steam-NOTICE.txt
    LICENSES/Steamworks.NET-LICENSE.txt
    LICENSES/README.txt
)

MISSING=0
for rel in "${EXPECTED_FILES[@]}"; do
    if [ ! -e "$LIBRARIES_DIR/$rel" ]; then
        echo "MISSING: $LIBRARIES_DIR/$rel" >&2
        MISSING=1
    fi
done
if [ "$MISSING" = "1" ]; then
    echo "ERROR: dependency staging is incomplete." >&2
    exit 1
fi

echo
echo "==> All expected artefacts present in $LIBRARIES_DIR"
( cd "$LIBRARIES_DIR" && ls -lh | sed 's/^/  /' )

fi  # DO_DEPS

# ---- 5. package the distributable bundle ----------------------------------
# Publishes Legacy framework-dependently, stages the bundle tree, and
# packs dist/MagnetarForLinux.7z. Skipped with --deps-only.

if [ "$DO_PACKAGE" = "1" ]; then
    if [ ! -d "$LIBRARIES_DIR" ]; then
        echo "ERROR: --skip-deps requested but $LIBRARIES_DIR is missing." >&2
        echo "       Run without --skip-deps once first." >&2
        exit 1
    fi

    echo
    echo "############################################################"
    echo "# package: MagnetarForLinux"
    echo "############################################################"
    bash "$SCRIPTS_DIR/package_magnetar_for_linux.sh"
fi
