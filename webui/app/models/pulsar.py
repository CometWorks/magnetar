"""Pydantic models matching Pulsar's Shared/Config C# classes."""

from pydantic import BaseModel, Field


class CoreConfig(BaseModel):
    data_handling_consent: bool = False
    data_handling_consent_date: str = ""
    allow_ipv6: bool = True
    network_timeout: int = 5000
    game_version: str | None = None


class RemoteHubConfig(BaseModel):
    name: str = ""
    repo: str = ""
    branch: str = "main"
    last_check: str | None = None
    hash: str = ""
    enabled: bool = True
    trusted: bool = False


class LocalHubConfig(BaseModel):
    name: str = ""
    folder: str = ""
    enabled: bool = True
    hash: str = ""


class RemotePluginConfig(BaseModel):
    name: str = ""
    repo: str = ""
    branch: str = "main"
    file: str = ""
    last_check: str | None = None
    enabled: bool = True
    trusted: bool = False


class LocalPluginConfig(BaseModel):
    name: str = ""
    folder: str = ""
    enabled: bool = True


class ModConfig(BaseModel):
    name: str = ""
    id: int = 0
    enabled: bool = True


class SourcesConfig(BaseModel):
    show_warning: bool = True
    max_source_age: int = 2
    local_hub_sources: list[LocalHubConfig] = Field(default_factory=list)
    remote_hub_sources: list[RemoteHubConfig] = Field(default_factory=list)
    remote_plugin_sources: list[RemotePluginConfig] = Field(default_factory=list)
    local_plugin_sources: list[LocalPluginConfig] = Field(default_factory=list)
    mod_sources: list[ModConfig] = Field(default_factory=list)


class GitHubPluginConfig(BaseModel):
    id: str = ""
    selected_version: str = ""


class LocalFolderConfig(BaseModel):
    id: str = ""
    data_file: str = ""
    debug_build: bool = True


class Profile(BaseModel):
    name: str = ""
    github: list[GitHubPluginConfig] = Field(default_factory=list)
    dev_folder: list[LocalFolderConfig] = Field(default_factory=list)
    local: list[str] = Field(default_factory=list)
    mods: list[int] = Field(default_factory=list)


class ProfileList(BaseModel):
    current: str = ""
    profiles: list[str] = Field(default_factory=list)


class Flags(BaseModel):
    splash_type: str = "Pulsar"
    update_type: str = "Standard"
    external_debug: bool = False
    debug_menu: bool = False
    custom_sources: bool = False
    continue_game: bool = False
    check_all_plugins: bool = False
    game_intro_video: bool = False
    make_check_file: bool = False
    trusted_mods: bool = False
