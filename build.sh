#!/usr/bin/env bash
# build.sh
#
# Magnetar Linux build orchestrator. Populates build/Libraries/ with the
# managed and native dependencies that Legacy.csproj's AfterBuild and
# AfterPublish targets copy next to the MagnetarInterim apphost.
#
# Magnetar targets the Space Engineers Dedicated Server (headless), so it
# bundles:
#   * Steamworks.NET.dll              - built from rlabrecque/Steamworks.NET
#   * libsteam_api.so                 - Linux Steamworks SDK shared library
#                                       (proprietary blob; supply via Vendor/
#                                       or the $DS64 folder)
#   * libEOSSDK-Linux-Shipping.so     - Epic Online Services SDK; needed
#                                       because MySteamService.UpdateNetwork-
#                                       Thread drives MyEOSNetworking even
#                                       under Steam-only networking
#   * libHavok.so / libRecastDetour.so / libVRageNative.so
#                                     - PE-loader replacements for the
#                                       Windows native DLLs Keen ships;
#                                       downloaded from the
#                                       CometWorks/linux-native-wrappers release
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
#   DS64              = $HOME/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPTS_DIR="$SCRIPT_DIR/Scripts"

MAGNETAR_REPO_DIR="${MAGNETAR_REPO_DIR:-$SCRIPT_DIR}"
BUILD_DIR="${BUILD_DIR:-$MAGNETAR_REPO_DIR/build}"
LIBRARIES_DIR="${LIBRARIES_DIR:-$BUILD_DIR/Libraries}"
OUTPUT_DIR="${OUTPUT_DIR:-$MAGNETAR_REPO_DIR/dist}"
DS64="${DS64:-$HOME/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64}"
LICENSES_SRC="$SCRIPTS_DIR/Licenses"

export MAGNETAR_REPO_DIR BUILD_DIR LIBRARIES_DIR OUTPUT_DIR

CLEAN_ARGS=()
DO_DEPS=1
DO_PACKAGE=1
for arg in "$@"; do
    case "$arg" in
        --clean)      CLEAN_ARGS+=("--clean") ;;
        --deps-only)  DO_PACKAGE=0 ;;
        --skip-deps)  DO_DEPS=0 ;;
        -h|--help)    sed -n '2,32p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        *) echo "ERROR: unknown arg: $arg" >&2; exit 2 ;;
    esac
done

if [ "$DO_DEPS" = "1" ]; then

mkdir -p "$LIBRARIES_DIR/LICENSES"

# ---- 1. Steamworks.NET.dll --------------------------------------------------

echo
echo "############################################################"
echo "# build: Steamworks.NET"
echo "############################################################"
bash "$SCRIPTS_DIR/build_steamworks_net.sh" "${CLEAN_ARGS[@]}"

# ---- 2. libsteam_api.so (Linux Steamworks SDK shared library) --------------
#
# Search order:
#   1. $LIBSTEAM_API_SO        (explicit user override; full path)
#   2. <repo>/Vendor/libsteam_api.so
#   3. $DS64/libsteam_api.so   (will exist once Keen ships a native Linux DS)
#
# The shared library is part of the proprietary Steamworks SDK and cannot
# be committed to the public repo. Drop it under Vendor/ from your own
# Steamworks SDK download (sdk/redistributable_bin/linux64/libsteam_api.so)
# or supply LIBSTEAM_API_SO=/path/to/libsteam_api.so on the command line.

echo
echo "############################################################"
echo "# build: libsteam_api.so"
echo "############################################################"

STEAM_SO_SRC=""
for candidate in \
    "${LIBSTEAM_API_SO:-}" \
    "$MAGNETAR_REPO_DIR/Vendor/libsteam_api.so" \
    "$DS64/libsteam_api.so"; do
    if [ -n "$candidate" ] && [ -f "$candidate" ]; then
        STEAM_SO_SRC="$candidate"
        break
    fi
done

if [ -z "$STEAM_SO_SRC" ]; then
    echo "ERROR: libsteam_api.so not found." >&2
    echo "       Tried:" >&2
    echo "         \$LIBSTEAM_API_SO                                   = ${LIBSTEAM_API_SO:-(unset)}" >&2
    echo "         $MAGNETAR_REPO_DIR/Vendor/libsteam_api.so" >&2
    echo "         $DS64/libsteam_api.so" >&2
    echo "       Drop the Linux Steamworks SDK shared library at one of" >&2
    echo "       the above paths (e.g. into Vendor/) and re-run." >&2
    exit 1
fi

install -m 0755 "$STEAM_SO_SRC" "$LIBRARIES_DIR/libsteam_api.so"
echo "  copied libsteam_api.so from $STEAM_SO_SRC"

# ---- 2b. Linux compat native libraries --------------------------------------
#
# The dedicated server reaches into native code that on Windows lives in
# Havok.dll / RecastDetour.dll / VRage.Native.dll (PE-loaded by Keen) and
# EOSSDK-Shipping.dll (Epic SDK, called from MySteamService.UpdateNetworkThread
# even with Steam-only networking). Linux replacements have to be bundled
# next to the apphost so NativeLibraryPreloader.cs can dlopen them.
#
# Sources:
#   * EOSSDK:               Vendor/libEOSSDK-Linux-Shipping.so
#                          (the Epic SDK redistributable; drop it in Vendor/)
#   * Havok/RecastDetour/
#     VRageNative:         downloaded from the CometWorks/linux-native-wrappers
#                          GitHub release by fetch_native_wrappers.sh into
#                          build/native/ (see that script; the prebuilt asset
#                          replaces the old se-linux-compat Docker/source build)
#
# Per-library env overrides: $LIBEOSSDK_SO, $LIBHAVOK_SO, $LIBRECASTDETOUR_SO,
# $LIBVRAGENATIVE_SO. Otherwise probed under Vendor/ then the fetched
# build/native/ folder.

echo
echo "############################################################"
echo "# build: Linux-compat native libraries"
echo "############################################################"

# Download the prebuilt wrappers into build/native/ (cached by release tag).
bash "$SCRIPTS_DIR/fetch_native_wrappers.sh" "${CLEAN_ARGS[@]}"

LINUXCOMPAT_NATIVE="${LINUXCOMPAT_NATIVE:-$BUILD_DIR/native}"

stage_native() {
    local soname="$1"
    local env_override="$2"
    shift 2
    local src=""
    for candidate in "$env_override" "$MAGNETAR_REPO_DIR/Vendor/$soname" "$@"; do
        if [ -n "$candidate" ] && [ -f "$candidate" ]; then
            src="$candidate"
            break
        fi
    done
    if [ -z "$src" ]; then
        echo "ERROR: $soname not found." >&2
        echo "       Set the override env var or drop the file at one of:" >&2
        echo "         $MAGNETAR_REPO_DIR/Vendor/$soname" >&2
        for c in "$@"; do echo "         $c" >&2; done
        exit 1
    fi
    install -m 0755 "$src" "$LIBRARIES_DIR/$soname"
    echo "  copied $soname from $src"
}

stage_native libEOSSDK-Linux-Shipping.so "${LIBEOSSDK_SO:-}"

stage_native libHavok.so "${LIBHAVOK_SO:-}" \
    "$LINUXCOMPAT_NATIVE/libHavok.so"

stage_native libRecastDetour.so "${LIBRECASTDETOUR_SO:-}" \
    "$LINUXCOMPAT_NATIVE/libRecastDetour.so"

stage_native libVRageNative.so "${LIBVRAGENATIVE_SO:-}" \
    "$LINUXCOMPAT_NATIVE/libVRageNative.so"

# ---- 3. Licenses ------------------------------------------------------------

if [ -d "$LICENSES_SRC" ]; then
    echo
    echo "############################################################"
    echo "# build: licenses (Scripts/Licenses/ -> Libraries/LICENSES/)"
    echo "############################################################"
    shopt -s nullglob
    for f in "$LICENSES_SRC"/*.txt; do
        install -m 0644 "$f" "$LIBRARIES_DIR/LICENSES/$(basename "$f")"
        echo "  copied $(basename "$f")"
    done
    shopt -u nullglob
fi

# ---- 4. final assertion ----------------------------------------------------

EXPECTED_FILES=(
    Steamworks.NET.dll
    libsteam_api.so
    libEOSSDK-Linux-Shipping.so
    libHavok.so
    libRecastDetour.so
    libVRageNative.so
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
