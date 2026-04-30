"""HTTP client for communicating with the Admin plugin REST API running inside the DS."""

import httpx
from app.config import settings
from app.models.dedicated_server import ChatMessage, PlayerInfo, ServerState


async def _get(path: str) -> dict | list | None:
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            resp = await client.get(f"{settings.admin_api_url}{path}")
            resp.raise_for_status()
            return resp.json()
    except (httpx.ConnectError, httpx.TimeoutException, httpx.HTTPStatusError):
        return None


async def _post(path: str, data: dict | None = None) -> dict | None:
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            resp = await client.post(f"{settings.admin_api_url}{path}", json=data)
            resp.raise_for_status()
            return resp.json()
    except (httpx.ConnectError, httpx.TimeoutException, httpx.HTTPStatusError):
        return None


async def get_server_state() -> ServerState:
    data = await _get("/api/state")
    if data:
        return ServerState(**data)
    return ServerState(is_running=False)


async def get_players() -> list[PlayerInfo]:
    data = await _get("/api/players")
    if data and isinstance(data, list):
        return [PlayerInfo(**p) for p in data]
    return []


async def get_chat(count: int = 50) -> list[ChatMessage]:
    data = await _get(f"/api/chat?count={count}")
    if data and isinstance(data, list):
        return [ChatMessage(**m) for m in data]
    return []


async def send_chat(message: str) -> bool:
    result = await _post("/api/chat", {"message": message})
    return result is not None


async def save_world() -> bool:
    result = await _post("/api/save")
    return result is not None


async def stop_server() -> bool:
    result = await _post("/api/stop")
    return result is not None


async def kick_player(steam_id: int) -> bool:
    result = await _post(f"/api/players/{steam_id}/kick")
    return result is not None


async def ban_player(steam_id: int) -> bool:
    result = await _post(f"/api/players/{steam_id}/ban")
    return result is not None


async def unban_player(steam_id: int) -> bool:
    result = await _post(f"/api/players/{steam_id}/unban")
    return result is not None


async def promote_player(steam_id: int) -> bool:
    result = await _post(f"/api/players/{steam_id}/promote")
    return result is not None


async def demote_player(steam_id: int) -> bool:
    result = await _post(f"/api/players/{steam_id}/demote")
    return result is not None


async def update_session_settings(settings_dict: dict) -> bool:
    result = await _post("/api/session-settings", settings_dict)
    return result is not None
