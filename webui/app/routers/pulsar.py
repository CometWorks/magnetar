"""API routes for Pulsar/Magnetar plugin configuration."""

from fastapi import APIRouter, HTTPException

from app.models.pulsar import CoreConfig, Profile, ProfileList, SourcesConfig
from app.services import pulsar_config as svc

router = APIRouter(prefix="/api/pulsar", tags=["pulsar"])


@router.get("/config", response_model=CoreConfig)
def get_core_config():
    return svc.load_core_config()


@router.put("/config", response_model=CoreConfig)
def update_core_config(config: CoreConfig):
    svc.save_core_config(config)
    return config


@router.get("/sources", response_model=SourcesConfig)
def get_sources():
    return svc.load_sources_config()


@router.put("/sources", response_model=SourcesConfig)
def update_sources(config: SourcesConfig):
    svc.save_sources_config(config)
    return config


@router.get("/profiles", response_model=ProfileList)
def get_profiles():
    return svc.list_profiles()


@router.get("/profiles/{name}", response_model=Profile)
def get_profile(name: str):
    profile = svc.load_profile(name)
    if not profile:
        raise HTTPException(status_code=404, detail="Profile not found")
    return profile


@router.put("/profiles/{name}", response_model=Profile)
def update_profile(name: str, profile: Profile):
    profile.name = name
    svc.save_profile(profile)
    return profile


@router.post("/profiles/{name}", response_model=Profile)
def create_profile(name: str):
    existing = svc.load_profile(name)
    if existing:
        raise HTTPException(status_code=409, detail="Profile already exists")
    profile = Profile(name=name)
    svc.save_profile(profile)
    return profile


@router.delete("/profiles/{name}")
def delete_profile(name: str):
    if not svc.delete_profile(name):
        raise HTTPException(status_code=404, detail="Profile not found")
    return {"status": "deleted"}


@router.post("/profiles/{name}/activate")
def activate_profile(name: str):
    svc.set_current_profile(name)
    return {"status": "activated", "name": name}
