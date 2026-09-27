# Docker

The Magnetar container image runs the Space Engineers 1 Dedicated Server on
Linux under `MagnetarInterim`. The image holds Magnetar, the .NET 10 runtime
and steamcmd. The dedicated server itself is not in the image: on every start
the container installs or updates it with steamcmd into the data volume, so a
new DS version needs no new image.

## Running

Everything the server keeps goes into one folder mapped to `/data`:

```sh
mkdir -p data
docker run -d --name magnetar \
  -p 27016:27016/udp -p 8766:8766/udp \
  -v "$PWD/data:/data" \
  ghcr.io/cometworks/magnetar:latest
docker logs -f magnetar
```

The first start downloads about 8 GB of dedicated server files. Later starts
only check for updates and take seconds.

Stop the server with a grace period. Magnetar saves the world on `SIGTERM`, and
Docker's default of 10 seconds can cut a large save short:

```sh
docker stop -t 120 magnetar
```

[`Docker/compose.yaml`](../Docker/compose.yaml) does the same with Docker
Compose, with `./data` next to the file:

```sh
cd Docker
docker compose up -d
```

### The data folder

| Path | Holds |
| ---- | ----- |
| `/data/DedicatedServer` | The dedicated server install, kept up to date by steamcmd. |
| `/data/Instance` | The DS data folder (`-path`): `SpaceEngineers-Dedicated.cfg`, `Saves/`, workshop mods, DS logs. |
| `/data/Magnetar` | The Magnetar config folder (`-config`): `config.xml`, plugin sources and profiles, `info.log`. |
| `/data/NuGet` | NuGet packages that plugins pull in when they compile. |

The container runs as uid 1000 by default, which owns the folder on most
single-user Linux hosts. If the folder belongs to someone else, pass the owner
with `--user <uid>:<gid>`. The container stops with an error when it cannot
write the folder.

### First start

When `/data/Instance` has no `SpaceEngineers-Dedicated.cfg`, the entrypoint
writes one that starts a new survival world from a DS template. The DS saves
the world into `Saves/` and loads it on later starts. After the first start,
edit the config and the world settings like on any other DS, either by hand or
with the config tool:

```sh
docker exec -it magnetar magnetar-config
```

`magnetar-config` is `MagnetarConfig` with the container's folders filled in.
Restart the container after editing so the server picks the changes up.

To bring an existing world instead, put the DS data folder (the one with
`SpaceEngineers-Dedicated.cfg` and `Saves/`) into `data/Instance` before the
first start.

### Environment variables

| Variable | Default | Effect |
| -------- | ------- | ------ |
| `DS_UPDATE` | `1` | `0` skips the steamcmd update on start. The DS is still installed when it is missing. |
| `WORLD_TEMPLATE` | `Star System` | DS template for the first-start world, a folder name under `Content/CustomWorlds` of the DS. |
| `SERVER_NAME` | `Magnetar` | Server name written into the first-start config. |
| `WORLD_NAME` | the template name | World name written into the first-start config. |
| `PULSAR_GITHUB_TOKEN` | | GitHub token for plugin downloads, see [Usage](Usage.md#github-token). |
| `DS_DIR`, `INSTANCE_DIR`, `CONFIG_DIR` | under `/data` | Move one of the three folders elsewhere, for example onto its own volume. |

The first-start variables only matter while no config exists.

### Arguments

Arguments after the image name go to `MagnetarInterim`, after the `-config`,
`-ds64` and `-path` options the entrypoint sets. For example, to opt in to
the anonymous plugin statistics:

```sh
docker run ... ghcr.io/cometworks/magnetar:latest -consent accept
```

`-help` prints the options without installing anything. A first argument that
is not an option runs as a command instead, so `docker run -it --rm <image> bash`
opens a shell.

### Ports

| Port | Use |
| ---- | --- |
| `27016/udp` | Game port (`ServerPort` in the DS config). |
| `8766/udp` | Steam port (`SteamPort`). |

The first-start config turns the DS Remote API off. To use it, enable it in the
config, set a security key, and publish its TCP port (8080 by default).

## Building the image

The Dockerfile is in the [`Docker`](../Docker) folder. By default it installs a
published release and checks the download against the digest GitHub shows for
the release asset:

```sh
docker build -t magnetar Docker
docker build -t magnetar:2.4.2.3 --build-arg MAGNETAR_VERSION=2.4.2.3 Docker
```

To package your own build instead, point the `magnetar` build context at the
deployed install tree, the folder with `MagnetarInterim.bin`:

```sh
docker build -t magnetar:dev \
  --build-arg MAGNETAR_SOURCE=local \
  --build-context magnetar="$HOME/.config/Magnetar" \
  Docker
```

## Published images

The [`Release`](../.github/workflows/release.yml) workflow builds the image
from the Linux bundle of each release build and runs `-help` in it as a smoke
test. Public releases also push it to the GitHub Container Registry as
`ghcr.io/cometworks/magnetar:<version>` and `:latest`. Draft and pull request
builds only build the image.

A new package on the GitHub Container Registry starts out private. After the
first push, an organization owner has to make the `magnetar` package public in
its package settings once, so that `docker pull` works without logging in.
