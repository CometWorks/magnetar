using System;
using System.Collections.Generic;
using PluginSdk;
using ProtoBuf;
using Pulsar.Legacy.Loader;
using Pulsar.Shared;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using VRage.Game;
using LauncherGame = Pulsar.Legacy.Launcher.Game;

namespace Pulsar.Legacy.Integration;

/// <summary>
/// Host-side sender for <see cref="MissionScreens"/>. The paired client
/// receiver lives in the repository's MagnetarMod folder and must be enabled as
/// a world mod for clients to see the popup.
/// </summary>
internal static class MissionScreenSender
{
    public static bool ShowToPlayer(long identityId, MissionScreenContent content)
    {
        if (identityId == 0L || MySession.Static == null)
            return false;

        ulong steamId = MySession.Static.Players.TryGetSteamId(identityId);
        return ShowToSteam(steamId, content);
    }

    public static bool ShowToSteam(ulong steamId, MissionScreenContent content)
    {
        if (steamId == 0UL || !content.HasContent || MyAPIGateway.Multiplayer == null ||
            MyAPIGateway.Utilities == null || MySession.Static == null || !ReceiverModLoaded())
            return false;

        byte[] payload = Serialize(content);
        LauncherGame.RunOnGameThread(() => SendToSteamOnGameThread(steamId, payload));
        return true;
    }

    public static bool ShowToAll(MissionScreenContent content)
    {
        if (!content.HasContent || MySession.Static == null || MyAPIGateway.Multiplayer == null ||
            MyAPIGateway.Utilities == null || !ReceiverModLoaded())
            return false;

        byte[] payload = Serialize(content);
        LauncherGame.RunOnGameThread(() =>
        {
            try
            {
                ICollection<MyPlayer> players = MySession.Static.Players.GetOnlinePlayers();
                foreach (MyPlayer player in players)
                {
                    if (player != null && player.Id.SteamId != 0UL)
                        SendToSteamOnGameThread(player.Id.SteamId, payload);
                }
            }
            catch (Exception e)
            {
                LogFile.Error($"Mission screen broadcast failed: {e}");
            }
        });
        return true;
    }

    private static void SendToSteamOnGameThread(ulong steamId, byte[] payload)
    {
        try
        {
            if (MyAPIGateway.Multiplayer == null)
                return;

            MyAPIGateway.Multiplayer.SendMessageTo(MissionScreens.ChannelId, payload, steamId, true);
        }
        catch (Exception e)
        {
            LogFile.Error($"Mission screen send to {steamId} failed: {e}");
        }
    }

    private static bool ReceiverModLoaded()
    {
        if (MySession.Static?.Mods == null)
            return false;

        foreach (MyObjectBuilder_Checkpoint.ModItem mod in MySession.Static.Mods)
        {
            if (mod.PublishedFileId == MagnetarClientMod.WorkshopId ||
                ContainsModName(mod.Name) ||
                ContainsModName(mod.FriendlyName))
                return true;
        }

        return false;
    }

    private static bool ContainsModName(string value)
        => !string.IsNullOrEmpty(value) &&
           value.IndexOf("MagnetarMod", StringComparison.OrdinalIgnoreCase) >= 0;

    private static byte[] Serialize(MissionScreenContent content)
    {
        var packet = new MissionScreenPacket
        {
            ProtocolVersion = MissionScreens.ProtocolVersion,
            PacketType = MissionScreens.ShowMissionScreenPacket,
            ScreenTitle = content.ScreenTitle ?? string.Empty,
            CurrentObjectivePrefix = content.CurrentObjectivePrefix ?? string.Empty,
            CurrentObjective = content.CurrentObjective ?? string.Empty,
            ScreenDescription = content.ScreenDescription ?? string.Empty,
            OkButtonCaption = content.OkButtonCaption ?? string.Empty
        };

        return MyAPIGateway.Utilities.SerializeToBinary(packet);
    }

    [ProtoContract]
    private sealed class MissionScreenPacket
    {
        [ProtoMember(1)]
        public byte ProtocolVersion;

        [ProtoMember(2)]
        public byte PacketType;

        [ProtoMember(3)]
        public string ScreenTitle;

        [ProtoMember(4)]
        public string CurrentObjectivePrefix;

        [ProtoMember(5)]
        public string CurrentObjective;

        [ProtoMember(6)]
        public string ScreenDescription;

        [ProtoMember(7)]
        public string OkButtonCaption;
    }
}
