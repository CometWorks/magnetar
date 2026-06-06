# Builds the se-linux-compat NativeWrappers shared libraries that Magnetar's
# Linux build needs (libHavok.so / libRecastDetour.so / libVRageNative.so).
#
# Build context MUST be the `NativeWrappers/` folder of a se-linux-compat
# checkout. The release workflow clones se-linux-compat at a pinned commit and
# passes that folder as the context.
#
# Extract just the .so files to ./native with BuildKit's local exporter:
#
#   docker build -f nativewrappers.Dockerfile \
#       -o type=local,dest=native <path-to>/se-linux-compat/NativeWrappers
#
# The wrappers are plain C++17 built with cmake + make (see the repo's
# NativeWrappers/Makefile and CMakeLists.txt).

# ---- build stage ------------------------------------------------------------
FROM ubuntu:24.04 AS build

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        build-essential \
        cmake \
        make \
        g++ \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY . .

# `make` runs `cmake --preset default` then `cmake --build --preset default`,
# writing libHavok.so / libRecastDetour.so / libVRageNative.so (and the unused
# D3DCompiler / DllLoader targets) into ./build.
RUN make

# Collect only the three libraries Magnetar bundles. Fail loudly if any is
# missing so a wrapper rename upstream can't silently ship an incomplete set.
RUN mkdir -p /out \
    && cp build/libHavok.so build/libRecastDetour.so build/libVRageNative.so /out/ \
    && ls -l /out

# ---- export stage -----------------------------------------------------------
# `-o type=local,dest=native` copies the contents of this scratch layer to the
# host, leaving just the three .so files in ./native.
FROM scratch AS export
COPY --from=build /out/ /
