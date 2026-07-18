using System;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace MagnetarMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class MagnetarModSession : MySessionComponentBase
    {
        private const ushort ChannelId = 48731;
        private const byte ProtocolVersion = 1;
        private const byte ShowMissionScreenPacket = 1;

        private bool registered;

        public override void LoadData()
        {
            if (MyAPIGateway.Multiplayer == null)
                return;

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ChannelId, OnMessage);
            registered = true;
        }

        protected override void UnloadData()
        {
            if (!registered || MyAPIGateway.Multiplayer == null)
                return;

            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(ChannelId, OnMessage);
            registered = false;
        }

        private void OnMessage(ushort handlerId, byte[] data, ulong sender, bool sentFromServer)
        {
            if (!sentFromServer || data == null || data.Length == 0)
                return;

            MissionScreenPacket packet;
            if (!TryDeserialize(data, out packet))
                return;

            if (MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.InvokeOnGameThread(delegate
            {
                if (MyAPIGateway.Utilities == null)
                    return;

                MyAPIGateway.Utilities.ShowMissionScreen(
                    EmptyToNull(packet.ScreenTitle),
                    EmptyToNull(packet.CurrentObjectivePrefix),
                    EmptyToNull(packet.CurrentObjective),
                    EmptyToNull(packet.ScreenDescription),
                    null,
                    EmptyToNull(packet.OkButtonCaption));
            }, "MagnetarMod.ShowMissionScreen");
        }

        private static bool TryDeserialize(byte[] data, out MissionScreenPacket packet)
        {
            packet = null;

            try
            {
                if (MyAPIGateway.Utilities == null)
                    return false;

                packet = MyAPIGateway.Utilities.SerializeFromBinary<MissionScreenPacket>(data);
                return packet != null &&
                       packet.ProtocolVersion == ProtocolVersion &&
                       packet.PacketType == ShowMissionScreenPacket;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("[MagnetarMod] Failed to decode mission screen packet: " + e);
                return false;
            }
        }

        private static string EmptyToNull(string value)
        {
            return string.IsNullOrEmpty(value) ? null : value;
        }

        [ProtoContract]
        public sealed class MissionScreenPacket
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
}
