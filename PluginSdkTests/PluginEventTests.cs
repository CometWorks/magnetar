using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using Xunit;

namespace PluginSdk.Tests
{
    [Collection("ServerControl")]
    public sealed class PluginEventTests
    {
        private const string Id = "Cluster State.dll";

        private static PluginClusterClient Client()
        {
            PluginCluster.BindOwner(Id, typeof(PluginEventTests).Assembly);
            return PluginCluster.ForPlugin(Id);
        }

        [Fact]
        public async Task Events_relay_from_the_registered_provider_only_while_it_is_registered()
        {
            var client = Client();
            var seen = new List<string>();
            Action<PluginClusterEvent> broken = _ => throw new InvalidOperationException("observer fault");
            Action<PluginClusterEvent> observer = change => seen.Add(change.Kind + ":" + change.Node);
            client.ClusterEvent += broken;
            client.ClusterEvent += observer;
            var provider = new EventProvider();
            try
            {
                Assert.True(PluginCluster.Register(provider));
                provider.Raise(new PluginClusterEvent { Kind = PluginClusterEventKind.NodeUp, Node = "node-2" });
                Assert.Equal(new[] { "NodeUp:node-2" }, seen);   // the throwing observer did not starve this one
                var nodes = await client.NodesAsync();
                Assert.Equal("node-9", Assert.Single(nodes).Node);
                Assert.True(PluginCluster.Unregister(provider));
                provider.Raise(new PluginClusterEvent { Kind = PluginClusterEventKind.NodeDown, Node = "node-2" });
                Assert.Single(seen);
            }
            finally
            {
                PluginCluster.Unregister(provider);
                client.ClusterEvent -= broken; client.ClusterEvent -= observer;
            }
        }

        [Fact]
        public async Task Plain_server_has_one_standalone_node_and_a_cluster_without_events_has_none_known()
        {
            var client = Client();
            Assert.Equal("standalone", Assert.Single(await client.NodesAsync()).Node);
            string previous = Environment.GetEnvironmentVariable("CLUSTER_NODE_ID");
            Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", "node-1");
            try { Assert.Null(await client.NodesAsync()); }
            finally { Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", previous); }
        }

        private sealed class EventProvider : IPluginClusterProvider, IPluginClusterEventProvider
        {
            public event Action<PluginClusterEvent> ClusterEvent;
            public void Raise(PluginClusterEvent change) => ClusterEvent?.Invoke(change);
            public Task<IReadOnlyList<PluginNodeInfo>> NodesAsync(CancellationToken cancellationToken) =>
                Task.FromResult<IReadOnlyList<PluginNodeInfo>>(new[] { new PluginNodeInfo { Node = "node-9" } });
            public NodeContext Context => new NodeContext { Node = "node-1", Available = true };
            public event Action ContextChanged { add { } remove { } }
            public Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public Task<PluginResult> RequestAsync(string plugin, PluginTarget target, string topic, byte[] payload,
                Guid operationId, TimeSpan timeout, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public IDisposable RegisterHandler(string plugin, string topic, Func<PluginMessage, Task<byte[]>> handler) => null;
        }
    }
}
