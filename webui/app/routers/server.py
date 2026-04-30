"""API routes for Dedicated Server configuration and live management."""

from fastapi import APIRouter

from app.models.dedicated_server import (
    ChatMessage,
    DedicatedConfig,
    PlayerInfo,
    SavedWorld,
    ServerState,
)
from app.services import admin_client, ds_config

router = APIRouter(prefix="/api/server", tags=["server"])


@router.get("/config", response_model=DedicatedConfig)
def get_ds_config():
    return ds_config.load_ds_config()


@router.put("/config", response_model=DedicatedConfig)
def update_ds_config(config: DedicatedConfig):
    ds_config.save_ds_config(config)
    return config


@router.get("/worlds", response_model=list[SavedWorld])
def get_worlds():
    return ds_config.list_saved_worlds()


@router.get("/state", response_model=ServerState)
async def get_server_state():
    return await admin_client.get_server_state()


@router.get("/players", response_model=list[PlayerInfo])
async def get_players():
    return await admin_client.get_players()


@router.get("/chat", response_model=list[ChatMessage])
async def get_chat(count: int = 50):
    return await admin_client.get_chat(count)


@router.post("/chat")
async def send_chat(message: str):
    ok = await admin_client.send_chat(message)
    return {"status": "sent" if ok else "failed"}


@router.post("/save")
async def save_world():
    ok = await admin_client.save_world()
    return {"status": "saved" if ok else "failed"}


@router.post("/stop")
async def stop_server():
    ok = await admin_client.stop_server()
    return {"status": "stopping" if ok else "failed"}


@router.post("/players/{steam_id}/kick")
async def kick_player(steam_id: int):
    ok = await admin_client.kick_player(steam_id)
    return {"status": "kicked" if ok else "failed"}


@router.post("/players/{steam_id}/ban")
async def ban_player(steam_id: int):
    ok = await admin_client.ban_player(steam_id)
    return {"status": "banned" if ok else "failed"}


@router.post("/players/{steam_id}/unban")
async def unban_player(steam_id: int):
    ok = await admin_client.unban_player(steam_id)
    return {"status": "unbanned" if ok else "failed"}


@router.post("/players/{steam_id}/promote")
async def promote_player(steam_id: int):
    ok = await admin_client.promote_player(steam_id)
    return {"status": "promoted" if ok else "failed"}


@router.post("/players/{steam_id}/demote")
async def demote_player(steam_id: int):
    ok = await admin_client.demote_player(steam_id)
    return {"status": "demoted" if ok else "failed"}


@router.post("/session-settings")
async def update_session_settings(settings: dict):
    ok = await admin_client.update_session_settings(settings)
    return {"status": "updated" if ok else "failed"}
