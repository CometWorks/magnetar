# Configuration

There are **three** distinct folders involved, each overridable on the command
line:

| Folder | Holds | Default | Override |
| ------ | ----- | ------- | -------- |
| **Magnetar config dir** | Magnetar's own config (`config.xml`), logs, preloader cache, telemetry `instance.id` | Windows: `<launcher dir>\MagnetarLegacy` or `<launcher dir>\MagnetarInterim` (named after the launcher, next to the binary; falls back to `MagnetarLegacy` if the named folder does not exist)<br>Linux: `$XDG_CONFIG_HOME/Magnetar` → `~/.config/Magnetar` | `-config <dir>` |
| **DS install dir** | The dedicated-server binaries (`DedicatedServer64/`) | Auto-detected (see below) | `-ds64 <dir>` |
| **DS data dir (AppData)** | `SpaceEngineers-Dedicated.cfg` **and the world saves** (`Saves/`) | Windows: `%APPDATA%\SpaceEngineersDedicated`<br>(`%APPDATA%` = roaming AppData) | `-path <dir>` |

## Command-line parameters that change folders

### `-config <dir>` — Magnetar's own config/log directory

Overrides where Magnetar stores its own configuration, logs, and the preloader
cache. A relative path resolves against the launcher's directory. This does
**not** affect the dedicated server's config or saves.

Each Magnetar startup creates a new timestamped `info_yyyyMMdd_HHmmssfff.log`
file in this directory and updates `info.current` with the active file name.
This matches the Dedicated Server's per-start log history: failed startup
attempts are preserved instead of being overwritten. The compiler AppDomain and
Quasar.Agent's PluginSdk log mirror both use the active file named by
`info.current`.

While a Magnetar-launched server is running it also writes a `magnetar.pid`
file here — the process id on the first line, the resolved DS data dir
(`-path`) on the second — and removes it on clean shutdown. `MagnetarConfig`
uses it to discover the instance and report server status; see the
[Config tool](ConfigTerminal.md#28-process-model-and-pid-file).

* **Install dir (default, where the launcher lives)**
  * Windows — the extracted `Magnetar\` tree next to the dedicated-server
    installation.
  * Linux — `$XDG_DATA_HOME/Magnetar`, falling back to `~/.local/share/Magnetar`.

### `-ds64 <dir>` — dedicated-server install location

Points Magnetar at the `DedicatedServer64/` folder containing
`SpaceEngineersDedicated.exe`. A relative path resolves against the launcher's
directory.

When not given, the DS install is auto-detected from the Steam registry
(Windows) or `~/.steam/steam/steamapps/common/SpaceEngineersDedicatedServer/DedicatedServer64`
(Linux), or any Steam library listed in `libraryfolders.vdf`.

### `-path <dir>` — DS data directory (AppData: config + world saves)

This is the **dedicated server's own** argument; Magnetar passes the full
command line through to it. It sets the server *instance/data* directory — the
folder holding `SpaceEngineers-Dedicated.cfg` and the `Saves/` worlds. Without
it, the server uses its default instance, `%APPDATA%\SpaceEngineersDedicated` on
Windows.

Workshop downloads for dedicated-server world mods also use this data root. Steam
stores them under `content/244850/<workshop-id>`; if Steam returns an early
`*_legacy.bin` package, Magnetar expands it in that folder before the server
loads definitions.

```sh
MagnetarInterim -path "D:\SE\MyServerInstance"
```

The directory **must already exist**. If it does not, Magnetar logs an error and
exits (it will **not** silently start on the default instance). Absolute paths
work on both platforms (`C:\...`, `/srv/...`); a relative path is resolved against
the DS binaries' folder, not the launcher.

> **Note.** The dedicated server only applies `-path` inside its
> `-console`/`-noconsole` startup branch, which Magnetar's headless launch
> normally skips. Magnetar handles this for you: when `-path` is present and you
> have not passed `-console`/`-noconsole` yourself, it appends `-console`
> automatically so the path takes effect. You do **not** need to pass a console
> flag.

#### `-console` / `-noconsole` (optional)

You do **not** need these to run headless — Magnetar already bypasses the
server's WinForms/Telerik configurator and starts it directly (with console
output enabled, equivalent to `-console`). They differ only in whether the
server, *when running interactively*, attaches to the parent console or
allocates a new console window; on a non-interactive host both are no-ops. Pass
one explicitly only if you want to override that default — e.g. **`-noconsole`**
to skip the console attach entirely when running under Quasar with `-daemon`
(which releases the console on Windows), so the server won't re-grab or pop a
console window. (When you pass `-noconsole` together with `-path`, the server
still applies the path — Magnetar only auto-appends `-console` when *no* console
flag is present.)

Related pass-through DS flags `-session:<path>` (selects which saved world to
load) and `-ignorelastsession` take effect with or without a console flag.

## Telemetry consent (instance.id)

Anonymous plugin-usage telemetry is **opt-in** (see
[Usage → Telemetry and consent](Usage.md#telemetry-and-consent) for what is sent
and the controlling flags). Two pieces of state live in the **Magnetar config dir**:

* **`instance.id`** — a random anonymous UUID created only when you grant consent.
  Its presence *is* the record that telemetry is enabled, and the first 20 hex
  characters of the UUID are the only identifier sent to the statistics server (no
  Steam ID or account is ever involved). Deleting this file disables telemetry;
  `-withdraw-consent` deletes it and also asks the server to erase the data.
* **`config.xml`** — records the human-visible decision in `DataHandlingConsent`
  (`true` / `false` / unset) and `DataHandlingConsentDate`. An accepted decision is
  only honored while its `instance.id` exists; a stored `true` with no `instance.id`
  is treated as undecided and you are prompted again.

## Environment variables

| Variable             | Effect                                                           |
| -------------------- | ---------------------------------------------------------------- |
| `MAGNETAR_SAFE_MODE` | When `1`, disables preloader patches for a one-off recovery run. |
| `MAGNETAR_GITHUB_TOKEN` | GitHub token used for GitHub API/archive downloads when `-github-token` is not supplied; use a classic token from https://github.com/settings/tokens/new with no permissions selected. |
| `XDG_CONFIG_HOME`    | Overrides the Magnetar config-dir base (Linux).                  |
| `XDG_DATA_HOME`      | Overrides the Magnetar install-dir base (Linux).                 |
| `DS64`               | Build-time override for the DS reference path.                   |

Build-time overrides are covered in full in **[Build.md](Build.md)**.
