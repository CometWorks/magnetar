# syntax=docker/dockerfile:1

# Magnetar container image: the Space Engineers 1 Dedicated Server on .NET 10,
# launched by MagnetarInterim. The server itself is not in the image: the
# entrypoint installs and updates it with steamcmd into the /data volume.
# See Docs/Docker.md.
#
# MAGNETAR_SOURCE picks where the Magnetar install tree comes from:
#   release  Download MagnetarForLinux-<MAGNETAR_VERSION>.7z from the GitHub
#            releases and check it against the digest GitHub publishes for it.
#            MAGNETAR_VERSION is a release version such as 2.4.2.3, or latest.
#   local    Copy an extracted install tree (the folder that holds
#            MagnetarInterim.bin) from the "magnetar" build context:
#            docker build --build-arg MAGNETAR_SOURCE=local \
#              --build-context magnetar=<install tree> Docker
ARG MAGNETAR_SOURCE=release

FROM ubuntu:24.04 AS bundle-release
ARG MAGNETAR_VERSION=latest
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl jq 7zip \
    && rm -rf /var/lib/apt/lists/*
RUN set -eu; \
    case "$MAGNETAR_VERSION" in \
      latest) release=latest ;; \
      ''|*[!0-9.]*) echo "Invalid MAGNETAR_VERSION: $MAGNETAR_VERSION" >&2; exit 1 ;; \
      *) release="tags/v$MAGNETAR_VERSION" ;; \
    esac; \
    curl -fsSL "https://api.github.com/repos/CometWorks/magnetar/releases/$release" -o /tmp/release.json; \
    asset='.assets[] | select(.name | test("^MagnetarForLinux-.*\\.7z$"))'; \
    url="$(jq -er "$asset | .browser_download_url" /tmp/release.json)"; \
    digest="$(jq -er "$asset | .digest" /tmp/release.json)"; \
    curl -fsSL "$url" -o /tmp/magnetar.7z; \
    echo "${digest#sha256:}  /tmp/magnetar.7z" | sha256sum --check --strict -; \
    7z x -bso0 -o/ /tmp/magnetar.7z; \
    test -x /Magnetar/MagnetarInterim.bin

FROM scratch AS bundle-local
COPY --from=magnetar . /Magnetar/

FROM bundle-${MAGNETAR_SOURCE} AS bundle

FROM mcr.microsoft.com/dotnet/runtime:10.0

ARG MAGNETAR_VERSION
LABEL org.opencontainers.image.title="Magnetar" \
      org.opencontainers.image.description="Space Engineers 1 Dedicated Server launcher and plugin loader" \
      org.opencontainers.image.source="https://github.com/CometWorks/magnetar" \
      org.opencontainers.image.licenses="MIT" \
      org.opencontainers.image.version="$MAGNETAR_VERSION"

# lib32gcc-s1: steamcmd is a 32-bit program. tini: PID 1 that forwards
# SIGTERM to Magnetar (graceful stop) and reaps the compiler child processes.
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl lib32gcc-s1 tini \
    && rm -rf /var/lib/apt/lists/*

# The base image ships a uid 1000 "ubuntu" user. Renamed, it becomes the
# default user, which owns bind-mounted folders on most single-user hosts.
RUN usermod -l magnetar -d /home/magnetar -m ubuntu && groupmod -n magnetar ubuntu

# steamcmd updates itself on its first run, so do that once here. It keeps
# writing into its own folder on every run, and the container may run under
# any uid (docker run --user), hence the world-writable folder.
RUN mkdir /opt/steamcmd \
    && curl -fsSL https://steamcdn-a.akamaihd.net/client/installer/steamcmd_linux.tar.gz \
       | tar -xz -C /opt/steamcmd \
    && (/opt/steamcmd/steamcmd.sh +quit || true) \
    && test -f /opt/steamcmd/linux64/steamclient.so \
    && chmod -R a+rwX /opt/steamcmd \
    && rm -rf /root/Steam /root/.steam

COPY --from=bundle /Magnetar/ /opt/magnetar/
COPY --chmod=755 entrypoint.sh /usr/local/bin/magnetar-entrypoint
COPY --chmod=755 magnetar-config /usr/local/bin/magnetar-config

USER magnetar
VOLUME /data
# Game port and Steam port, both UDP, the DS defaults.
EXPOSE 27016/udp 8766/udp
ENTRYPOINT ["tini", "--", "magnetar-entrypoint"]
