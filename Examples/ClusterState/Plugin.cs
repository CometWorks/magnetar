using System;
using System.Text;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using VRage.Plugins;

namespace ClusterState;

// Include this same Local plugin on every node. It has no Quasar/Agent dependency.
public sealed class Plugin : IPlugin
{
    private PluginClusterClient client;
    private IDisposable handler;
    public void Init(object gameInstance) => client = PluginCluster.ForPlugin("example-cluster-state");
    public void Update()
    {
        if (handler == null && client.Context.Available)
            handler = client.RegisterHandler("remember", RememberAsync);
    }
    private async Task<byte[]> RememberAsync(PluginMessage message)
    {
        // Handler begins on the game thread. After await, do not touch game objects.
        var current = await client.ReadAsync("last-message");
        if (current.Code is not (PluginResultCode.Success or PluginResultCode.NotFound))
            return Encoding.UTF8.GetBytes(current.Code.ToString());
        var result = await client.CompareExchangeAsync("last-message", current.Record.Version,
            1, message.Payload, message.OperationId, message.Destination);
        // Conflict is visible to the caller; never silently overwrite another writer.
        return Encoding.UTF8.GetBytes(result.Code.ToString());
    }
    public Task<PluginResult> RememberOnWorldAuthorityAsync(byte[] value) => client.RequestAsync(
        new PluginTarget { Kind = PluginTargetKind.WorldAuthority }, "remember", value,
        Guid.NewGuid(), TimeSpan.FromSeconds(5));
    public void Dispose() => handler?.Dispose();
}
