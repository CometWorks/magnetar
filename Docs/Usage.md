# Usage

Run the `MagnetarLegacy` or `MagnetarInterim` executable from your Dedicated
Server installation in place of `SpaceEngineersDedicated.exe`. Magnetar resolves
the DS install, applies any preloader patches, loads enabled plugins, then hands
off to the dedicated server's own `Main`.

```sh
# Windows
%APPDATA%\Magnetar\MagnetarLegacy.exe
%APPDATA%\Magnetar\MagnetarInterim.exe

# Linux
~/.local/share/Magnetar/MagnetarInterim.bin
```

See **[Configuration](Configuration.md)** for the config/install directories, DS
detection, and environment variables.

## Command-line help

Pass `-help` (also `-h` or `--help`) to print the full list of options — Magnetar's
own switches, the plugin-loader switches shared with Pulsar, the telemetry-consent
switches, and the dedicated-server arguments Magnetar passes through — then exit
without starting the server. On Linux the help screen deliberately skips loading
the bundled native libraries, so it prints cleanly without startup noise.

The plugin-loader flags (`-profile`, `-safeMode`, `-bare`, `-sources`,
`-hardened`, `-multiInstance`, `-useHome`, `-lazyPreload`, `-stableLogs`,
`-noUpdate`, `-preRelease`, `-mkCheck`, `-debug`, `-debugMods`,
`-debugCompileAll`) are
Pulsar's own flags with Pulsar's semantics; Magnetar additionally forces the
headless defaults (`-noSplash -noPrompt -lazySteam`) internally, and Pulsar's
client-only options (splash, intro video, F12 menu, …) are accepted but
meaningless on a server.

`-github-token <token>` / `MAGNETAR_GITHUB_TOKEN` is accepted for compatibility
but currently has **no effect**: Pulsar's network layer does not support
authenticated GitHub requests yet. Magnetar logs a warning when it is supplied.

## Client companion mod

By default Magnetar auto-loads the Steam Workshop `MagnetarMod` client companion
so server-side PluginSdk features can open mission-screen popups on clients. Pass
`-noimplicitmod` to skip adding it and remove it from the active world mod list
for that run.

Magnetar only adds and downloads the companion when Space Engineers has
registered its Steam Workshop service. Without that service, Magnetar removes
the implicit mod, logs a warning, and continues loading the server; mission-screen
popups remain unavailable for that run. Other Workshop update failures are also
reported as warnings and do not abort startup.

## Legacy Workshop mods

Some early Space Engineers Workshop mods download as a single `*_legacy.bin`
archive instead of loose files. Magnetar expands those archives after Workshop
download/update into the same mod folder before the dedicated server loads
definitions or scripts. The expanded files live under the DS Workshop cache
selected by `-path` (`content/244850/<workshop-id>`).

## Telemetry and consent

Magnetar can send **anonymous** plugin usage statistics (the list of enabled
plugin IDs, including built-in compatibility plugins) to help prioritize
development. Nothing else is collected — no personal data, no account or Steam ID,
no IP address, no world or server content. Participation is **opt-in** and the
identity is a random anonymous instance ID stored locally (see
[Configuration → instance.id](Configuration.md#telemetry-consent-instanceid)).

On the **first interactive start**, Magnetar asks once (Y/N) and remembers the
answer. When there is no interactive terminal (e.g. running headless or under a
supervisor), telemetry stays **disabled** unless you opt in explicitly. Control it
with these flags:

| Flag | Effect |
| ---- | ------ |
| `-consent` | Enable sending anonymous usage statistics and remember the decision. |
| `-noconsent` | Disable sending statistics **for this run only** (does not change the stored decision). |
| `-withdraw-consent` | Ask the statistics server to erase this instance's data, delete the local instance ID, record the denial, then exit without starting the server. |

`-withdraw-consent` is best effort: if the statistics server cannot be reached,
telemetry is still disabled locally.

## Daemon mode

Pass `-daemon` to detach the process from its parent (typically
[Quasar](https://github.com/viktor-ferenczi/Quasar/releases)) at startup, so the
parent terminating does not take the server down with it.

* **Linux** — this is a `setsid()`: the process leaves the parent's session and
  process group (an explicit `kill -HUP <pid>` still reloads the config). When
  launched as a child it detaches in place, keeping the PID and inherited
  stdout/stderr so a managing parent keeps capturing the log stream until it
  exits. When the process is itself a process-group leader (e.g. a wrapper
  script that `exec`s it), `setsid()` is not permitted in place, so it re-execs
  a detached child and the original exits.
* **Windows** — it detaches from the inherited console.

When Magnetar detaches, it also writes `magnetar.pid` (PID + resolved data dir)
in its config dir so tools can find and verify the running instance; it is
removed on clean shutdown.

## Configuring the server (MagnetarConfig)

**MagnetarConfig** is a terminal UI bundled next to the launcher for editing and
operating **one** DS instance without hand-editing XML. Run it from the
installed bundle:

```sh
# Linux
~/.local/share/Magnetar/Config/MagnetarConfig

# Windows
%APPDATA%\Magnetar\Config\MagnetarConfig.exe
```

It binds to a `(-config, -path)` folder pair — the same pair Magnetar itself
runs with — and edits the DS files in place (atomic writes with `.bak`
backups): the global `SpaceEngineers-Dedicated.cfg`, each world's
`Sandbox_config.sbc` (session settings and mod list) and `LastSession.sbl`
(which world loads next). From the same UI you can create a world by copying a
DS template, delete a world, manage plugins/sources/profiles
(`Profiles/`, `Sources/sources.xml`), start/stop/reload the daemonized server
(status read from `magnetar.pid`), and read the game and Magnetar logs. All
edits **save automatically**.

Key flags: `-path <dir>` (DS data dir) · `-config <dir>` (Magnetar config dir) ·
`-magnetar <file>` (launcher to start/stop) · `-ds64 <dir>` (for world
templates) · `-netdriver` (portable terminal driver) · `-diag` (print a
headless read-only instance report and exit) · `-help`. Graceful stop and config
reload use SIGTERM/SIGHUP and are **Linux-only**; on Windows the server can only
be force-killed (with a data-loss warning). See the
[Config tool user manual](ConfigTerminal.md) for full usage, and the
[design and implementation notes](ConfigTerminalInternals.md) for internals.
