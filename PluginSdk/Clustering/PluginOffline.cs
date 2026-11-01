#nullable disable
using System;

namespace PluginSdk.Clustering
{
    /// <summary>Optional provider capability: let plugins keep partitions online.</summary>
    public interface IPluginClusterOfflineProvider
    {
        IDisposable RegisterOfflineBlocker(string plugin, Func<long, string> blocker);
    }
}
