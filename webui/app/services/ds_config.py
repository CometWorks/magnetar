"""Service for reading and writing Space Engineers Dedicated Server configuration."""

import os
import xml.etree.ElementTree as ET
from pathlib import Path

from app.config import settings
from app.models.dedicated_server import DedicatedConfig, SavedWorld, SessionSettings


def _ds_config_dir() -> Path:
    return Path(settings.ds_config_dir)


def _ds_cfg_path() -> Path:
    return _ds_config_dir() / "SpaceEngineers-Dedicated.cfg"


def load_ds_config() -> DedicatedConfig:
    path = _ds_cfg_path()
    if not path.exists():
        return DedicatedConfig()
    tree = ET.parse(path)
    root = tree.getroot()
    ss_elem = root.find("SessionSettings")
    ss = _parse_session_settings(ss_elem) if ss_elem is not None else SessionSettings()
    return DedicatedConfig(
        session_settings=ss,
        load_world=_text(root, "LoadWorld", ""),
        cross_platform=_bool(root, "CrossPlatform", False),
        ip=_text(root, "IP", "0.0.0.0"),
        steam_port=_int(root, "SteamPort", 8766),
        server_port=_int(root, "ServerPort", 27016),
        asteroid_amount=_int(root, "AsteroidAmount", 4),
        administrators=_text_list(root, "Administrators", "unsignedLong"),
        banned=_int_list(root, "Banned", "unsignedLong"),
        reserved=_int_list(root, "Reserved", "unsignedLong"),
        group_id=_int(root, "GroupID", 0),
        server_name=_text(root, "ServerName", ""),
        world_name=_text(root, "WorldName", ""),
        verbose_network_logging=_bool(root, "VerboseNetworkLogging", False),
        pause_game_when_empty=_bool(root, "PauseGameWhenEmpty", False),
        message_of_the_day=_text(root, "MessageOfTheDay", ""),
        message_of_the_day_url=_text(root, "MessageOfTheDayUrl", ""),
        auto_restart_enabled=_bool(root, "AutoRestartEnabled", True),
        auto_restart_time_in_min=_int(root, "AutoRestatTimeInMin", 0),
        auto_restart_save=_bool(root, "AutoRestartSave", True),
        auto_update_enabled=_bool(root, "AutoUpdateEnabled", False),
        auto_update_check_interval_in_min=_int(root, "AutoUpdateCheckIntervalInMin", 10),
        auto_update_restart_delay_in_min=_int(root, "AutoUpdateRestartDelayInMin", 15),
        auto_update_steam_branch=_text(root, "AutoUpdateSteamBranch", ""),
        auto_update_branch_password=_text(root, "AutoUpdateBranchPassword", ""),
        ignore_last_session=_bool(root, "IgnoreLastSession", False),
        premade_checkpoint_path=_text(root, "PremadeCheckpointPath", ""),
        server_description=_text(root, "ServerDescription", ""),
        server_password_hash=_text(root, "ServerPasswordHash", ""),
        server_password_salt=_text(root, "ServerPasswordSalt", ""),
        remote_api_enabled=_bool(root, "RemoteApiEnabled", True),
        remote_security_key=_text(root, "RemoteSecurityKey", ""),
        remote_api_port=_int(root, "RemoteApiPort", 8080),
        remote_api_ip=_text(root, "RemoteApiIP", ""),
        plugins=_text_list(root, "Plugins", "string"),
        watcher_interval=_float(root, "WatcherInterval", 30.0),
        watcher_simulation_speed_minimum=_float(root, "WatcherSimulationSpeedMinimum", 0.05),
        manual_action_delay=_int(root, "ManualActionDelay", 5),
        manual_action_chat_message=_text(root, "ManualActionChatMessage", "Server will be shut down in {0} min(s)."),
        autodetect_dependencies=_bool(root, "AutodetectDependencies", True),
        save_chat_to_log=_bool(root, "SaveChatToLog", False),
        network_type=_text(root, "NetworkType", "steam"),
        console_compatibility=_bool(root, "ConsoleCompatibility", False),
        network_parameters=_text_list(root, "NetworkParameters", "Parameter"),
        chat_anti_spam_enabled=_bool(root, "ChatAntiSpamEnabled", True),
        same_message_timeout=_int(root, "SameMessageTimeout", 30),
        spam_messages_time=_float(root, "SpamMessagesTime", 0.5),
        spam_messages_timeout=_int(root, "SpamMessagesTimeout", 60),
        dedicated_id=_int(root, "DedicatedId", 0),
    )


def save_ds_config(config: DedicatedConfig):
    path = _ds_cfg_path()
    path.parent.mkdir(parents=True, exist_ok=True)
    root = ET.Element("MyConfigDedicated")
    root.set("xmlns:xsd", "http://www.w3.org/2001/XMLSchema")
    root.set("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance")

    _write_session_settings(root, config.session_settings)
    _set_elem(root, "LoadWorld", config.load_world)
    _set_elem(root, "CrossPlatform", _bstr(config.cross_platform))
    _set_elem(root, "IP", config.ip)
    _set_elem(root, "SteamPort", str(config.steam_port))
    _set_elem(root, "ServerPort", str(config.server_port))
    _set_elem(root, "AsteroidAmount", str(config.asteroid_amount))
    _write_list(root, "Administrators", "unsignedLong", [str(a) for a in config.administrators])
    _write_list(root, "Banned", "unsignedLong", [str(b) for b in config.banned])
    _set_elem(root, "GroupID", str(config.group_id))
    _set_elem(root, "ServerName", config.server_name)
    _set_elem(root, "WorldName", config.world_name)
    _set_elem(root, "VerboseNetworkLogging", _bstr(config.verbose_network_logging))
    _set_elem(root, "PauseGameWhenEmpty", _bstr(config.pause_game_when_empty))
    _set_elem(root, "MessageOfTheDay", config.message_of_the_day)
    _set_elem(root, "MessageOfTheDayUrl", config.message_of_the_day_url)
    _set_elem(root, "AutoRestartEnabled", _bstr(config.auto_restart_enabled))
    _set_elem(root, "AutoRestatTimeInMin", str(config.auto_restart_time_in_min))
    _set_elem(root, "AutoRestartSave", _bstr(config.auto_restart_save))
    _set_elem(root, "AutoUpdateEnabled", _bstr(config.auto_update_enabled))
    _set_elem(root, "AutoUpdateCheckIntervalInMin", str(config.auto_update_check_interval_in_min))
    _set_elem(root, "AutoUpdateRestartDelayInMin", str(config.auto_update_restart_delay_in_min))
    _set_elem(root, "AutoUpdateSteamBranch", config.auto_update_steam_branch)
    _set_elem(root, "AutoUpdateBranchPassword", config.auto_update_branch_password)
    _set_elem(root, "IgnoreLastSession", _bstr(config.ignore_last_session))
    _set_elem(root, "PremadeCheckpointPath", config.premade_checkpoint_path)
    _set_elem(root, "ServerDescription", config.server_description)
    _set_elem(root, "ServerPasswordHash", config.server_password_hash)
    _set_elem(root, "ServerPasswordSalt", config.server_password_salt)
    _write_list(root, "Reserved", "unsignedLong", [str(r) for r in config.reserved])
    _set_elem(root, "RemoteApiEnabled", _bstr(config.remote_api_enabled))
    _set_elem(root, "RemoteSecurityKey", config.remote_security_key)
    _set_elem(root, "RemoteApiPort", str(config.remote_api_port))
    _set_elem(root, "RemoteApiIP", config.remote_api_ip)
    _write_list(root, "Plugins", "string", config.plugins)
    _set_elem(root, "WatcherInterval", str(config.watcher_interval))
    _set_elem(root, "WatcherSimulationSpeedMinimum", str(config.watcher_simulation_speed_minimum))
    _set_elem(root, "ManualActionDelay", str(config.manual_action_delay))
    _set_elem(root, "ManualActionChatMessage", config.manual_action_chat_message)
    _set_elem(root, "AutodetectDependencies", _bstr(config.autodetect_dependencies))
    _set_elem(root, "SaveChatToLog", _bstr(config.save_chat_to_log))
    _set_elem(root, "NetworkType", config.network_type)
    _set_elem(root, "ConsoleCompatibility", _bstr(config.console_compatibility))
    _write_list(root, "NetworkParameters", "Parameter", config.network_parameters)
    _set_elem(root, "ChatAntiSpamEnabled", _bstr(config.chat_anti_spam_enabled))
    _set_elem(root, "SameMessageTimeout", str(config.same_message_timeout))
    _set_elem(root, "SpamMessagesTime", str(config.spam_messages_time))
    _set_elem(root, "SpamMessagesTimeout", str(config.spam_messages_timeout))
    _set_elem(root, "DedicatedId", str(config.dedicated_id))

    tree = ET.ElementTree(root)
    ET.indent(tree, space="  ")
    tree.write(path, encoding="utf-8", xml_declaration=True)


def list_saved_worlds() -> list[SavedWorld]:
    saves_dir = _ds_config_dir() / "Saves"
    worlds = []
    if not saves_dir.exists():
        return worlds
    for entry in saves_dir.iterdir():
        if entry.is_dir():
            sandbox_path = entry / "Sandbox.sbc"
            size_mb = sum(f.stat().st_size for f in entry.rglob("*") if f.is_file()) / (1024 * 1024)
            last_saved = ""
            if sandbox_path.exists():
                last_saved = str(sandbox_path.stat().st_mtime)
            worlds.append(SavedWorld(
                name=entry.name,
                path=str(entry),
                last_saved=last_saved,
                size_mb=round(size_mb, 2),
            ))
    return sorted(worlds, key=lambda w: w.name)


def _parse_session_settings(elem: ET.Element) -> SessionSettings:
    return SessionSettings(
        game_mode=_int(elem, "GameMode", 0),
        inventory_size_multiplier=_float(elem, "InventorySizeMultiplier", 3.0),
        blocks_inventory_size_multiplier=_float(elem, "BlocksInventorySizeMultiplier", 1.0),
        assembler_speed_multiplier=_float(elem, "AssemblerSpeedMultiplier", 3.0),
        assembler_efficiency_multiplier=_float(elem, "AssemblerEfficiencyMultiplier", 3.0),
        refinery_speed_multiplier=_float(elem, "RefinerySpeedMultiplier", 3.0),
        online_mode=_int(elem, "OnlineMode", 3),
        max_players=_int(elem, "MaxPlayers", 4),
        max_floating_objects=_int(elem, "MaxFloatingObjects", 100),
        total_bot_limit=_int(elem, "TotalBotLimit", 32),
        max_backup_saves=_int(elem, "MaxBackupSaves", 5),
        max_grid_size=_int(elem, "MaxGridSize", 50000),
        max_blocks_per_player=_int(elem, "MaxBlocksPerPlayer", 100000),
        total_pcu=_int(elem, "TotalPCU", 600000),
        pirate_pcu=_int(elem, "PiratePCU", 25000),
        global_encounter_pcu=_int(elem, "GlobalEncounterPCU", 25000),
        max_factions_count=_int(elem, "MaxFactionsCount", 0),
        enable_remote_block_removal=_bool(elem, "EnableRemoteBlockRemoval", True),
        environment_hostility=_int(elem, "EnvironmentHostility", 1),
        auto_healing=_bool(elem, "AutoHealing", True),
        enable_copy_paste=_bool(elem, "EnableCopyPaste", True),
        weapons_enabled=_bool(elem, "WeaponsEnabled", True),
        show_player_names_on_hud=_bool(elem, "ShowPlayerNamesOnHud", True),
        thruster_damage=_bool(elem, "ThrusterDamage", True),
        cargo_ships_enabled=_bool(elem, "CargoShipsEnabled", True),
        enable_spectator=_bool(elem, "EnableSpectator", False),
        world_size_km=_int(elem, "WorldSizeKm", 0),
        respawn_ship_delete=_bool(elem, "RespawnShipDelete", False),
        reset_ownership=_bool(elem, "ResetOwnership", False),
        welder_speed_multiplier=_float(elem, "WelderSpeedMultiplier", 2.0),
        grinder_speed_multiplier=_float(elem, "GrinderSpeedMultiplier", 2.0),
        realistic_sound=_bool(elem, "RealisticSound", False),
        hack_speed_multiplier=_float(elem, "HackSpeedMultiplier", 0.33),
        permanent_death=_bool_nullable(elem, "PermanentDeath"),
        enable_saving=_bool(elem, "EnableSaving", True),
        infinite_ammo=_bool(elem, "InfiniteAmmo", False),
        enable_container_drops=_bool(elem, "EnableContainerDrops", True),
        spawn_ship_time_multiplier=_float(elem, "SpawnShipTimeMultiplier", 0.0),
        procedural_density=_float(elem, "ProceduralDensity", 0.0),
        procedural_seed=_int(elem, "ProceduralSeed", 0),
        destructible_blocks=_bool(elem, "DestructibleBlocks", True),
        enable_ingame_scripts=_bool(elem, "EnableIngameScripts", True),
        view_distance=_int(elem, "ViewDistance", 15000),
        enable_tool_shake=_bool(elem, "EnableToolShake", False),
        voxel_generator_version=_int(elem, "VoxelGeneratorVersion", 4),
        enable_oxygen=_bool(elem, "EnableOxygen", False),
        enable_oxygen_pressurization=_bool(elem, "EnableOxygenPressurization", False),
        enable_3rd_person_view=_bool(elem, "Enable3rdPersonView", True),
        enable_encounters=_bool(elem, "EnableEncounters", True),
        enable_convert_to_station=_bool(elem, "EnableConvertToStation", True),
        station_voxel_support=_bool(elem, "StationVoxelSupport", False),
        enable_sun_rotation=_bool(elem, "EnableSunRotation", True),
        enable_respawn_ships=_bool(elem, "EnableRespawnShips", True),
        scenario_edit_mode=_bool(elem, "ScenarioEditMode", False),
        scenario=_bool(elem, "Scenario", False),
        can_join_running=_bool(elem, "CanJoinRunning", False),
        physics_iterations=_int(elem, "PhysicsIterations", 8),
        sun_rotation_interval_minutes=_float(elem, "SunRotationIntervalMinutes", 120.0),
        enable_jetpack=_bool(elem, "EnableJetpack", True),
        spawn_with_tools=_bool(elem, "SpawnWithTools", True),
        enable_voxel_destruction=_bool(elem, "EnableVoxelDestruction", True),
        max_drones=_int(elem, "MaxDrones", 5),
        enable_drones=_bool(elem, "EnableDrones", True),
        enable_wolfs=_bool(elem, "EnableWolfs", True),
        enable_spiders=_bool(elem, "EnableSpiders", False),
        flora_density_multiplier=_float(elem, "FloraDensityMultiplier", 1.0),
        enable_scripter_role=_bool(elem, "EnableScripterRole", False),
        min_drop_container_respawn_time=_int(elem, "MinDropContainerRespawnTime", 5),
        max_drop_container_respawn_time=_int(elem, "MaxDropContainerRespawnTime", 20),
        enable_turrets_friendly_fire=_bool(elem, "EnableTurretsFriendlyFire", False),
        enable_subgrid_damage=_bool(elem, "EnableSubgridDamage", False),
        sync_distance=_int(elem, "SyncDistance", 3000),
        experimental_mode=_bool(elem, "ExperimentalMode", False),
        adaptive_simulation_quality=_bool(elem, "AdaptiveSimulationQuality", True),
        enable_voxel_hand=_bool(elem, "EnableVoxelHand", False),
        remove_old_identities_h=_int(elem, "RemoveOldIdentitiesH", 0),
        trash_removal_enabled=_bool(elem, "TrashRemovalEnabled", True),
        stop_grids_period_min=_int(elem, "StopGridsPeriodMin", 15),
        trash_flags_value=_int(elem, "TrashFlagsValue", 7706),
        afk_timeout_min=_int(elem, "AFKTimeountMin", 0),
        block_count_threshold=_int(elem, "BlockCountThreshold", 20),
        player_distance_threshold=_float(elem, "PlayerDistanceThreshold", 500.0),
        optimal_grid_count=_int(elem, "OptimalGridCount", 0),
        player_inactivity_threshold=_float(elem, "PlayerInactivityThreshold", 0.0),
        player_character_removal_threshold=_int(elem, "PlayerCharacterRemovalThreshold", 15),
        voxel_trash_removal_enabled=_bool(elem, "VoxelTrashRemovalEnabled", False),
        voxel_player_distance_threshold=_float(elem, "VoxelPlayerDistanceThreshold", 5000.0),
        voxel_grid_distance_threshold=_float(elem, "VoxelGridDistanceThreshold", 5000.0),
        voxel_age_threshold=_int(elem, "VoxelAgeThreshold", 24),
        enable_research=_bool(elem, "EnableResearch", False),
        enable_good_bot_hints=_bool(elem, "EnableGoodBotHints", True),
        optimal_spawn_distance=_float(elem, "OptimalSpawnDistance", 16000.0),
        enable_autorespawn=_bool(elem, "EnableAutorespawn", True),
        enable_bounty_contracts=_bool(elem, "EnableBountyContracts", True),
        enable_supergridding=_bool(elem, "EnableSupergridding", False),
        enable_economy=_bool(elem, "EnableEconomy", False),
        deposits_count_coefficient=_float(elem, "DepositsCountCoefficient", 2.0),
        deposit_size_denominator=_float(elem, "DepositSizeDenominator", 30.0),
        weather_system=_bool(elem, "WeatherSystem", True),
        weather_lighting_damage=_bool(elem, "WeatherLightingDamage", False),
        harvest_ratio_multiplier=_float(elem, "HarvestRatioMultiplier", 1.0),
        trade_factions_count=_int(elem, "TradeFactionsCount", 10),
        stations_distance_inner_radius=_float(elem, "StationsDistanceInnerRadius", 5000000.0),
        stations_distance_outer_radius_start=_float(elem, "StationsDistanceOuterRadiusStart", 5000000.0),
        stations_distance_outer_radius_end=_float(elem, "StationsDistanceOuterRadiusEnd", 10000000.0),
        economy_tick_in_seconds=_int(elem, "EconomyTickInSeconds", 300),
        npc_grid_claim_time_limit=_int(elem, "NPCGridClaimTimeLimit", 120),
        simplified_simulation=_bool(elem, "SimplifiedSimulation", False),
        enable_pcu_trading=_bool(elem, "EnablePcuTrading", True),
        family_sharing=_bool(elem, "FamilySharing", True),
        enable_selective_physics_updates=_bool(elem, "EnableSelectivePhysicsUpdates", False),
        predefined_asteroids=_bool(elem, "PredefinedAsteroids", True),
        use_console_pcu=_bool(elem, "UseConsolePCU", False),
        max_planets=_int(elem, "MaxPlanets", 99),
        offensive_words_filtering=_bool(elem, "OffensiveWordsFiltering", False),
    )


def _write_session_settings(parent: ET.Element, ss: SessionSettings):
    elem = ET.SubElement(parent, "SessionSettings")
    _set_elem(elem, "GameMode", str(ss.game_mode))
    _set_elem(elem, "InventorySizeMultiplier", str(ss.inventory_size_multiplier))
    _set_elem(elem, "BlocksInventorySizeMultiplier", str(ss.blocks_inventory_size_multiplier))
    _set_elem(elem, "AssemblerSpeedMultiplier", str(ss.assembler_speed_multiplier))
    _set_elem(elem, "AssemblerEfficiencyMultiplier", str(ss.assembler_efficiency_multiplier))
    _set_elem(elem, "RefinerySpeedMultiplier", str(ss.refinery_speed_multiplier))
    _set_elem(elem, "OnlineMode", str(ss.online_mode))
    _set_elem(elem, "MaxPlayers", str(ss.max_players))
    _set_elem(elem, "MaxFloatingObjects", str(ss.max_floating_objects))
    _set_elem(elem, "TotalBotLimit", str(ss.total_bot_limit))
    _set_elem(elem, "MaxBackupSaves", str(ss.max_backup_saves))
    _set_elem(elem, "MaxGridSize", str(ss.max_grid_size))
    _set_elem(elem, "MaxBlocksPerPlayer", str(ss.max_blocks_per_player))
    _set_elem(elem, "TotalPCU", str(ss.total_pcu))
    _set_elem(elem, "PiratePCU", str(ss.pirate_pcu))
    _set_elem(elem, "GlobalEncounterPCU", str(ss.global_encounter_pcu))
    _set_elem(elem, "MaxFactionsCount", str(ss.max_factions_count))
    _set_elem(elem, "EnableRemoteBlockRemoval", _bstr(ss.enable_remote_block_removal))
    _set_elem(elem, "EnvironmentHostility", str(ss.environment_hostility))
    _set_elem(elem, "AutoHealing", _bstr(ss.auto_healing))
    _set_elem(elem, "EnableCopyPaste", _bstr(ss.enable_copy_paste))
    _set_elem(elem, "WeaponsEnabled", _bstr(ss.weapons_enabled))
    _set_elem(elem, "ShowPlayerNamesOnHud", _bstr(ss.show_player_names_on_hud))
    _set_elem(elem, "ThrusterDamage", _bstr(ss.thruster_damage))
    _set_elem(elem, "CargoShipsEnabled", _bstr(ss.cargo_ships_enabled))
    _set_elem(elem, "EnableSpectator", _bstr(ss.enable_spectator))
    _set_elem(elem, "WorldSizeKm", str(ss.world_size_km))
    _set_elem(elem, "RespawnShipDelete", _bstr(ss.respawn_ship_delete))
    _set_elem(elem, "ResetOwnership", _bstr(ss.reset_ownership))
    _set_elem(elem, "WelderSpeedMultiplier", str(ss.welder_speed_multiplier))
    _set_elem(elem, "GrinderSpeedMultiplier", str(ss.grinder_speed_multiplier))
    _set_elem(elem, "RealisticSound", _bstr(ss.realistic_sound))
    _set_elem(elem, "HackSpeedMultiplier", str(ss.hack_speed_multiplier))
    if ss.permanent_death is not None:
        _set_elem(elem, "PermanentDeath", _bstr(ss.permanent_death))
    _set_elem(elem, "EnableSaving", _bstr(ss.enable_saving))
    _set_elem(elem, "InfiniteAmmo", _bstr(ss.infinite_ammo))
    _set_elem(elem, "EnableContainerDrops", _bstr(ss.enable_container_drops))
    _set_elem(elem, "SpawnShipTimeMultiplier", str(ss.spawn_ship_time_multiplier))
    _set_elem(elem, "ProceduralDensity", str(ss.procedural_density))
    _set_elem(elem, "ProceduralSeed", str(ss.procedural_seed))
    _set_elem(elem, "DestructibleBlocks", _bstr(ss.destructible_blocks))
    _set_elem(elem, "EnableIngameScripts", _bstr(ss.enable_ingame_scripts))
    _set_elem(elem, "ViewDistance", str(ss.view_distance))
    _set_elem(elem, "EnableToolShake", _bstr(ss.enable_tool_shake))
    _set_elem(elem, "VoxelGeneratorVersion", str(ss.voxel_generator_version))
    _set_elem(elem, "EnableOxygen", _bstr(ss.enable_oxygen))
    _set_elem(elem, "EnableOxygenPressurization", _bstr(ss.enable_oxygen_pressurization))
    _set_elem(elem, "Enable3rdPersonView", _bstr(ss.enable_3rd_person_view))
    _set_elem(elem, "EnableEncounters", _bstr(ss.enable_encounters))
    _set_elem(elem, "EnableConvertToStation", _bstr(ss.enable_convert_to_station))
    _set_elem(elem, "StationVoxelSupport", _bstr(ss.station_voxel_support))
    _set_elem(elem, "EnableSunRotation", _bstr(ss.enable_sun_rotation))
    _set_elem(elem, "EnableRespawnShips", _bstr(ss.enable_respawn_ships))
    _set_elem(elem, "ScenarioEditMode", _bstr(ss.scenario_edit_mode))
    _set_elem(elem, "Scenario", _bstr(ss.scenario))
    _set_elem(elem, "CanJoinRunning", _bstr(ss.can_join_running))
    _set_elem(elem, "PhysicsIterations", str(ss.physics_iterations))
    _set_elem(elem, "SunRotationIntervalMinutes", str(ss.sun_rotation_interval_minutes))
    _set_elem(elem, "EnableJetpack", _bstr(ss.enable_jetpack))
    _set_elem(elem, "SpawnWithTools", _bstr(ss.spawn_with_tools))
    _set_elem(elem, "EnableVoxelDestruction", _bstr(ss.enable_voxel_destruction))
    _set_elem(elem, "MaxDrones", str(ss.max_drones))
    _set_elem(elem, "EnableDrones", _bstr(ss.enable_drones))
    _set_elem(elem, "EnableWolfs", _bstr(ss.enable_wolfs))
    _set_elem(elem, "EnableSpiders", _bstr(ss.enable_spiders))
    _set_elem(elem, "FloraDensityMultiplier", str(ss.flora_density_multiplier))
    _set_elem(elem, "EnableScripterRole", _bstr(ss.enable_scripter_role))
    _set_elem(elem, "MinDropContainerRespawnTime", str(ss.min_drop_container_respawn_time))
    _set_elem(elem, "MaxDropContainerRespawnTime", str(ss.max_drop_container_respawn_time))
    _set_elem(elem, "EnableTurretsFriendlyFire", _bstr(ss.enable_turrets_friendly_fire))
    _set_elem(elem, "EnableSubgridDamage", _bstr(ss.enable_subgrid_damage))
    _set_elem(elem, "SyncDistance", str(ss.sync_distance))
    _set_elem(elem, "ExperimentalMode", _bstr(ss.experimental_mode))
    _set_elem(elem, "AdaptiveSimulationQuality", _bstr(ss.adaptive_simulation_quality))
    _set_elem(elem, "EnableVoxelHand", _bstr(ss.enable_voxel_hand))
    _set_elem(elem, "RemoveOldIdentitiesH", str(ss.remove_old_identities_h))
    _set_elem(elem, "TrashRemovalEnabled", _bstr(ss.trash_removal_enabled))
    _set_elem(elem, "StopGridsPeriodMin", str(ss.stop_grids_period_min))
    _set_elem(elem, "TrashFlagsValue", str(ss.trash_flags_value))
    _set_elem(elem, "AFKTimeountMin", str(ss.afk_timeout_min))
    _set_elem(elem, "BlockCountThreshold", str(ss.block_count_threshold))
    _set_elem(elem, "PlayerDistanceThreshold", str(ss.player_distance_threshold))
    _set_elem(elem, "OptimalGridCount", str(ss.optimal_grid_count))
    _set_elem(elem, "PlayerInactivityThreshold", str(ss.player_inactivity_threshold))
    _set_elem(elem, "PlayerCharacterRemovalThreshold", str(ss.player_character_removal_threshold))
    _set_elem(elem, "VoxelTrashRemovalEnabled", _bstr(ss.voxel_trash_removal_enabled))
    _set_elem(elem, "VoxelPlayerDistanceThreshold", str(ss.voxel_player_distance_threshold))
    _set_elem(elem, "VoxelGridDistanceThreshold", str(ss.voxel_grid_distance_threshold))
    _set_elem(elem, "VoxelAgeThreshold", str(ss.voxel_age_threshold))
    _set_elem(elem, "EnableResearch", _bstr(ss.enable_research))
    _set_elem(elem, "EnableGoodBotHints", _bstr(ss.enable_good_bot_hints))
    _set_elem(elem, "OptimalSpawnDistance", str(ss.optimal_spawn_distance))
    _set_elem(elem, "EnableAutorespawn", _bstr(ss.enable_autorespawn))
    _set_elem(elem, "EnableBountyContracts", _bstr(ss.enable_bounty_contracts))
    _set_elem(elem, "EnableSupergridding", _bstr(ss.enable_supergridding))
    _set_elem(elem, "EnableEconomy", _bstr(ss.enable_economy))
    _set_elem(elem, "DepositsCountCoefficient", str(ss.deposits_count_coefficient))
    _set_elem(elem, "DepositSizeDenominator", str(ss.deposit_size_denominator))
    _set_elem(elem, "WeatherSystem", _bstr(ss.weather_system))
    _set_elem(elem, "WeatherLightingDamage", _bstr(ss.weather_lighting_damage))
    _set_elem(elem, "HarvestRatioMultiplier", str(ss.harvest_ratio_multiplier))
    _set_elem(elem, "TradeFactionsCount", str(ss.trade_factions_count))
    _set_elem(elem, "StationsDistanceInnerRadius", str(ss.stations_distance_inner_radius))
    _set_elem(elem, "StationsDistanceOuterRadiusStart", str(ss.stations_distance_outer_radius_start))
    _set_elem(elem, "StationsDistanceOuterRadiusEnd", str(ss.stations_distance_outer_radius_end))
    _set_elem(elem, "EconomyTickInSeconds", str(ss.economy_tick_in_seconds))
    _set_elem(elem, "NPCGridClaimTimeLimit", str(ss.npc_grid_claim_time_limit))
    _set_elem(elem, "SimplifiedSimulation", _bstr(ss.simplified_simulation))
    _set_elem(elem, "EnablePcuTrading", _bstr(ss.enable_pcu_trading))
    _set_elem(elem, "FamilySharing", _bstr(ss.family_sharing))
    _set_elem(elem, "EnableSelectivePhysicsUpdates", _bstr(ss.enable_selective_physics_updates))
    _set_elem(elem, "PredefinedAsteroids", _bstr(ss.predefined_asteroids))
    _set_elem(elem, "UseConsolePCU", _bstr(ss.use_console_pcu))
    _set_elem(elem, "MaxPlanets", str(ss.max_planets))
    _set_elem(elem, "OffensiveWordsFiltering", _bstr(ss.offensive_words_filtering))


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


def _bool_nullable(parent: ET.Element, tag: str) -> bool | None:
    elem = parent.find(tag)
    if elem is None:
        return None
    xsi_nil = elem.get("{http://www.w3.org/2001/XMLSchema-instance}nil")
    if xsi_nil == "true":
        return None
    if elem.text:
        return elem.text.lower() == "true"
    return None


def _int(parent: ET.Element, tag: str, default: int) -> int:
    val = _text(parent, tag, None)
    if val is None:
        return default
    try:
        return int(val)
    except ValueError:
        return default


def _float(parent: ET.Element, tag: str, default: float) -> float:
    val = _text(parent, tag, None)
    if val is None:
        return default
    try:
        return float(val)
    except ValueError:
        return default


def _text_list(parent: ET.Element, tag: str, item_tag: str) -> list[str]:
    elem = parent.find(tag)
    if elem is None:
        return []
    return [e.text for e in elem.findall(item_tag) if e.text]


def _int_list(parent: ET.Element, tag: str, item_tag: str) -> list[int]:
    return [int(v) for v in _text_list(parent, tag, item_tag)]


def _set_elem(parent: ET.Element, tag: str, text: str):
    e = ET.SubElement(parent, tag)
    e.text = text


def _write_list(parent: ET.Element, tag: str, item_tag: str, items: list[str]):
    elem = ET.SubElement(parent, tag)
    for item in items:
        e = ET.SubElement(elem, item_tag)
        e.text = item


def _bstr(val: bool) -> str:
    return "true" if val else "false"
