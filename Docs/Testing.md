# Testing

## Prerequisites

The project uses [uv](https://docs.astral.sh/uv/) for dependency management. Dependencies (including `pytest`) are declared in `webui/pyproject.toml`.

From the `webui/` directory create the virtual environment and install all dependencies:

```
cd webui
uv sync
```

This creates `.venv/` and installs both the runtime dependencies and the `dev` dependency group (which includes `pytest`).

## WebUI REST API Tests

These tests validate the WebUI's backend API routes using FastAPI's `TestClient`.
They use isolated temp directories for config files and mock the Admin plugin
HTTP client, so **no running Dedicated Server is required**.

### Running

```
cd webui
uv run pytest tests/test_webui_api.py -v
```

### What is covered

| Area | Tests |
|---|---|
| DS config (`/api/server/config`) | GET default, PUT roundtrip, session settings, lists (admins, banned, plugins), auto-restart fields |
| Saved worlds (`/api/server/worlds`) | Empty listing, populated listing with size, sort order |
| Server live state (`/api/server/state`) | Running state, offline state (mocked Admin API) |
| Players (`/api/server/players`) | Player list, empty list (mocked) |
| Chat (`/api/server/chat`) | GET with count, POST send success/failure (mocked) |
| Server actions (`/api/server/save`, `stop`) | Success and failure paths (mocked) |
| Player actions (`kick`, `ban`, `unban`, `promote`, `demote`) | All five actions, failure path (mocked) |
| Session settings (`/api/server/session-settings`) | Forwarding to admin client (mocked) |
| Pulsar core config (`/api/pulsar/config`) | GET default, PUT roundtrip, nullable game_version |
| Pulsar sources (`/api/pulsar/sources`) | GET default, PUT roundtrip with remote hubs, local plugins, mods |
| Pulsar profiles | Create, read, update, delete, duplicate conflict (409), not found (404), activate, list sorted, full lifecycle |
| HTML pages | Smoke test all 8 page routes return HTTP 200 with HTML content type |

## Admin Plugin Integration Tests

These tests exercise the Admin plugin's HTTP API (port 9000) against a **real
running Dedicated Server**. They start a DS process, wait for it to load a test
world, run the tests, then shut down and clean up.

### Prerequisites

1. Space Engineers Dedicated Server installed
2. The Admin plugin DLL built and placed in the DS `Plugins/` directory

### Running

```
set ADMIN_INTEGRATION=1
set ADMIN_TEST_DS_DIR=C:\SteamCMD\steamapps\common\SpaceEngineersDedicatedServer\DedicatedServer64
cd webui
uv run pytest tests/test_admin_plugin.py -v -s
```

The tests are **skipped by default** unless `ADMIN_INTEGRATION=1` is set.

Startup takes 60-120 seconds depending on hardware. The test creates a
temporary config directory (`%APPDATA%\SpaceEngineersDedicated_Test`) with a
minimal world configuration. The test world is cleaned up automatically after
the run.

### What is covered

| Area | Tests |
|---|---|
| Server state (`/api/state`) | Running flag, player count, sim speed, PCU, version, uptime |
| Players (`/api/players`) | List structure and field presence |
| Chat (`/api/chat`) | GET list, GET with count, POST send and verify receipt, sender field |
| Save (`/api/save`) | Successful world save |
| Player actions | kick, ban, unban, promote, demote on non-existent player, unknown action |
| Session settings (`/api/session-settings`) | 501 Not Implemented response |
| Error handling | Unknown endpoint (404), CORS preflight (204) |
| Server stop (`/api/stop`) | Graceful shutdown (runs last) |

### Notes

- The tests use non-standard ports (19000 for Admin API, 37016/38766 for
  game/steam) to avoid conflicts with a production server.
- Player action tests run against non-existent Steam IDs, so they verify the
  endpoint responds correctly without requiring actual connected players.
- The stop test is named `test_zz_stop_server` so it sorts last alphabetically.
