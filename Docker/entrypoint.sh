#!/bin/bash
# Container entrypoint: install or update the dedicated server, create a
# first-start config when the instance has none, then run MagnetarInterim.
# Arguments are passed on to MagnetarInterim. A first argument that is not an
# option runs as a plain command instead (docker run ... bash).
set -euo pipefail

DATA_DIR="${DATA_DIR:-/data}"
DS_DIR="${DS_DIR:-$DATA_DIR/DedicatedServer}"
INSTANCE_DIR="${INSTANCE_DIR:-$DATA_DIR/Instance}"
CONFIG_DIR="${CONFIG_DIR:-$DATA_DIR/Magnetar}"
DS_UPDATE="${DS_UPDATE:-1}"
WORLD_TEMPLATE="${WORLD_TEMPLATE:-Star System}"
SERVER_NAME="${SERVER_NAME:-Magnetar}"
WORLD_NAME="${WORLD_NAME:-$WORLD_TEMPLATE}"

MAGNETAR=/opt/magnetar/MagnetarInterim.bin
DS_APPID=298740

log() { echo "[entrypoint] $*"; }

if [ $# -gt 0 ] && [ "${1#-}" = "$1" ]; then
  exec "$@"
fi

case "${1:-}" in
  -h|-help|--help) exec "$MAGNETAR" "$@" ;;
esac

if ! mkdir -p "$DS_DIR" "$INSTANCE_DIR" "$CONFIG_DIR" 2>/dev/null \
   || ! [ -w "$DS_DIR" ] || ! [ -w "$INSTANCE_DIR" ] || ! [ -w "$CONFIG_DIR" ]; then
  log "ERROR: uid $(id -u) cannot write the data folders under $DATA_DIR."
  log "Make the mapped folder writable for that uid, or pass --user <uid>:<gid> of its owner."
  exit 1
fi

# steamcmd and the Steam API look for their files under $HOME, which is
# missing or read-only when the container runs under an arbitrary uid.
if ! [ -w "${HOME:-/nonexistent}" ]; then
  export HOME=/tmp/home
  mkdir -p "$HOME"
fi
# The Steam game server API loads steamclient.so from ~/.steam/sdk64.
mkdir -p "$HOME/.steam/sdk64"
ln -sf /opt/steamcmd/linux64/steamclient.so "$HOME/.steam/sdk64/steamclient.so"

ds_exe="$DS_DIR/DedicatedServer64/SpaceEngineersDedicated.exe"
if [ "$DS_UPDATE" = 1 ] || ! [ -f "$ds_exe" ]; then
  # The DS has no Linux depot: install the Windows files, which run through
  # the Linux native wrappers. steamcmd exit codes are unreliable, so the
  # executable is the success marker, and a failed update of an existing
  # install only warns.
  log "Installing or updating the dedicated server in $DS_DIR"
  for attempt in 1 2 3; do
    /opt/steamcmd/steamcmd.sh \
      +@sSteamCmdForcePlatformType windows \
      +force_install_dir "$DS_DIR" \
      +login anonymous \
      +app_update "$DS_APPID" \
      +quit && break
    log "steamcmd attempt $attempt failed"
    sleep 5
  done
  if ! [ -f "$ds_exe" ]; then
    log "ERROR: the dedicated server is not installed in $DS_DIR"
    exit 1
  fi
fi

xml_escape() {
  local s="$1"
  s="${s//&/&amp;}"; s="${s//</&lt;}"; s="${s//>/&gt;}"
  printf '%s' "$s"
}

cfg="$INSTANCE_DIR/SpaceEngineers-Dedicated.cfg"
if ! [ -f "$cfg" ]; then
  # Without a config, the DS has no world to load and exits with "Premade world
  # not found". Start a new world from one of the DS templates instead; later
  # starts load the last saved session.
  template="$DS_DIR/Content/CustomWorlds/$WORLD_TEMPLATE"
  if ! [ -d "$template" ]; then
    log "ERROR: world template '$WORLD_TEMPLATE' not found. Available templates:"
    ls -1 "$DS_DIR/Content/CustomWorlds" >&2
    exit 1
  fi
  log "Creating $cfg for a new world from the '$WORLD_TEMPLATE' template"
  cat > "$cfg" <<XML
<?xml version="1.0" encoding="utf-8"?>
<MyConfigDedicated xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <SessionSettings>
    <GameMode>Survival</GameMode>
    <OnlineMode>PUBLIC</OnlineMode>
    <MaxPlayers>4</MaxPlayers>
  </SessionSettings>
  <IP>0.0.0.0</IP>
  <SteamPort>8766</SteamPort>
  <ServerPort>27016</ServerPort>
  <ServerName>$(xml_escape "$SERVER_NAME")</ServerName>
  <WorldName>$(xml_escape "$WORLD_NAME")</WorldName>
  <PremadeCheckpointPath>$(xml_escape "$template")</PremadeCheckpointPath>
  <RemoteApiEnabled>false</RemoteApiEnabled>
</MyConfigDedicated>
XML
fi

exec "$MAGNETAR" \
  -config "$CONFIG_DIR" \
  -ds64 "$DS_DIR/DedicatedServer64" \
  -path "$INSTANCE_DIR" \
  "$@"
