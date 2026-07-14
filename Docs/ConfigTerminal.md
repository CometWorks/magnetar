# MagnetarConfig — user manual

**MagnetarConfig** is a cross-platform terminal UI (Terminal.Gui, classic Turbo
Vision look) that configures **and operates** one Space Engineers 1 Dedicated
Server instance running under Magnetar. From a single screen you edit the
server's global config, each world's session settings and mod list, choose and
create worlds, manage Magnetar plugins / sources / profiles, start and stop the
daemonized server, and read its logs — editing the DS files in place with atomic
writes and `.bak` backups.

The tool is scoped to **exactly one** instance: the `(-config, -path)` folder
pair it is launched with — the same pair Magnetar itself runs with. It is *not*
a fleet manager; run a separate copy per instance.

This is the operator's guide. For the design and implementation — file formats,
architecture, data model, internal state machines, testing and packaging — see
**[ConfigTerminal — design and implementation notes](ConfigTerminalInternals.md)**.

## Table of contents

1. [What it does](#1-what-it-does)
2. [Running MagnetarConfig](#2-running-magnetarconfig)
3. [Screens and navigation](#3-screens-and-navigation)
4. [Editing settings](#4-editing-settings)
5. [Worlds](#5-worlds)
6. [Plugins, sources and profiles](#6-plugins-sources-and-profiles)
7. [Running the server](#7-running-the-server)
8. [Reading logs](#8-reading-logs)
9. [Files it edits and safety](#9-files-it-edits-and-safety)
10. [Platform notes](#10-platform-notes)
11. [Limitations](#11-limitations)

---

## 1. What it does

**It can:**

- **Open a DS instance** and show its state: server identity, ports, the world
  that loads next, the worlds on disk, and whether the server is running.
- **Edit the DS global config** — every field of the dedicated-server config
  (network, identity, MOTD, auto-restart/update, remote API, watchdog,
  anti-spam), the **access lists** (Administrators / Banned / Reserved /
  GroupID), and the **server password** (PBKDF2-hashed exactly like the DS;
  never stored in plaintext).
- **Edit per-world session settings** (~180 options) and the per-world **mod
  list** (ordered, with the `IsDependency` flag) in each world's
  `Sandbox_config.sbc`, plus the **new-world defaults** template embedded in the
  cfg (used only for worlds created later; existing worlds keep their own
  settings).
- **Choose, create and delete worlds** — activate which world loads next, create
  a new world by copying one of the DS's own templates (no server start
  required, editable immediately), or delete a world.
- **Start / stop / restart / reload** the daemonized Magnetar instance bound to
  this configuration, with live PID-file status (state, PID, uptime) on the
  dashboard and status bar.
- **Manage Magnetar plugins** — enable/disable local DLLs and registered dev
  folders, browse the hub/remote plugin catalogs (read offline from Magnetar's
  own caches), manage the catalog **sources**, and manage named **profiles** of
  the enabled-plugin set.
- **Read logs** — browse and follow the game log and Magnetar's timestamped
  `info_*.log` files.

**It does not:**

- Manage more than one instance (no fleet management) — one `(config, data)`
  pair per invocation. (Use Quasar if you need to manage multiple servers.)
- Fetch or refresh hub catalogs over the network — it **reads** what Magnetar
  has already cached; add a source, then start the server once so Magnetar
  populates it ([§6](#6-plugins-sources-and-profiles)).
- Manage mods in a plugin profile — a dedicated server's mods belong to the
  world, edited **per-world** in `Sandbox_config.sbc` ([§5](#5-worlds)), not in a
  shared plugin profile.
- Edit world content (`Sandbox.sbc` entities / players / factions) — it only
  ever *reads* it as a display fallback, never writes it.
- Operate remotely — it is a local tool working on the local filesystem.

---

## 2. Running MagnetarConfig

Run it from the installed bundle, next to the launcher:

```sh
# Linux
~/.local/share/Magnetar/MagnetarConfig

# Windows
%APPDATA%\Magnetar\MagnetarConfig.bat
```

### Command-line options

```
MagnetarConfig [options]

  -path <dir>        DS data directory (config + Saves). Same semantics as the
                     DS/Magnetar; must exist. Default (Linux):
                     ~/.config/SpaceEngineersDedicated
  -config <dir>      Magnetar config directory (Magnetar's config.xml, logs,
                     magnetar.pid). Same semantics as Magnetar's own -config.
                     Default (Linux): ~/.config/Magnetar
  -magnetar <file>   Magnetar launcher executable to start/stop. Default:
                     ~/.local/share/Magnetar/MagnetarInterim (Linux); on Windows
                     chosen at startup between the installed MagnetarLegacy.exe
                     (.NET Framework 4.8) and MagnetarInterim.exe (.NET 10) — see
                     the resolution order below
  -ds64 <dir>        DedicatedServer64 folder (for world templates). Default:
                     auto-detected like Magnetar (Steam registry / library
                     folders / ~/.steam default path)
  -netdriver         force Terminal.Gui's NetDriver (portable fallback when
                     curses/terminfo is broken over e.g. exotic SSH terminals)
  -diag              print a headless read-only diagnostics report for the
                     resolved instance (paths, cfg summary, worlds, active
                     world, templates, plugins, sources, profiles, server
                     status, problems) and exit
  -help / -h / --help
```

The `(-config, -path)` **pair defines the instance** the tool is bound to for
the whole session — the same pair Magnetar itself is launched with, so
editing, PID/status, logs and launching all resolve against it. Running several
Magnetar instances on one machine means separate pairs and separate
`MagnetarConfig` invocations; the tool never touches other instances.

Use **`-diag`** for a quick, scriptable status report without opening the UI.

### How the instance is resolved

1. **Explicit arguments** win. A directory you name that does not exist is an
   error (the tool never silently falls back past a value you gave).
2. **On Windows**, when neither `-magnetar` nor `-config` is given and **both**
   launchers are installed under `%APPDATA%\Magnetar`, a startup prompt asks
   which one to configure — `MagnetarLegacy.exe` (.NET Framework 4.8) or
   `MagnetarInterim.exe` (.NET 10). It is auto-selected when only one is present.
3. **When neither `-path` nor `-config` is given**, the tool opens the
   **instance picker** — four editable path fields (DS data dir, Magnetar config
   dir, launcher, DS install), each with a **Browse** button, pre-filled with the
   resolved platform defaults / auto-detection. The DS data dir must exist for
   the instance to open. Reach it again any time from `File → Open Instance…`.

The tool keeps a tiny per-instance settings file, `ConfigTerminal.xml`, next to
Magnetar's `config.xml` in the selected config dir (currently just the folder
the plugin-manifest picker last used). It is created on first run and tolerates
a missing or corrupt file without failing.

---

## 3. Screens and navigation

One window at a time sits on the Turbo-Vision desktop, with a menu bar on top
and a status bar (F-key hints + live server status) at the bottom:

```
┌ File ─ Server ─ Worlds ─ Plugins ─ Tools ─ Help ────────────────────────┐
│▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒│
│▒▒╔═[■]═ Worlds — /home/se/instance ═══════════════════════════════╗▒▒▒▒▒│
│▒▒║ Name             Last saved         Mods  Size    Config       ║▒▒▒▒▒│
│▒▒║▶Red Ship         2026-07-10 22:14   12    1.2 GB  ok    ◀ACTIVE║▒▒▒▒▒│
│▒▒║ Creative Test    2026-06-01 10:03    0    250 MB  missing      ║▒▒▒▒▒│
│▒▒║ …                                                              ║▒▒▒▒▒│
│▒▒╚════════════════════════════════════════════════════════════════╝▒▒▒▒▒│
│▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒│
│ F1 Help│F3 Worlds│F4 Logs│F5 Start/Stop│F7 Settings│F8 Plugins│F10 Quit │
│ ● RUNNING pid 41230 up 2:14:07 — Red Ship                               │
└─────────────────────────────────────────────────────────────────────────┘
```

The bottom status line shows the live server state: `● RUNNING pid … up …`,
`○ STOPPED`, `◌ STARTING…`, or `! STALE PID FILE`.

### Keyboard

`F1` help/about · `F3` worlds · `F4` logs · `F5` start/stop server
(context-sensitive) · `F7` server settings · `F8` plugins (Local & Dev Folders)
· `F10` quit · `Tab`/arrows to move. Within the **Worlds** view, `F6` activates
the selected world. Editing **saves automatically** — there is no Save key. Every
function is also on the Alt-hotkey menus (`File` / `Server` / `Worlds` /
`Plugins` / `Tools` / `Help`; the `Server` menu carries Start / Stop / Restart /
Reload Config). The mouse works too.

### The screens

| View | What it is | Reached via |
| --- | --- | --- |
| **Instance picker** (dialog) | four path fields — DS data dir (`-path`), Magnetar config (`-config`), launcher (`-magnetar`), DS install (`-ds64`) — each with a **Browse** button and pre-filled with resolved defaults; the DS data dir must exist | startup without `-path`/`-config`, `File → Open Instance…` |
| **Dashboard** | server status (state/PID/uptime) with **Start / Stop / Restart / Reload** buttons; server name/ports/network type; active world; last lines of the active log; warnings (problems, experimental settings, `LoadWorld` set, `IgnoreLastSession` true, stale PID file) | on open |
| **Server Settings** | the DS global config: category list (left) + scrollable field form (right), a **Filter** box, per-field hint + default + restart/live badge | `Server → Settings` (F7) |
| **Access Lists** | three tabs — Administrators / Banned / Reserved — plus the GroupID field; add/remove/paste SteamIDs with validation | `Server → Access Lists` |
| **Password** (dialog) | set / clear the server password; two-field confirm; shows only whether a password is set | `Server → Server Password…` |
| **New-World Defaults** | session-settings form bound to the cfg's `<SessionSettings>`; banner: *"Template for newly created worlds — existing worlds keep their own settings"* | `Server → New World Defaults` |
| **Worlds** | the worlds table (above) with **Settings / Mods / Activate (F6) / Refresh / New World / Delete** | `Worlds → List (Saves)` (F3) |
| **World Settings** | session-settings form bound to that world's `Sandbox_config.sbc`; same categories/filter; experimental badges | Worlds → **Settings** |
| **Activate World** (confirm) | shows exactly what will be written (`LastSession.sbl`, an `IgnoreLastSession` flip if set, a `LoadWorld` clear if set); notes that a `-session:` on the command line overrides it | Worlds → **Activate** (F6) |
| **Delete World** (confirm) | Keep/Delete (defaults to Keep); removes the folder and clears a now-dangling `LastSession.sbl` / cfg `LoadWorld` | Worlds → **Delete** |
| **New World wizard** | template list → world name (validated) → confirm → copies the template into `Saves/<name>` and activates it, no server start | `Worlds → New World…` |
| **Log viewer** | file selector (game + Magnetar logs, active file marked); read-only text pane with "Game ready"/"Exception" lines highlighted; `/` find (case / whole-word toggles), `n`/`N` next/prev match, `Esc` clear search, `[`/`]` prev/next highlighted line, `End` follow, `Home` top, `W` wrap, `R` re-read | `Tools → Logs` (F4) |
| **Hub Plugins** | the cached hub/remote catalog **plus registered dev folders** (`- dev folder` suffix); Space/Enter toggles enabled (hub deps pulled in), with an author/tagline/description pane and a filter box | `Plugins → Hub` |
| **Plugin Profiles** | saved-preset list (the one matching the active set is marked); Load, Save As New, Update, Rename, Delete | `Plugins → Profiles` |
| **Local & Dev Folders** | local DLLs from `Local/` (Space toggles) and **registered** dev folders (Add picks a manifest `.xml`; Remove unregisters — registering does not enable, you toggle under Hub) | `Plugins → Local & Dev Folders` (F8) |
| **Plugin Sources** | manage the hub / remote-plugin / local-hub catalog sources: Add, Space toggles enabled, Remove | `Plugins → Sources` |
| **Mods** (per-world) | ordered mod list for the selected world: Add (Workshop id **or URL**, name auto-resolved), Del, Up/Down reorder, Toggle Dependency | `Worlds → Mods` |
| **Help / About** | key reference, files edited, new-world note | F1 / `Help → About` |

---

## 4. Editing settings

The Server Settings, New-World Defaults and World Settings screens all use the
same **option form**. Given a set of options and a config file:

- A **Filter** box on top, then a two-pane layout: a category list on the left
  and a scrollable field form on the right, rebuilt per category. The filter
  narrows to fields whose label, XML name or help text match — across **all**
  categories while it is non-empty.
- Field widgets follow the option type: checkboxes for booleans, radio groups or
  combo boxes for enums, validated text fields for numbers (with a Min/Max hint),
  a multi-line editor for MOTD and the manual-action message, and a small
  key/value table for block-type limits.
- Each row shows a status glyph: `○` = the field is absent from the file and
  showing the DS default (a value that is present shows no marker), `↕` =
  live-reloadable, `▲` = the current value triggers experimental mode. A value
  you type that will not parse is shown **red** and kept **out** of the file
  until it becomes valid — a half-typed value is never written.
- The bottom hint bar shows the focused option's help, default, XML name, and
  whether it applies live or needs a server restart.

**There is no Save key.** Edits are flushed automatically — on a roughly
one-second tick, when you switch screens, and on quit. A flush validates first
and writes atomically (a `.bak` is made once per session before the first
overwrite). If a screen still has invalid (red) fields when you leave it or quit,
you are prompted rather than losing them silently. See the
[save-pipeline state machine](ConfigTerminalInternals.md#9-state-machines) for
the full flow.

---

## 5. Worlds

The **Worlds** view (F3) lists everything under the instance's `Saves/` folder
with its name, last-save time, mod count, size and whether it has a
`Sandbox_config.sbc`. The active world (the one the server loads next) is marked.

- **Settings** / **Mods** — open that world's session-settings form or its mod
  list ([§4](#4-editing-settings), and mods below).
- **Activate** (F6) — make this world the one the server loads next. A confirm
  dialog lists exactly what it writes: `Saves/LastSession.sbl`, and — only if
  needed — flipping `IgnoreLastSession` to false and clearing a stale
  `LoadWorld`. It takes effect on the next server start; a `-session:` on the
  launch command line would override it.
- **New World…** — a wizard: pick a DS template, enter a name (rejected up front
  if it collides with an existing folder or world name), confirm. The tool
  **copies the template folder** into `Saves/<name>`, stamps the name into its
  `Sandbox_config.sbc`, and activates it — **no server start required**, and the
  world is editable immediately. (This needs the DS install for its templates; if
  it can't be found, the wizard says so.)
- **Delete** — removes a world folder behind a Keep/Delete confirmation
  (defaults to Keep) and clears a now-dangling `LastSession.sbl` / `LoadWorld`.
- **Refresh** — re-scan `Saves/` to pick up changes made outside the tool.

### Mods (per world)

The **Mods** button opens the ordered mod list for the selected world, stored in
its `Sandbox_config.sbc` (load order = list order):

- **Add** accepts a Workshop **id or URL** (paste several at once); the tool
  looks up each mod's name for you, expands a pasted collection into its members,
  and skips non-mods. If the Workshop is unreachable the ids are still added,
  just without names.
- **Del**, **Up/Down** to reorder, and **Toggle Dependency** to flip the
  `IsDependency` flag.

Mods are deliberately **per world**, not part of a plugin profile — a dedicated
server's mod set belongs to the world.

---

## 6. Plugins, sources and profiles

MagnetarConfig edits the same plugin config Magnetar reads —
`Profiles/Current.xml` (the active enabled set) and `Sources/sources.xml`
(the catalog sources) — in place, preserving fields Magnetar manages itself.
Changes apply the next time the server starts.

- **Local & Dev Folders** (F8) — toggle **local DLLs** from the instance's
  `Local/` folder, and **register dev folders** by picking a plugin manifest
  `.xml` (Quasar-style). Registering only makes a folder *available*; you enable
  it from the Hub Plugins view.
- **Hub Plugins** — browse the plugins offered by the configured hub/remote
  sources **plus** your registered dev folders (marked `- dev folder`), with an
  author/tagline/description pane. Toggling a hub plugin on pulls in its declared
  dependencies automatically.
- **Plugin Sources** — add / remove / enable the hub catalogs (e.g. MagnetarHub),
  single remote plugin repos, and local manifest folders.
- **Plugin Profiles** — named presets of the enabled set, stored one file per
  profile under `Profiles/`. **Load** applies a preset to the active set,
  **Save As New** snapshots the current set, and **Update** / **Rename** /
  **Delete** manage existing ones. The preset matching the current active set is
  marked.

> **Hub catalogs are read offline.** The tool shows whatever Magnetar has already
> cached under `Sources/Hubs/` and `Sources/Plugins/`. After you add a **new**
> source it shows no plugins until you start the server once and let Magnetar
> fetch it — the Hub Plugins view says so.

---

## 7. Running the server

The dashboard and the `Server` menu (and F5) start and stop the daemonized
Magnetar instance bound to this `(-config, -path)` pair — always running exactly
one world at a time, exactly as the DS would. Status (state, PID, uptime) comes
from the `magnetar.pid` file that the launcher writes, verified against the live
process, and refreshes automatically.

- **Start** — spawns the launcher daemonized in the background and waits for it
  to come up; an early exit is shown with the tail of its log. Start is refused
  while the server is already running.
- **Stop** — **Linux:** sends SIGTERM so Magnetar saves the world and quits;
  after a grace period it offers a **force-kill** behind an explicit
  "may lose progress since last save" warning. **Windows:** there is no safe stop
  signal, so Stop offers only the confirmed force-kill.
- **Restart** — Stop then Start with the same launch settings.
- **Reload Config** (Linux) — sends SIGHUP so Magnetar saves and reloads the
  config live. Only some fields apply live (MOTD, access lists, the browser
  server name); everything else needs a restart, and the option form marks which
  is which. After you edit a live-reloadable field with the server running, use
  `Server → Reload Config` to push it — it is not automatic.

Config editing is **not** locked while the server runs: the DS reads the cfg only
at startup, so settings changes simply take effect on the next start.

---

## 8. Reading logs

`Tools → Logs` (F4) opens the log viewer over two groups: the **game log**
(`SpaceEngineersDedicated*.log` in the DS data dir) and **Magnetar's logs**
(`info_*.log` in the config dir, the active one marked). Pick a file on the left;
the pane on the right shows its tail.

- `End` toggles follow (tail -f), `Home` jumps to the top, `W` toggles line
  wrap, `R` re-reads the window.
- `/` opens a **Find** dialog — a search term plus two toggles, **Case sensitive**
  and **Whole words only** — and jumps to the first match; `n` / `N` move to the
  next / previous match, wrapping around the window. `Esc` cancels the search and
  brings the default key hints back. Starting a search stops follow so it doesn't
  snap you back to the tail.
- `[` / `]` jump to the previous / next **highlighted** line (see below), wrapping
  around the window — a quick way to step between "Game ready" and "Exception"
  events without scrolling.

Lines are colour-highlighted as they scroll by: the DS **"Game ready…"** readiness
line (black on green) and any **"Exception"** line (yellow on red), so a world
coming up or a fault stands out without reading every line.

The reader is windowed, so it stays responsive even on multi-gigabyte logs — search
runs over the loaded window, not the entire file on disk.

---

## 9. Files it edits and safety

The tool writes only these files, and always in place:

- `SpaceEngineers-Dedicated.cfg` — the DS global config (settings, access lists,
  password, new-world defaults).
- `Saves/<world>/Sandbox_config.sbc` — per-world session settings and mod list.
- `Saves/LastSession.sbl` — which world loads next.
- `Profiles/*.xml` and `Sources/sources.xml` — Magnetar plugin config.
- `ConfigTerminal.xml` — the tool's own small settings file.

Every write is **atomic** (write to a temp file, then rename) and makes a `.bak`
copy once per session before the first overwrite of a file. Edits are
**upserts**: only the fields you actually change are touched, and unknown
elements, comments and untouched values are preserved — so a newer game version's
fields are never dropped, and a field you never set stays at the DS default.
Reads are tolerant: a missing file falls back to defaults, and a malformed one
opens read-only with the parse error surfaced (and a restore-from-`.bak` offer).

It never writes `Sandbox.sbc` (world content) — that is only read as a display
fallback.

---

## 10. Platform notes

- **Linux** is the primary platform and has the full feature set, including
  graceful stop (SIGTERM) and live config reload (SIGHUP).
- **Windows** has Start, live status, config/world editing, plugins and logs
  working the same way; what differs is **stopping** — there is no safe stop
  signal for a detached process, so the server can only be **force-killed** (with
  a data-loss warning), and there is no live config reload. When both launchers
  are installed you are asked which to configure ([§2](#2-running-magnetarconfig)).
- The UI uses only the classic 16-color palette, so it renders the same over a
  Linux terminal, Windows Terminal and legacy conhost. If a terminal misbehaves
  (e.g. a broken terminfo over SSH), start with `-netdriver`.

---

## 11. Limitations

- **One instance per session** — no fleet management; run a copy per instance.
- **No graceful stop on Windows** — force-kill only (see [§10](#10-platform-notes)).
- **Hub catalogs are offline** — the tool shows only what Magnetar has already
  cached; a newly added source is empty until the server runs once
  ([§6](#6-plugins-sources-and-profiles)).
- **External changes aren't watched live** — if the running DS or another editor
  changes a world file while you have it open, use **Refresh** or re-open the
  screen to pick it up. Auto-save is content-compared, so it will not overwrite a
  change you did not make.
- **PID-file status needs a current launcher** — an older Magnetar started
  outside the tool writes no `magnetar.pid`; status then falls back to a
  best-effort process scan. The tool ships alongside the launcher that writes it.
- **Log viewer** search and highlighting operate over the loaded tail window
  (not the whole file on disk), and there is no exception-trace (stack-frame)
  navigation — only plain next/previous match.

For the full list of known limitations, design trade-offs and planned future
work, see the
[design and implementation notes](ConfigTerminalInternals.md#15-known-limitations-and-future-work).
