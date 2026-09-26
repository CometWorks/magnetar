#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PluginSdk.Clustering
{
    /// <summary>One live process of the cluster as the registry lists it for a broadcast (T-0256).</summary>
    public sealed class PluginNodeInfo
    {
        public string Node { get; set; }
        public long Incarnation { get; set; }
        /// <summary>"Regular" or "WorldAuthority", as in <see cref="NodeContext.Role"/>.</summary>
        public string Role { get; set; }
        /// <summary>The fence a message to this node is addressed to (target kind <see cref="PluginTargetKind.Node"/>).</summary>
        public OwnerFence Fence { get; set; }
    }

    /// <summary>What a broadcast reached: one <see cref="PluginResult"/> per node it was addressed to.</summary>
    public sealed class PluginBroadcastResult
    {
        /// <summary>Success when every addressed node answered Success; otherwise the first failure's code,
        /// or the reason nothing was sent (Unavailable, Unsupported, Invalid).</summary>
        public PluginResultCode Code { get; set; }
        /// <summary>Node id -> that node's handler result (its reply payload, or why it failed).</summary>
        public IReadOnlyDictionary<string, PluginResult> Nodes { get; set; } = new Dictionary<string, PluginResult>();
        public static PluginBroadcastResult Failure(PluginResultCode code) => new PluginBroadcastResult { Code = code };
    }

    /// <summary>
    /// Optional provider capability: fan a plugin message out to every live node. A provider without it
    /// answers <see cref="PluginResultCode.Unsupported"/>; the message and record protocols are unchanged, so
    /// a broadcast is N fenced unicast messages, each to one node's handler for the topic.
    /// </summary>
    public interface IPluginClusterBroadcastProvider
    {
        Task<PluginBroadcastResult> BroadcastAsync(string plugin, string topic, byte[] payload, Guid operationId,
            bool includeWorldAuthority, TimeSpan timeout, CancellationToken cancellationToken);
    }
}
