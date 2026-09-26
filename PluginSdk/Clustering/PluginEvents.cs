#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PluginSdk.Clustering
{
    public enum PluginClusterEventKind
    {
        /// <summary>A node (or the World Authority) became ready: <see cref="PluginClusterEvent.Node"/>, Incarnation, Role.</summary>
        NodeUp,
        /// <summary>A node incarnation left the ready set (stopped, restarted, draining or lost its lease).</summary>
        NodeDown,
        /// <summary>The World Authority generation changed: a new WA took over. <see cref="PluginClusterEvent.Generation"/>.</summary>
        WorldAuthorityChanged,
        /// <summary>This node now owns <see cref="PluginClusterEvent.Partition"/> at <see cref="PluginClusterEvent.Generation"/>.</summary>
        PartitionAcquired,
        /// <summary>This node no longer owns <see cref="PluginClusterEvent.Partition"/>.</summary>
        PartitionLost,
    }

    /// <summary>A change in the cluster, raised on the game thread (T-0256).</summary>
    public sealed class PluginClusterEvent
    {
        public PluginClusterEventKind Kind { get; set; }
        public string Node { get; set; }
        public long Incarnation { get; set; }
        /// <summary>"Regular" or "WorldAuthority" for node events.</summary>
        public string Role { get; set; }
        public ulong Partition { get; set; }
        public long Generation { get; set; }
        public override string ToString() => Kind + " node=" + Node + " incarnation=" + Incarnation + " role=" + Role
            + " partition=" + Partition + " generation=" + Generation;
    }

    /// <summary>Optional provider capability: cluster events and the live node roster.</summary>
    public interface IPluginClusterEventProvider
    {
        /// <summary>Raised on the game thread.</summary>
        event Action<PluginClusterEvent> ClusterEvent;
        /// <summary>The ready nodes and the WA now, from the registry.</summary>
        Task<IReadOnlyList<PluginNodeInfo>> NodesAsync(CancellationToken cancellationToken);
    }
}
