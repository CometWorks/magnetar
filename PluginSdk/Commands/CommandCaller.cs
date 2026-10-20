using VRage.Game.ModAPI;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Identity of the player who issued a chat command. Populated by the host
    /// from the session before a handler runs.
    /// </summary>
    public readonly struct CommandCaller
    {
        /// <summary>Steam (platform) id of the caller.</summary>
        public ulong SteamId { get; }

        /// <summary>In-game identity id, used as the reply target. <c>0</c> for
        /// the server console.</summary>
        public long IdentityId { get; }

        /// <summary>Display name of the caller.</summary>
        public string Name { get; }

        /// <summary>Promote level of the caller, used for permission checks.</summary>
        public MyPromoteLevel PromoteLevel { get; }

        /// <summary><c>true</c> when the command originates from the server
        /// console rather than a player.</summary>
        public bool IsConsole { get; }

        public CommandCaller(
            ulong steamId,
            long identityId,
            string name,
            MyPromoteLevel promoteLevel,
            bool isConsole = false
        )
        {
            SteamId = steamId;
            IdentityId = identityId;
            Name = name;
            PromoteLevel = promoteLevel;
            IsConsole = isConsole;
        }
    }
}
