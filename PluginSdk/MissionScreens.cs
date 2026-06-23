using System;
using System.Runtime.CompilerServices;

// Only the host launcher may Bind the facade to its implementation.
[assembly: InternalsVisibleTo("MagnetarInterim")]
[assembly: InternalsVisibleTo("MagnetarLegacy")]
[assembly: InternalsVisibleTo("PluginSdkTests")]

namespace PluginSdk
{
    /// <summary>
    /// Plugin-facing facade for opening Space Engineers mission-screen popups on
    /// clients. The dedicated-server host sends the payload to the bundled
    /// MagnetarMod client receiver; without that mod loaded client-side, the
    /// game silently drops the packet.
    /// </summary>
    public static class MissionScreens
    {
        public const ushort ChannelId = 48731;
        public const byte ProtocolVersion = 1;
        public const byte ShowMissionScreenPacket = 1;

        private static Func<long, MissionScreenContent, bool> showToPlayer = (identityId, content) => false;
        private static Func<ulong, MissionScreenContent, bool> showToSteam = (steamId, content) => false;
        private static Func<MissionScreenContent, bool> showToAll = content => false;

        /// <summary>
        /// True when the Magnetar host has installed a server-side sender.
        /// This does not prove that the client has the MagnetarMod receiver
        /// enabled in the world.
        /// </summary>
        public static bool IsHostSenderAvailable { get; private set; }

        public static bool ShowToPlayer(
            long playerIdentityId,
            string screenTitle,
            string currentObjectivePrefix,
            string currentObjective,
            string screenDescription,
            string okButtonCaption = null)
            => ShowToPlayer(playerIdentityId, new MissionScreenContent(
                screenTitle,
                currentObjectivePrefix,
                currentObjective,
                screenDescription,
                okButtonCaption));

        public static bool ShowToPlayer(long playerIdentityId, MissionScreenContent content)
            => playerIdentityId != 0L && content.HasContent && showToPlayer(playerIdentityId, content);

        public static bool ShowToSteam(
            ulong steamId,
            string screenTitle,
            string currentObjectivePrefix,
            string currentObjective,
            string screenDescription,
            string okButtonCaption = null)
            => ShowToSteam(steamId, new MissionScreenContent(
                screenTitle,
                currentObjectivePrefix,
                currentObjective,
                screenDescription,
                okButtonCaption));

        public static bool ShowToSteam(ulong steamId, MissionScreenContent content)
            => steamId != 0UL && content.HasContent && showToSteam(steamId, content);

        public static bool ShowToAll(
            string screenTitle,
            string currentObjectivePrefix,
            string currentObjective,
            string screenDescription,
            string okButtonCaption = null)
            => ShowToAll(new MissionScreenContent(
                screenTitle,
                currentObjectivePrefix,
                currentObjective,
                screenDescription,
                okButtonCaption));

        public static bool ShowToAll(MissionScreenContent content)
            => content.HasContent && showToAll(content);

        internal static void Bind(
            Func<long, MissionScreenContent, bool> showToPlayer,
            Func<ulong, MissionScreenContent, bool> showToSteam,
            Func<MissionScreenContent, bool> showToAll)
        {
            MissionScreens.showToPlayer = showToPlayer ?? ((identityId, content) => false);
            MissionScreens.showToSteam = showToSteam ?? ((steamId, content) => false);
            MissionScreens.showToAll = showToAll ?? (content => false);
            IsHostSenderAvailable = showToPlayer != null || showToSteam != null || showToAll != null;
        }
    }
}
