"""Service for reading and writing Pulsar/Magnetar XML configuration files."""

import os
import xml.etree.ElementTree as ET
from pathlib import Path

from app.config import settings
from app.models.pulsar import (
    CoreConfig,
    GitHubPluginConfig,
    LocalFolderConfig,
    LocalHubConfig,
    LocalPluginConfig,
    ModConfig,
    Profile,
    ProfileList,
    RemoteHubConfig,
    RemotePluginConfig,
    SourcesConfig,
)


def _config_dir() -> Path:
    return Path(settings.pulsar_config_dir)


def _ensure_dir(path: Path):
    path.mkdir(parents=True, exist_ok=True)


def load_core_config() -> CoreConfig:
    path = _config_dir() / "config.xml"
    if not path.exists():
        return CoreConfig()
    tree = ET.parse(path)
    root = tree.getroot()
    return CoreConfig(
        data_handling_consent=_bool(root, "DataHandlingConsent", False),
        data_handling_consent_date=_text(root, "DataHandlingConsentDate", ""),
        allow_ipv6=_bool(root, "AllowIPv6", True),
        network_timeout=_int(root, "NetworkTimeout", 5000),
        game_version=_text(root, "GameVersion", None),
    )


def save_core_config(config: CoreConfig):
    path = _config_dir() / "config.xml"
    _ensure_dir(path.parent)
    root = ET.Element("CoreConfig")
    _set_elem(root, "DataHandlingConsent", str(config.data_handling_consent).lower())
    _set_elem(root, "DataHandlingConsentDate", config.data_handling_consent_date)
    _set_elem(root, "AllowIPv6", str(config.allow_ipv6).lower())
    _set_elem(root, "NetworkTimeout", str(config.network_timeout))
    if config.game_version:
        _set_elem(root, "GameVersion", config.game_version)
    tree = ET.ElementTree(root)
    ET.indent(tree, space="  ")
    tree.write(path, encoding="utf-8", xml_declaration=True)


def load_sources_config() -> SourcesConfig:
    path = _config_dir() / "Sources" / "sources.xml"
    if not path.exists():
        return SourcesConfig()
    tree = ET.parse(path)
    root = tree.getroot()
    return SourcesConfig(
        show_warning=_bool(root, "ShowWarning", True),
        max_source_age=_int(root, "MaxSourceAge", 2),
        local_hub_sources=_parse_local_hubs(root),
        remote_hub_sources=_parse_remote_hubs(root),
        remote_plugin_sources=_parse_remote_plugins(root),
        local_plugin_sources=_parse_local_plugins(root),
        mod_sources=_parse_mods(root),
    )


def save_sources_config(config: SourcesConfig):
    path = _config_dir() / "Sources" / "sources.xml"
    _ensure_dir(path.parent)
    root = ET.Element("SourcesConfig")
    _set_elem(root, "ShowWarning", str(config.show_warning).lower())
    _set_elem(root, "MaxSourceAge", str(config.max_source_age))

    lh = ET.SubElement(root, "LocalHubSources")
    for s in config.local_hub_sources:
        e = ET.SubElement(lh, "LocalHub")
        _set_elem(e, "Name", s.name)
        _set_elem(e, "Folder", s.folder)
        _set_elem(e, "Enabled", str(s.enabled).lower())
        _set_elem(e, "Hash", s.hash)

    rh = ET.SubElement(root, "RemoteHubSources")
    for s in config.remote_hub_sources:
        e = ET.SubElement(rh, "RemoteHub")
        _set_elem(e, "Name", s.name)
        _set_elem(e, "Repo", s.repo)
        _set_elem(e, "Branch", s.branch)
        _set_elem(e, "Enabled", str(s.enabled).lower())
        _set_elem(e, "Trusted", str(s.trusted).lower())
        _set_elem(e, "Hash", s.hash)
        if s.last_check:
            _set_elem(e, "LastCheck", s.last_check)

    rp = ET.SubElement(root, "RemotePluginSources")
    for s in config.remote_plugin_sources:
        e = ET.SubElement(rp, "RemotePlugin")
        _set_elem(e, "Name", s.name)
        _set_elem(e, "Repo", s.repo)
        _set_elem(e, "Branch", s.branch)
        _set_elem(e, "File", s.file)
        _set_elem(e, "Enabled", str(s.enabled).lower())
        _set_elem(e, "Trusted", str(s.trusted).lower())
        if s.last_check:
            _set_elem(e, "LastCheck", s.last_check)

    lp = ET.SubElement(root, "LocalPluginSources")
    for s in config.local_plugin_sources:
        e = ET.SubElement(lp, "LocalPlugin")
        _set_elem(e, "Name", s.name)
        _set_elem(e, "Folder", s.folder)
        _set_elem(e, "Enabled", str(s.enabled).lower())

    ms = ET.SubElement(root, "ModSources")
    for s in config.mod_sources:
        e = ET.SubElement(ms, "Mod")
        _set_elem(e, "Name", s.name)
        _set_elem(e, "ID", str(s.id))
        _set_elem(e, "Enabled", str(s.enabled).lower())

    tree = ET.ElementTree(root)
    ET.indent(tree, space="  ")
    tree.write(path, encoding="utf-8", xml_declaration=True)


def list_profiles() -> ProfileList:
    profiles_dir = _config_dir() / "Profiles"
    current_name = ""
    profile_names = []
    if profiles_dir.exists():
        current_path = profiles_dir / "Current.xml"
        if current_path.exists():
            try:
                tree = ET.parse(current_path)
                name_elem = tree.getroot().find("Name")
                if name_elem is not None and name_elem.text:
                    current_name = name_elem.text
            except ET.ParseError:
                pass
        for f in profiles_dir.glob("*.xml"):
            if f.stem != "Current":
                profile_names.append(f.stem)
    return ProfileList(current=current_name, profiles=sorted(profile_names))


def load_profile(name: str) -> Profile | None:
    profiles_dir = _config_dir() / "Profiles"
    path = profiles_dir / f"{name}.xml"
    if not path.exists():
        return None
    return _parse_profile(path)


def save_profile(profile: Profile):
    profiles_dir = _config_dir() / "Profiles"
    _ensure_dir(profiles_dir)
    path = profiles_dir / f"{profile.name}.xml"
    root = ET.Element("Profile")
    _set_elem(root, "Name", profile.name)

    gh = ET.SubElement(root, "GitHub")
    for g in profile.github:
        e = ET.SubElement(gh, "GitHubPluginConfig")
        _set_elem(e, "Id", g.id)
        _set_elem(e, "SelectedVersion", g.selected_version)

    df = ET.SubElement(root, "DevFolder")
    for d in profile.dev_folder:
        e = ET.SubElement(df, "LocalFolderConfig")
        _set_elem(e, "Id", d.id)
        _set_elem(e, "DataFile", d.data_file)
        _set_elem(e, "DebugBuild", str(d.debug_build).lower())

    loc = ET.SubElement(root, "Local")
    for l_id in profile.local:
        e = ET.SubElement(loc, "string")
        e.text = l_id

    mods = ET.SubElement(root, "Mods")
    for m_id in profile.mods:
        e = ET.SubElement(mods, "unsignedLong")
        e.text = str(m_id)

    tree = ET.ElementTree(root)
    ET.indent(tree, space="  ")
    tree.write(path, encoding="utf-8", xml_declaration=True)


def set_current_profile(name: str):
    profiles_dir = _config_dir() / "Profiles"
    _ensure_dir(profiles_dir)
    profile = load_profile(name)
    if profile:
        path = profiles_dir / "Current.xml"
        root = ET.Element("Profile")
        _set_elem(root, "Name", profile.name)

        gh = ET.SubElement(root, "GitHub")
        for g in profile.github:
            e = ET.SubElement(gh, "GitHubPluginConfig")
            _set_elem(e, "Id", g.id)
            _set_elem(e, "SelectedVersion", g.selected_version)

        df = ET.SubElement(root, "DevFolder")
        for d in profile.dev_folder:
            e = ET.SubElement(df, "LocalFolderConfig")
            _set_elem(e, "Id", d.id)
            _set_elem(e, "DataFile", d.data_file)
            _set_elem(e, "DebugBuild", str(d.debug_build).lower())

        loc = ET.SubElement(root, "Local")
        for l_id in profile.local:
            e = ET.SubElement(loc, "string")
            e.text = l_id

        mods_elem = ET.SubElement(root, "Mods")
        for m_id in profile.mods:
            e = ET.SubElement(mods_elem, "unsignedLong")
            e.text = str(m_id)

        tree = ET.ElementTree(root)
        ET.indent(tree, space="  ")
        tree.write(path, encoding="utf-8", xml_declaration=True)


def delete_profile(name: str) -> bool:
    path = _config_dir() / "Profiles" / f"{name}.xml"
    if path.exists():
        path.unlink()
        return True
    return False


def _parse_profile(path: Path) -> Profile:
    tree = ET.parse(path)
    root = tree.getroot()
    github = []
    gh_elem = root.find("GitHub")
    if gh_elem is not None:
        for e in gh_elem.findall("GitHubPluginConfig"):
            github.append(GitHubPluginConfig(
                id=_text(e, "Id", ""),
                selected_version=_text(e, "SelectedVersion", ""),
            ))
    dev_folder = []
    df_elem = root.find("DevFolder")
    if df_elem is not None:
        for e in df_elem.findall("LocalFolderConfig"):
            dev_folder.append(LocalFolderConfig(
                id=_text(e, "Id", ""),
                data_file=_text(e, "DataFile", ""),
                debug_build=_bool(e, "DebugBuild", True),
            ))
    local = []
    loc_elem = root.find("Local")
    if loc_elem is not None:
        for e in loc_elem.findall("string"):
            if e.text:
                local.append(e.text)
    mods = []
    mods_elem = root.find("Mods")
    if mods_elem is not None:
        for e in mods_elem.findall("unsignedLong"):
            if e.text:
                mods.append(int(e.text))
    return Profile(
        name=_text(root, "Name", path.stem),
        github=github,
        dev_folder=dev_folder,
        local=local,
        mods=mods,
    )


def _parse_local_hubs(root: ET.Element) -> list[LocalHubConfig]:
    items = []
    parent = root.find("LocalHubSources")
    if parent is None:
        return items
    for e in parent.findall("LocalHub"):
        items.append(LocalHubConfig(
            name=_text(e, "Name", ""),
            folder=_text(e, "Folder", ""),
            enabled=_bool(e, "Enabled", True),
            hash=_text(e, "Hash", ""),
        ))
    return items


def _parse_remote_hubs(root: ET.Element) -> list[RemoteHubConfig]:
    items = []
    parent = root.find("RemoteHubSources")
    if parent is None:
        return items
    for e in parent.findall("RemoteHub"):
        items.append(RemoteHubConfig(
            name=_text(e, "Name", ""),
            repo=_text(e, "Repo", ""),
            branch=_text(e, "Branch", "main"),
            last_check=_text(e, "LastCheck", None),
            hash=_text(e, "Hash", ""),
            enabled=_bool(e, "Enabled", True),
            trusted=_bool(e, "Trusted", False),
        ))
    return items


def _parse_remote_plugins(root: ET.Element) -> list[RemotePluginConfig]:
    items = []
    parent = root.find("RemotePluginSources")
    if parent is None:
        return items
    for e in parent.findall("RemotePlugin"):
        items.append(RemotePluginConfig(
            name=_text(e, "Name", ""),
            repo=_text(e, "Repo", ""),
            branch=_text(e, "Branch", "main"),
            file=_text(e, "File", ""),
            last_check=_text(e, "LastCheck", None),
            enabled=_bool(e, "Enabled", True),
            trusted=_bool(e, "Trusted", False),
        ))
    return items


def _parse_local_plugins(root: ET.Element) -> list[LocalPluginConfig]:
    items = []
    parent = root.find("LocalPluginSources")
    if parent is None:
        return items
    for e in parent.findall("LocalPlugin"):
        items.append(LocalPluginConfig(
            name=_text(e, "Name", ""),
            folder=_text(e, "Folder", ""),
            enabled=_bool(e, "Enabled", True),
        ))
    return items


def _parse_mods(root: ET.Element) -> list[ModConfig]:
    items = []
    parent = root.find("ModSources")
    if parent is None:
        return items
    for e in parent.findall("Mod"):
        items.append(ModConfig(
            name=_text(e, "Name", ""),
            id=_int(e, "ID", 0),
            enabled=_bool(e, "Enabled", True),
        ))
    return items


def _text(parent: ET.Element, tag: str, default):
    elem = parent.find(tag)
    if elem is not None and elem.text:
        return elem.text
    return default


def _bool(parent: ET.Element, tag: str, default: bool) -> bool:
    val = _text(parent, tag, None)
    if val is None:
        return default
    return val.lower() == "true"


def _int(parent: ET.Element, tag: str, default: int) -> int:
    val = _text(parent, tag, None)
    if val is None:
        return default
    try:
        return int(val)
    except ValueError:
        return default


def _set_elem(parent: ET.Element, tag: str, text: str):
    e = ET.SubElement(parent, tag)
    e.text = text
