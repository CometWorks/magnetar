#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace PluginSdk.Clustering
{
    /// <summary>A player online somewhere in the server (T-0256).</summary>
    public sealed class PluginPlayerInfo
    {
        /// <summary>The node the player is attached to ("standalone" on a plain server).</summary>
        public string Node { get; set; }
        public ulong SteamId { get; set; }
        public int SerialId { get; set; }
        public long IdentityId { get; set; }
        public string Name { get; set; }
        /// <summary>The attached node's view when it last reported; promotion is replicated separately.</summary>
        public bool IsAdmin { get; set; }
    }

    /// <summary>Where a player is: the entity it controls, else its character, else the player position.</summary>
    public sealed class PluginPlayerPosition
    {
        public long IdentityId { get; set; }
        public ulong SteamId { get; set; }
        /// <summary>The controlled entity or character; 0 when the player has neither.</summary>
        public long EntityId { get; set; }
        public Vector3D Position { get; set; }
    }

    /// <summary>What this process may do with an entity.</summary>
    public enum PluginEntityResidence
    {
        /// <summary>No such entity in this process.</summary>
        Absent,
        /// <summary>Simulated and owned here: this process is the one that may change and persist it.</summary>
        Local,
        /// <summary>Owned here but frozen mid-handover: leaving or arriving; do not change it now.</summary>
        Frozen,
        /// <summary>Exists here but in no partition this node owns (not yet claimed, or a copy): do not persist changes.</summary>
        Unowned,
    }

    /// <summary>Where an entity lives, as seen from this process.</summary>
    public sealed class PluginEntityPlacement
    {
        /// <summary>The top-level entity (the grid group's root, or the character) the lookup resolved to.</summary>
        public long EntityId { get; set; }
        public PluginEntityResidence Residence { get; set; }
        /// <summary>The partition holding it on this node; 0 when none (always 0 on a plain server).
        /// <see cref="PluginClusterClient.ResolveOwnerAsync"/> with this partition gives its owner fence.</summary>
        public ulong Partition { get; set; }
        /// <summary>This process's node id when the entity is here, else null.</summary>
        public string Node { get; set; }
    }

    /// <summary>
    /// Optional provider capability: cluster-wide read-only views. Called on the game thread. A provider
    /// without it leaves the views unavailable (null) in a cluster process.
    /// </summary>
    public interface IPluginClusterViewProvider
    {
        IReadOnlyList<PluginPlayerInfo> OnlinePlayers();
        IReadOnlyList<PluginPlayerPosition> PlayerPositions();
        PluginEntityPlacement LocateEntity(long entityId);
    }

    /// <summary>The plain-server views: this process is the whole server, read through the ModAPI.</summary>
    internal static class PluginLocalViews
    {
        public const string Node = "standalone";

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IReadOnlyList<PluginPlayerInfo> OnlinePlayers()
        {
            var result = new List<PluginPlayerInfo>();
            foreach (IMyPlayer player in Players())
                result.Add(new PluginPlayerInfo { Node = Node, SteamId = player.SteamUserId, IdentityId = player.IdentityId,
                    Name = player.DisplayName ?? string.Empty, IsAdmin = player.PromoteLevel >= VRage.Game.ModAPI.MyPromoteLevel.Admin });
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IReadOnlyList<PluginPlayerPosition> PlayerPositions()
        {
            var result = new List<PluginPlayerPosition>();
            foreach (IMyPlayer player in Players())
            {
                IMyEntity entity = player.Controller?.ControlledEntity?.Entity ?? player.Character;
                result.Add(new PluginPlayerPosition { IdentityId = player.IdentityId, SteamId = player.SteamUserId,
                    EntityId = entity?.EntityId ?? 0, Position = entity?.WorldMatrix.Translation ?? player.GetPosition() });
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static PluginEntityPlacement LocateEntity(long entityId)
        {
            IMyEntity entity = MyAPIGateway.Entities?.GetEntityById(entityId);
            IMyEntity top = entity?.GetTopMostParent() ?? entity;
            return top == null || top.MarkedForClose
                ? new PluginEntityPlacement { EntityId = entityId, Residence = PluginEntityResidence.Absent }
                : new PluginEntityPlacement { EntityId = top.EntityId, Residence = PluginEntityResidence.Local, Node = Node };
        }

        private static List<IMyPlayer> Players()
        {
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players?.GetPlayers(players, player => player != null && !player.IsBot);
            return players;
        }
    }
}
