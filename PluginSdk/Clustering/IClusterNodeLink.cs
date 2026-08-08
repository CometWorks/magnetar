using System;

namespace PluginSdk.Clustering
{
    /// <summary>
    /// Gateway data-link service supplied by the active server transport.
    /// Callbacks run on the transport thread and must return promptly.
    /// </summary>
    public interface IClusterNodeLink
    {
        bool IsConnected { get; }

        event Action<byte[]> MessageReceived;
        event Action<bool> ConnectionChanged;
        event Action<ulong> ClientDetached;
        event Action<ClusterLifecycleAcknowledgement> LifecycleAcknowledged;

        Func<ulong, byte[], bool> AttachValidator { get; set; }

        bool Send(byte[] payload);
        bool SendLifecycleRequest(ClusterLifecycleRequest request);
    }
}
