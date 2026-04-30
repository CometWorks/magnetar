"""Pulsar WebUI - FastAPI application."""

from pathlib import Path

from dotenv import load_dotenv

load_dotenv()

from fastapi import FastAPI, Request
from fastapi.responses import HTMLResponse
from fastapi.staticfiles import StaticFiles
from fastapi.templating import Jinja2Templates

from app.routers import pulsar, server

app = FastAPI(title="Pulsar WebUI", version="1.0.0")

app_dir = Path(__file__).parent
app.mount("/static", StaticFiles(directory=app_dir / "static"), name="static")
templates = Jinja2Templates(directory=app_dir / "templates")

app.include_router(pulsar.router)
app.include_router(server.router)


@app.get("/", response_class=HTMLResponse)
async def index(request: Request):
    return templates.TemplateResponse("index.html", {"request": request})


@app.get("/pulsar/config", response_class=HTMLResponse)
async def pulsar_config_page(request: Request):
    return templates.TemplateResponse("pages/pulsar_config.html", {"request": request})


@app.get("/pulsar/profiles", response_class=HTMLResponse)
async def pulsar_profiles_page(request: Request):
    return templates.TemplateResponse("pages/pulsar_profiles.html", {"request": request})


@app.get("/pulsar/sources", response_class=HTMLResponse)
async def pulsar_sources_page(request: Request):
    return templates.TemplateResponse("pages/pulsar_sources.html", {"request": request})


@app.get("/server/config", response_class=HTMLResponse)
async def server_config_page(request: Request):
    return templates.TemplateResponse("pages/server_config.html", {"request": request})


@app.get("/server/world", response_class=HTMLResponse)
async def server_world_page(request: Request):
    return templates.TemplateResponse("pages/server_world.html", {"request": request})


@app.get("/server/admin", response_class=HTMLResponse)
async def server_admin_page(request: Request):
    return templates.TemplateResponse("pages/server_admin.html", {"request": request})


@app.get("/server/dashboard", response_class=HTMLResponse)
async def server_dashboard_page(request: Request):
    return templates.TemplateResponse("pages/server_dashboard.html", {"request": request})
