using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// The single declarative source of truth for every editable config option,
/// hand-transcribed from the decompiled <c>MyConfigDedicatedData</c> and
/// <c>MyObjectBuilder_SessionSettings</c> (build 1.209.024) and cross-checked
/// against Quasar's metadata. Field names (including Keen's typos), defaults,
/// and enum XML names are exact — they must match the game byte-for-byte. See
/// Docs/ConfigTerminal.md §6.
/// </summary>
internal static class OptionRegistry
{
    // --- enum choices (serialized BY NAME; values verified against decompiled enums) ---

    private static readonly EnumChoice[] GameMode =
    {
        new(0, "Creative", "Creative"),
        new(1, "Survival", "Survival"),
    };

    private static readonly EnumChoice[] OnlineMode =
    {
        new(0, "OFFLINE", "Offline"),
        new(1, "PUBLIC", "Public"),
        new(2, "FRIENDS", "Friends"),
        new(3, "PRIVATE", "Private"),
    };

    private static readonly EnumChoice[] EnvironmentHostility =
    {
        new(0, "SAFE", "Safe"),
        new(1, "NORMAL", "Normal"),
        new(2, "CATACLYSM", "Cataclysm"),
        new(3, "CATACLYSM_UNREAL", "Cataclysm Unreal"),
    };

    private static readonly EnumChoice[] BlockLimits =
    {
        // Note: PER_FACTION precedes PER_PLAYER in the enum.
        new(0, "NONE", "None"),
        new(1, "GLOBALLY", "Globally"),
        new(2, "PER_FACTION", "Per Faction"),
        new(3, "PER_PLAYER", "Per Player"),
    };

    private static readonly EnumChoice[] LimitBlocksBy =
    {
        new(0, "BlockPairName", "Block Pair Name"),
        new(1, "Tag", "Tag"),
    };

    private static List<OptionDefinition> dedicated;
    private static List<OptionDefinition> session;

    public static IReadOnlyList<OptionDefinition> DedicatedOptions => dedicated ??= BuildDedicated();
    public static IReadOnlyList<OptionDefinition> SessionOptions => session ??= BuildSession();

    public static IEnumerable<OptionDefinition> All => DedicatedOptions.Concat(SessionOptions);

    public static OptionDefinition ById(string id) => All.FirstOrDefault(o => o.Id == id);

    public static IEnumerable<string> Categories(OptionScope scope) =>
        (scope == OptionScope.DedicatedRoot ? DedicatedOptions : SessionOptions)
        .Select(o => o.Category)
        .Distinct();

    // --- dedicated root options (MyConfigDedicatedData) ---

    private static List<OptionDefinition> BuildDedicated()
    {
        var b = new Builder(OptionScope.DedicatedRoot);
        const Liveness Live = Liveness.LiveViaReload;

        b.Cat("Identity");
        b.Text("ServerName", "", Live, "Name shown in the server browser.");
        b.Multiline("ServerDescription", "", Live, "Description shown to clients in the browser.");
        b.Multiline("MessageOfTheDay", "", Live, "Message shown to players on join; recomputed per join.");
        b.Text("MessageOfTheDayUrl", "", Live);
        b.UInt("GroupID", "0", help: "Steam group id restricting who may join (0 = open).");

        b.Cat("Network");
        b.Text("IP", "0.0.0.0", help: "Bind address for the game port.");
        b.Int("ServerPort", "27016", 0, 65535, help: "Game (UDP) port.");
        b.Int("SteamPort", "8766", 0, 65535, help: "Steam query port.");
        b.Text("NetworkType", "steam", help: "Transport: steam or EOS.");
        b.Bool("CrossPlatform", "false");
        b.Bool("ConsoleCompatibility", "false");
        b.Bool("VerboseNetworkLogging", "false");
        b.StringList("NetworkParameters", "Parameter");

        b.Cat("Remote API");
        b.Bool("RemoteApiEnabled", "true");
        b.Int("RemoteApiPort", "8080", 0, 65535);
        b.Text("RemoteApiIP", "");

        b.Cat("World Selection");
        b.Text("LoadWorld", "", help: "World folder to load when no LastSession is set. Usually left empty.");
        b.Bool("IgnoreLastSession", "false", help: "When true the DS ignores Saves/LastSession.sbl.");
        b.Text("PremadeCheckpointPath", "", help: "Template world path the DS materializes into a new world on next start.");
        b.Text("WorldName", "", help: "Name given to a newly created world.");
        b.Int("AsteroidAmount", "4");
        b.Bool("PauseGameWhenEmpty", "false", Live);

        b.Cat("Auto Restart");
        b.Bool("AutoRestartEnabled", "true");
        // Keen's typo is the real element name — do not "fix" it.
        b.Int("AutoRestatTimeInMin", "0", 0, null, help: "Auto-restart interval in minutes (0 = off). Field name keeps the vanilla typo.");
        b.Bool("AutoRestartSave", "true");

        b.Cat("Auto Update");
        b.Bool("AutoUpdateEnabled", "false");
        b.Int("AutoUpdateCheckIntervalInMin", "10");
        b.Int("AutoUpdateRestartDelayInMin", "15");
        b.Text("AutoUpdateSteamBranch", "");
        b.Text("AutoUpdateBranchPassword", "");

        b.Cat("Watchdog");
        b.Float("WatcherInterval", "30");
        b.Float("WatcherSimulationSpeedMinimum", "0.05");
        b.Int("ManualActionDelay", "5");
        b.Text("ManualActionChatMessage", "Server will be shut down in {0} min(s).");

        b.Cat("Chat & Anti-Spam");
        b.Bool("SaveChatToLog", "false");
        b.Bool("ChatAntiSpamEnabled", "true", Live);
        b.Int("SameMessageTimeout", "30", Live);
        b.Float("SpamMessagesTime", "0.5", Live);
        b.Int("SpamMessagesTimeout", "60", Live);

        b.Cat("Mods & Plugins");
        b.Bool("AutodetectDependencies", "true");

        return b.Options;
    }

    // --- session settings (MyObjectBuilder_SessionSettings) ---

    private static List<OptionDefinition> BuildSession()
    {
        var b = new Builder(OptionScope.Session);

        b.Cat("Core");
        b.Enum("GameMode", GameMode, "Creative", help: "Creative or Survival.");
        b.Enum("OnlineMode", OnlineMode, "OFFLINE", help: "Visibility: Offline/Public/Friends/Private.");
        b.Short("MaxPlayers", "4", 0, 128, help: "Player slots. Must be non-zero for the DS to create/load a world.");
        b.Bool("EnableSaving", "true");
        b.UInt("AutoSaveInMinutes", "5");
        b.Short("MaxBackupSaves", "5");
        b.Bool("WeaponsEnabled", "true");
        b.Bool("InfiniteAmmo", "false");
        b.Bool("ThrusterDamage", "true");
        b.Bool("EnableSpectator", "false", experimental: true);

        b.Cat("Multipliers");
        b.Float("InventorySizeMultiplier", "3");
        b.Float("BlocksInventorySizeMultiplier", "1");
        b.Float("AssemblerSpeedMultiplier", "3");
        b.Float("AssemblerEfficiencyMultiplier", "3");
        b.Float("RefinerySpeedMultiplier", "3");
        b.Float("WelderSpeedMultiplier", "2");
        b.Float("GrinderSpeedMultiplier", "2");
        b.Float("HackSpeedMultiplier", "0.33");
        b.Float("HarvestRatioMultiplier", "1");
        b.Float("CharacterSpeedMultiplier", "1");
        b.Float("EnvironmentDamageMultiplier", "1");

        b.Cat("Block Limits");
        b.Enum("BlockLimitsEnabled", BlockLimits, "NONE", experimental: true,
            experimentalRule: "BlockLimitsEnabled == NONE",
            help: "How build limits are scoped. NONE forces experimental mode.");
        b.Enum("LimitBlocksBy", LimitBlocksBy, "BlockPairName");
        b.Int("MaxGridSize", "50000");
        b.Int("MaxBlocksPerPlayer", "100000");
        b.Int("TotalPCU", "600000", experimentalRule: "TotalPCU above safe cap");
        b.Int("MaxFactionsCount", "0");
        b.BlockLimits("BlockTypeLimits");

        b.Cat("Environment");
        b.Enum("EnvironmentHostility", EnvironmentHostility, "NORMAL");
        b.Int("WorldSizeKm", "0");
        b.Int("MinimumWorldSize", "0");
        b.Bool("EnableOxygen", "false");
        b.Bool("EnableOxygenPressurization", "false");
        b.Bool("EnableSunRotation", "true");
        b.Float("SunRotationIntervalMinutes", "120");
        b.Bool("DestructibleBlocks", "true");
        b.Bool("EnableVoxelDestruction", "true");
        b.Bool("RealisticSound", "false");
        b.Int("PhysicsIterations", "8", experimentalRule: "PhysicsIterations != 8");
        b.Int("SyncDistance", "3000", experimentalRule: "SyncDistance > 3000");
        b.Int("ViewDistance", "15000");
        b.Int("VoxelGeneratorVersion", "4");
        b.Float("ProceduralDensity", "0", experimentalRule: "ProceduralDensity > 0.35");
        b.Int("ProceduralSeed", "0");
        b.Float("EncounterDensity", "0.35");
        b.Bool("WeatherSystem", "true");
        b.Bool("WeatherLightingDamage", "false");
        b.Bool("EnableRadiation", "true");
        b.Float("SolarRadiationIntensity", "0");
        b.Bool("EnableTurretsFriendlyFire", "false");
        b.Short("MaxFloatingObjects", "100");
        b.Float("DepositsCountCoefficient", "2");
        b.Float("DepositSizeDenominator", "30");
        b.Bool("ScrapEnabled", "true");
        b.Bool("TemporaryContainers", "true");
        b.Bool("PredefinedAsteroids", "true");
        b.Int("MaxPlanets", "99");
        b.Long("PrefetchShapeRayLengthLimit", "15000");
        b.Short("MaxCargoBags", "100");
        b.Int("BroadcastControllerMaxOfflineTransmitDistance", "200");
        b.Float("FoodConsumptionRate", "0");

        b.Cat("Players");
        b.Bool("AutoHealing", "true");
        b.Bool("EnableCopyPaste", "true");
        b.Bool("ShowPlayerNamesOnHud", "true");
        b.Bool("Enable3rdPersonView", "true");
        b.Bool("EnableJetpack", "true");
        b.Bool("SpawnWithTools", "true");
        b.Bool("EnableToolShake", "false");
        b.Bool("EnableRespawnShips", "true");
        b.Bool("EnableAutorespawn", "true");
        b.Bool("StartInRespawnScreen", "false");
        b.Bool("EnableSpaceSuitRespawn", "true");
        b.Bool("EnableReducedStatsOnRespawn", "true");
        b.Bool("EnableSurvivalBuffs", "true");
        b.Bool("EnableRecoil", "true");
        b.Bool("EnableGamepadAimAssist", "false");
        b.Bool("BlueprintShare", "true");
        b.Int("BlueprintShareTimeout", "30");
        b.Float("SpawnShipTimeMultiplier", "0");
        b.Float("OptimalSpawnDistance", "16000");
        b.Float("BackpackDespawnTimer", "5");
        b.Bool("EnablePcuTrading", "true");
        b.Bool("FamilySharing", "true");
        b.Bool("EnableBountyContracts", "true");

        b.Cat("NPCs");
        b.Bool("CargoShipsEnabled", "true");
        b.Bool("EnableEncounters", "true");
        b.Bool("EnableDrones", "true");
        b.Bool("EnableWolfs", "true");
        b.Bool("EnableSpiders", "false");
        b.Bool("EnableOrca", "true");
        b.Bool("EnablePlanetaryEncounters", "true");
        b.Int("PiratePCU", "25000");
        b.Int("GlobalEncounterPCU", "25000");
        b.Int("GlobalEncounterTimer", "15");
        b.Int("GlobalEncounterCap", "1");
        b.Bool("GlobalEncounterEnableRemovalTimer", "true");
        b.Int("GlobalEncounterMinRemovalTimer", "90");
        b.Int("GlobalEncounterMaxRemovalTimer", "180");
        b.Int("GlobalEncounterRemovalTimeClock", "30");
        b.Float("PlanetaryEncounterTimerMin", "15");
        b.Float("PlanetaryEncounterTimerMax", "30");
        b.Float("PlanetaryEncounterTimerFirst", "5");
        b.Int("PlanetaryEncounterExistingStructuresRange", "7000");
        b.Int("PlanetaryEncounterAreaLockdownRange", "10000");
        b.Int("PlanetaryEncounterDesiredSpawnRange", "6000");
        b.Int("PlanetaryEncounterPresenceRange", "20000");
        b.Float("PlanetaryEncounterDespawnTimeout", "120");
        b.Int("NPCGridClaimTimeLimit", "120");
        b.Float("ReputationDecayRate", "0.5");

        b.Cat("Economy");
        b.Bool("EnableEconomy", "false");
        b.Int("TradeFactionsCount", "10");
        b.Double("StationsDistanceInnerRadius", "5000000");
        b.Double("StationsDistanceOuterRadiusStart", "5000000");
        b.Double("StationsDistanceOuterRadiusEnd", "10000000");
        b.Int("EconomyTickInSeconds", "600");

        b.Cat("Trash Removal");
        b.Bool("TrashRemovalEnabled", "true");
        b.Int("TrashFlagsValue", "7706");
        b.Int("StopGridsPeriodMin", "15");
        b.Int("BlockCountThreshold", "20");
        b.Float("PlayerDistanceThreshold", "500");
        b.Int("OptimalGridCount", "0");
        b.Float("PlayerInactivityThreshold", "0");
        b.Int("PlayerCharacterRemovalThreshold", "15");
        b.Int("RemoveOldIdentitiesH", "0");
        // Keen's typo is the real element name.
        b.Int("AFKTimeountMin", "0", help: "AFK timeout in minutes (0 = off). Field name keeps the vanilla typo.");
        b.Bool("VoxelTrashRemovalEnabled", "false");
        b.Float("VoxelPlayerDistanceThreshold", "5000");
        b.Float("VoxelGridDistanceThreshold", "5000");
        b.Int("VoxelAgeThreshold", "24");
        b.Bool("EnableTrashSettingsPlatformOverride", "true");
        b.Bool("ResetForageableItems", "true");
        b.Int("ResetForageableItemsTimeM", "30");
        b.Int("ResetForageableItemsDistance", "3000");

        b.Cat("Grid Storage");
        b.Bool("GridStorageAllowsInventory", "false");
        b.Int("GridStorageMaxPerPlayer", "100");
        b.Float("GridStorageRetrievalTimeMaxMinutes", "30");
        b.Float("GridStorageRetrievalTimeMinMinutes", "2");
        b.Float("GridStorageRetrievalTimeMultiplier", "1");
        b.Float("GridStorageMinutesPerPCU", "0.001");
        b.Float("GridStorageExpediteFactor", "0.5");
        b.Float("GridStorageExpediteCostPerSecond", "1000");

        b.Cat("Match & Team");
        b.Bool("EnableMatchComponent", "false");
        b.Float("PreMatchDuration", "0");
        b.Float("MatchDuration", "0");
        b.Float("PostMatchDuration", "0");
        b.Int("MatchRestartWhenEmptyTime", "0");
        b.Bool("EnableFriendlyFire", "true");
        b.Bool("EnableTeamBalancing", "false");
        b.Bool("EnableTeamScoreCounters", "true");
        b.Bool("EnableFactionVoiceChat", "false");
        b.Float("EnemyTargetIndicatorDistance", "20");

        b.Cat("Gameplay");
        b.Bool("EnableIngameScripts", "true", experimental: true);
        b.Bool("EnableScripterRole", "false");
        b.Bool("EnableResearch", "false");
        b.Bool("EnableContainerDrops", "true");
        b.Int("MinDropContainerRespawnTime", "5");
        b.Int("MaxDropContainerRespawnTime", "20");
        b.Bool("EnableConvertToStation", "true");
        b.Bool("RespawnShipDelete", "false");
        b.Bool("EnableRemoteBlockRemoval", "true");
        b.Bool("EnableVoxelHand", "false");
        b.Bool("AdaptiveSimulationQuality", "true");
        b.Bool("EnableSelectivePhysicsUpdates", "false");
        b.Bool("UseConsolePCU", "false");
        b.Bool("OffensiveWordsFiltering", "false");
        b.Bool("EnableGoodBotHints", "true");
        b.Int("MaxHudChatMessageCount", "100");
        b.Int("MaxProductionQueueLength", "50");
        b.Short("TotalBotLimit", "32");
        b.Int("MaxDrones", "5");

        b.Cat("Experimental");
        b.NullableBool("PermanentDeath", "false");
        b.Bool("ResetOwnership", "false");
        b.Bool("StationVoxelSupport", "false");
        b.Bool("EnableSubgridDamage", "false");
        b.Bool("EnableSupergridding", "false");
        b.Bool("ExperimentalMode", "false");
        b.Bool("EnableShareInertiaTensor", "false");
        b.Bool("EnableUnsafePistonImpulses", "false");
        b.Bool("EnableUnsafeRotorTorques", "false");

        return b.Options;
    }

    /// <summary>Compact builder that humanizes labels and tracks the current category.</summary>
    private sealed class Builder
    {
        private readonly OptionScope scope;
        private string category = "General";
        public List<OptionDefinition> Options { get; } = new();

        public Builder(OptionScope scope) => this.scope = scope;

        public void Cat(string name) => category = name;

        private void Add(string xml, OptionKind kind, string def, double? min, double? max,
            EnumChoice[] choices, Liveness liveness, string help, bool experimental, string rule)
        {
            Options.Add(new OptionDefinition(
                Id: $"{(scope == OptionScope.DedicatedRoot ? "Dedicated" : "Session")}.{xml}",
                Scope: scope,
                XmlName: xml,
                Kind: kind,
                Category: category,
                Label: Humanize(xml),
                Help: help ?? string.Empty,
                Default: def,
                Min: min,
                Max: max,
                Choices: choices,
                Liveness: liveness,
                Experimental: experimental,
                ExperimentalRule: rule));
        }

        public void Bool(string xml, string def, Liveness live = Liveness.RestartRequired, string help = null, bool experimental = false)
            => Add(xml, OptionKind.Bool, def, null, null, null, live, help, experimental, null);
        public void NullableBool(string xml, string def, bool experimental = true)
            => Add(xml, OptionKind.Bool, def, null, null, null, Liveness.RestartRequired, null, experimental, null);
        public void Int(string xml, string def, double? min = null, double? max = null, Liveness live = Liveness.RestartRequired, string help = null, string experimentalRule = null)
            => Add(xml, OptionKind.Int, def, min, max, null, live, help, experimentalRule != null, experimentalRule);
        // Overload used when the 3rd positional arg is Liveness (a couple of anti-spam ints).
        public void Int(string xml, string def, Liveness live)
            => Add(xml, OptionKind.Int, def, null, null, null, live, null, false, null);
        public void Short(string xml, string def, double? min = null, double? max = null, string help = null)
            => Add(xml, OptionKind.Int, def, min ?? short.MinValue, max ?? short.MaxValue, null, Liveness.RestartRequired, help, false, null);
        public void UInt(string xml, string def, string help = null)
            => Add(xml, OptionKind.UInt, def, 0, null, null, Liveness.RestartRequired, help, false, null);
        public void Long(string xml, string def)
            => Add(xml, OptionKind.Long, def, null, null, null, Liveness.RestartRequired, null, false, null);
        public void Float(string xml, string def, Liveness live = Liveness.RestartRequired, string experimentalRule = null)
            => Add(xml, OptionKind.Float, def, null, null, null, live, null, experimentalRule != null, experimentalRule);
        public void Double(string xml, string def)
            => Add(xml, OptionKind.Double, def, null, null, null, Liveness.RestartRequired, null, false, null);
        public void Text(string xml, string def, Liveness live = Liveness.RestartRequired, string help = null)
            => Add(xml, OptionKind.Text, def, null, null, null, live, help, false, null);
        public void Multiline(string xml, string def, Liveness live = Liveness.RestartRequired, string help = null)
            => Add(xml, OptionKind.MultilineText, def, null, null, null, live, help, false, null);
        public void StringList(string xml, string itemName)
            => Add(xml, OptionKind.StringList, "", null, null, null, Liveness.RestartRequired, null, false, null);
        public void BlockLimits(string xml)
            => Add(xml, OptionKind.BlockTypeLimits, "", null, null, null, Liveness.RestartRequired, null, false, null);
        public void Enum(string xml, EnumChoice[] choices, string def, Liveness live = Liveness.RestartRequired, string help = null, bool experimental = false, string experimentalRule = null)
            => Add(xml, OptionKind.Enum, def, null, null, choices, live, help, experimental, experimentalRule);
    }

    /// <summary>Inserts spaces before internal capitals so "MaxFloatingObjects" reads "Max Floating Objects".</summary>
    private static string Humanize(string xml)
    {
        var sb = new StringBuilder(xml.Length + 8);
        for (int i = 0; i < xml.Length; i++)
        {
            char c = xml[i];
            if (i > 0 && char.IsUpper(c) && (!char.IsUpper(xml[i - 1]) || (i + 1 < xml.Length && char.IsLower(xml[i + 1]))))
                sb.Append(' ');
            sb.Append(c);
        }
        return sb.ToString();
    }
}
