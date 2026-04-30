"""Pydantic models matching Space Engineers Dedicated Server configuration."""

from enum import IntEnum
from pydantic import BaseModel, Field


class GameModeEnum(IntEnum):
    CREATIVE = 0
    SURVIVAL = 1


class OnlineModeEnum(IntEnum):
    OFFLINE = 0
    PRIVATE = 1
    FRIENDS = 2
    PUBLIC = 3


class EnvironmentHostilityEnum(IntEnum):
    SAFE = 0
    NORMAL = 1
    CATACLYSM = 2
    CATACLYSM_UNREAL = 3


class SessionSettings(BaseModel):
    game_mode: int = GameModeEnum.SURVIVAL
    inventory_size_multiplier: float = 3.0
    blocks_inventory_size_multiplier: float = 1.0
    assembler_speed_multiplier: float = 3.0
    assembler_efficiency_multiplier: float = 3.0
    refinery_speed_multiplier: float = 3.0
    online_mode: int = OnlineModeEnum.PUBLIC
    max_players: int = 4
    max_floating_objects: int = 100
    total_bot_limit: int = 32
    max_backup_saves: int = 5
    max_grid_size: int = 50000
    max_blocks_per_player: int = 100000
    total_pcu: int = 600000
    pirate_pcu: int = 25000
    global_encounter_pcu: int = 25000
    max_factions_count: int = 0
    enable_remote_block_removal: bool = True
    environment_hostility: int = EnvironmentHostilityEnum.NORMAL
    auto_healing: bool = True
    enable_copy_paste: bool = True
    weapons_enabled: bool = True
    show_player_names_on_hud: bool = True
    thruster_damage: bool = True
    cargo_ships_enabled: bool = True
    enable_spectator: bool = False
    world_size_km: int = 0
    respawn_ship_delete: bool = False
    reset_ownership: bool = False
    welder_speed_multiplier: float = 2.0
    grinder_speed_multiplier: float = 2.0
    realistic_sound: bool = False
    hack_speed_multiplier: float = 0.33
    permanent_death: bool | None = False
    enable_saving: bool = True
    infinite_ammo: bool = False
    enable_container_drops: bool = True
    spawn_ship_time_multiplier: float = 0.0
    procedural_density: float = 0.0
    procedural_seed: int = 0
    destructible_blocks: bool = True
    enable_ingame_scripts: bool = True
    view_distance: int = 15000
    enable_tool_shake: bool = False
    voxel_generator_version: int = 4
    enable_oxygen: bool = False
    enable_oxygen_pressurization: bool = False
    enable_3rd_person_view: bool = True
    enable_encounters: bool = True
    enable_convert_to_station: bool = True
    station_voxel_support: bool = False
    enable_sun_rotation: bool = True
    enable_respawn_ships: bool = True
    scenario_edit_mode: bool = False
    scenario: bool = False
    can_join_running: bool = False
    physics_iterations: int = 8
    sun_rotation_interval_minutes: float = 120.0
    enable_jetpack: bool = True
    spawn_with_tools: bool = True
    enable_voxel_destruction: bool = True
    max_drones: int = 5
    enable_drones: bool = True
    enable_wolfs: bool = True
    enable_spiders: bool = False
    flora_density_multiplier: float = 1.0
    enable_scripter_role: bool = False
    min_drop_container_respawn_time: int = 5
    max_drop_container_respawn_time: int = 20
    enable_turrets_friendly_fire: bool = False
    enable_subgrid_damage: bool = False
    sync_distance: int = 3000
    experimental_mode: bool = False
    adaptive_simulation_quality: bool = True
    enable_voxel_hand: bool = False
    remove_old_identities_h: int = 0
    trash_removal_enabled: bool = True
    stop_grids_period_min: int = 15
    trash_flags_value: int = 7706
    afk_timeout_min: int = 0
    block_count_threshold: int = 20
    player_distance_threshold: float = 500.0
    optimal_grid_count: int = 0
    player_inactivity_threshold: float = 0.0
    player_character_removal_threshold: int = 15
    voxel_trash_removal_enabled: bool = False
    voxel_player_distance_threshold: float = 5000.0
    voxel_grid_distance_threshold: float = 5000.0
    voxel_age_threshold: int = 24
    enable_research: bool = False
    enable_good_bot_hints: bool = True
    optimal_spawn_distance: float = 16000.0
    enable_autorespawn: bool = True
    enable_bounty_contracts: bool = True
    enable_supergridding: bool = False
    enable_economy: bool = False
    deposits_count_coefficient: float = 2.0
    deposit_size_denominator: float = 30.0
    weather_system: bool = True
    weather_lighting_damage: bool = False
    harvest_ratio_multiplier: float = 1.0
    trade_factions_count: int = 10
    stations_distance_inner_radius: float = 5000000.0
    stations_distance_outer_radius_start: float = 5000000.0
    stations_distance_outer_radius_end: float = 10000000.0
    economy_tick_in_seconds: int = 300
    npc_grid_claim_time_limit: int = 120
    simplified_simulation: bool = False
    enable_pcu_trading: bool = True
    family_sharing: bool = True
    enable_selective_physics_updates: bool = False
    predefined_asteroids: bool = True
    use_console_pcu: bool = False
    max_planets: int = 99
    offensive_words_filtering: bool = False


class DedicatedConfig(BaseModel):
    session_settings: SessionSettings = Field(default_factory=SessionSettings)
    load_world: str = ""
    cross_platform: bool = False
    ip: str = "0.0.0.0"
    steam_port: int = 8766
    server_port: int = 27016
    asteroid_amount: int = 4
    administrators: list[str] = Field(default_factory=list)
    banned: list[int] = Field(default_factory=list)
    reserved: list[int] = Field(default_factory=list)
    group_id: int = 0
    server_name: str = ""
    world_name: str = ""
    verbose_network_logging: bool = False
    pause_game_when_empty: bool = False
    message_of_the_day: str = ""
    message_of_the_day_url: str = ""
    auto_restart_enabled: bool = True
    auto_restart_time_in_min: int = 0
    auto_restart_save: bool = True
    auto_update_enabled: bool = False
    auto_update_check_interval_in_min: int = 10
    auto_update_restart_delay_in_min: int = 15
    auto_update_steam_branch: str = ""
    auto_update_branch_password: str = ""
    ignore_last_session: bool = False
    premade_checkpoint_path: str = ""
    server_description: str = ""
    server_password_hash: str = ""
    server_password_salt: str = ""
    remote_api_enabled: bool = True
    remote_security_key: str = ""
    remote_api_port: int = 8080
    remote_api_ip: str = ""
    plugins: list[str] = Field(default_factory=list)
    watcher_interval: float = 30.0
    watcher_simulation_speed_minimum: float = 0.05
    manual_action_delay: int = 5
    manual_action_chat_message: str = "Server will be shut down in {0} min(s)."
    autodetect_dependencies: bool = True
    save_chat_to_log: bool = False
    network_type: str = "steam"
    console_compatibility: bool = False
    network_parameters: list[str] = Field(default_factory=list)
    chat_anti_spam_enabled: bool = True
    same_message_timeout: int = 30
    spam_messages_time: float = 0.5
    spam_messages_timeout: int = 60
    dedicated_id: int = 0


class SavedWorld(BaseModel):
    name: str = ""
    path: str = ""
    last_saved: str = ""
    size_mb: float = 0.0


class ServerState(BaseModel):
    is_running: bool = False
    server_name: str = ""
    world_name: str = ""
    players_online: int = 0
    max_players: int = 0
    sim_speed: float = 0.0
    sim_cpu_load: float = 0.0
    server_cpu_load: float = 0.0
    used_pcu: int = 0
    total_pcu: int = 0
    uptime_seconds: int = 0
    game_version: str = ""
    mods_loaded: int = 0
    plugins_loaded: int = 0


class PlayerInfo(BaseModel):
    steam_id: int = 0
    display_name: str = ""
    faction: str = ""
    is_admin: bool = False
    ping_ms: int = 0


class ChatMessage(BaseModel):
    timestamp: str = ""
    sender: str = ""
    message: str = ""
