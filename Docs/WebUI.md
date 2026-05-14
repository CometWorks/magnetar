# Web UI

A browser-based management interface for the Space Engineers Dedicated Server and Pulsar plugin loader.

## Overview

The Web UI is a Python FastAPI application that provides:
- Configuration editing for the Dedicated Server (`SpaceEngineers-Dedicated.cfg`)
- Configuration editing for Pulsar/Magnetar (plugin profiles, sources, core settings)
- Live server monitoring via the Admin plugin REST API
- Player management, chat, and server controls

## Stack

| Component | Technology |
|-----------|-----------|
| Backend | Python 3.13, FastAPI, Uvicorn |
| Frontend | Vanilla HTML/CSS/JS, Jinja2 templates |
| HTTP Client | httpx (async, for Admin plugin communication) |
| Config Format | XML (matching SE and Pulsar formats) |
| Styling | Custom dark theme with CSS variables |

## Architecture

```
Browser (http://localhost:8000)
    |
FastAPI Application
    |-- Template routes (GET /server/dashboard, /pulsar/config, etc.)
    |-- API routes
    |     |-- /api/pulsar/*  --> pulsar_config.py --> XML files in %APPDATA%/Magnetar/
    |     |-- /api/server/*  --> ds_config.py     --> SpaceEngineers-Dedicated.cfg
    |     |-- /api/server/*  --> admin_client.py  --> Admin plugin (http://127.0.0.1:9000)
    |-- Static files (CSS, JS)
```

## Pages

### Pulsar Settings
| Page | Path | Description |
|------|------|-------------|
| Core Config | `/pulsar/config` | Network timeout, IPv6, data consent, game version |
| Profiles | `/pulsar/profiles` | Create/edit/delete plugin profiles (GitHub plugins, dev folders, local plugins, mods) |
| Sources | `/pulsar/sources` | Manage plugin discovery sources (remote/local hubs, remote/local plugins, Steam mods) |

### Dedicated Server
| Page | Path | Description |
|------|------|-------------|
| Dashboard | `/server/dashboard` | Live status, player list, chat, save/stop controls |
| Server Config | `/server/config` | Server identity, network, remote API, auto-restart, watchdog, anti-spam |
| World Settings | `/server/world` | 100+ session settings: game mode, multipliers, PCU, gameplay features, environment, economy, trash removal, performance |
| Administration | `/server/admin` | Manage administrators, banned/reserved players, server plugins, saved worlds |

## API Routes

### Pulsar Configuration (`/api/pulsar`)

| Method | Path | Description |
|--------|------|-------------|
| `GET/PUT` | `/api/pulsar/config` | Core configuration |
| `GET/PUT` | `/api/pulsar/sources` | Plugin sources |
| `GET` | `/api/pulsar/profiles` | List profiles |
| `GET/PUT` | `/api/pulsar/profiles/{name}` | Load/update a profile |
| `POST` | `/api/pulsar/profiles/{name}` | Create a profile |
| `DELETE` | `/api/pulsar/profiles/{name}` | Delete a profile |
| `POST` | `/api/pulsar/profiles/{name}/activate` | Set active profile |

### Server Configuration and Management (`/api/server`)

| Method | Path | Description |
|--------|------|-------------|
| `GET/PUT` | `/api/server/config` | DS configuration file |
| `GET` | `/api/server/worlds` | List saved worlds |
| `GET` | `/api/server/state` | Live server state (proxied from Admin plugin) |
| `GET` | `/api/server/players` | Online players (proxied from Admin plugin) |
| `GET` | `/api/server/chat` | Chat history (proxied from Admin plugin) |
| `POST` | `/api/server/chat` | Send chat message (body: `{"message": "text"}`) |
| `POST` | `/api/server/save` | Save world |
| `POST` | `/api/server/stop` | Stop server |
| `POST` | `/api/server/players/{steamId}/{action}` | Player actions: kick, ban, unban, promote, demote |
| `POST` | `/api/server/session-settings` | Update session settings (stub) |

## Configuration

Settings are loaded from environment variables or a `.env` file. See `env.sample` for all options:

| Variable | Default | Description |
|----------|---------|-------------|
| `WEBUI_HOST` | `0.0.0.0` | Listen address |
| `WEBUI_PORT` | `8000` | Listen port |
| `PULSAR_CONFIG_DIR` | `%APPDATA%/Magnetar` | Pulsar config directory |
| `DS_CONFIG_DIR` | `%APPDATA%/SpaceEngineersDedicated` | DS config directory |
| `ADMIN_API_URL` | `http://127.0.0.1:9000` | Admin plugin URL |

## Running

The project uses [uv](https://docs.astral.sh/uv/) for dependency and virtual environment management. Dependencies are declared in `pyproject.toml`.

```bash
cd webui
uv sync
uv run python run.py
```

`uv sync` creates a `.venv/` directory and installs all runtime and dev dependencies from `pyproject.toml` and the `uv.lock` lock file.

The UI is then available at `http://localhost:8000`.

## DS Configuration Handling

The Web UI reads and writes `SpaceEngineers-Dedicated.cfg` as XML, matching the exact schema used by the Dedicated Server's `MyConfigDedicatedData<T>` class. This includes:

- Preserving the original XML typo `AutoRestatTimeInMin` (present in the DS source code)
- Using `unsignedLong` as the XML item tag for administrator and banned player lists
- Using `Parameter` as the XML item tag for network parameters
- Handling nullable `PermanentDeath` with `xsi:nil` support

Note: Saving the DS config rebuilds the XML from the model. Fields not represented in the model (e.g., newer SE session settings added in recent game updates) will not be preserved across a save cycle.
