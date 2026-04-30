# Admin Plugin

A REST API plugin that runs inside the Space Engineers Dedicated Server, exposing server state and management operations over HTTP.

## Overview

The Admin plugin implements the `IPlugin` interface and is loaded by the Dedicated Server at startup. It starts an HTTP listener on `http://127.0.0.1:9000/` and bridges HTTP requests to the game engine via `GameBridge`.

### Components

| File | Purpose |
|------|---------|
| `AdminPlugin.cs` | Entry point, implements `IPlugin` lifecycle |
| `HttpServer.cs` | HTTP listener with CORS support and request routing |
| `GameBridge.cs` | Accesses game state via `MySession`, `MyMultiplayer`, `MyFactions` |

## API Endpoints

All responses are JSON with camelCase property names.

### Server State

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/state` | Server status, player count, sim speed, PCU, uptime |
| `POST` | `/api/save` | Trigger a world save |
| `POST` | `/api/stop` | Gracefully shut down the server |

### Players

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/players` | List online players with faction, admin status, ping |
| `POST` | `/api/players/{steamId}/kick` | Kick a player |
| `POST` | `/api/players/{steamId}/ban` | Ban a player |
| `POST` | `/api/players/{steamId}/unban` | Unban a player |
| `POST` | `/api/players/{steamId}/promote` | Promote to admin |
| `POST` | `/api/players/{steamId}/demote` | Demote from admin |

### Chat

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/chat?count=50` | Get recent chat messages |
| `POST` | `/api/chat` | Send a chat message (body: `{"message": "text"}`) |

### Session Settings

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/session-settings` | Update session settings (returns 501, not yet implemented) |

## Data Transfer Objects

### ServerStateDto

```
isRunning, serverName, worldName, playersOnline, maxPlayers,
simSpeed, simCpuLoad, serverCpuLoad, usedPcu, totalPcu,
uptimeSeconds, gameVersion, modsLoaded, pluginsLoaded
```

### PlayerInfoDto

```
steamId, displayName, faction, isAdmin, pingMs
```

### ChatEntry

```
timestamp, sender, message
```

## Game Engine Access

The plugin accesses the Dedicated Server's game engine through the following APIs:

- `MySession.Static` - Session state, player list, factions, settings
- `MyMultiplayer.Static` - Kick, ban, and chat operations
- `MySandboxGame.Static` - Server shutdown (via reflection for `ExitThreadSafe`)
- `MySession.Static.Factions.GetPlayerFaction()` - Player faction lookup

The chat log is maintained in-memory (max 200 messages) since the game engine does not provide a chat history API.

## Configuration

The HTTP server listens on port 9000 by default (configurable via the `HttpServer` constructor). It only binds to `127.0.0.1` for security, requiring the Web UI to run on the same machine or use a reverse proxy.

CORS headers are included for cross-origin browser requests from the Web UI.
