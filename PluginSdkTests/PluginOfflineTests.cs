using System;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using Xunit;

namespace PluginSdk.Tests
{
    [Collection("ServerControl")]
    public sealed class PluginOfflineTests
    {
        private const string Id = "Cluster State.dll";

        private static PluginClusterClient Client()
        {
            PluginCluster.BindOwner(Id, typeof(PluginOfflineTests).Assembly);
            return PluginCluster.ForPlugin(Id);
        }

        [Fact]
        public void Plain_server_registration_succeeds_and_never_fires()
        {
            var registration = Client().RegisterOfflineBlocker(entity => "never");
            Assert.NotNull(registration);
            registration.Dispose();
            Assert.Throws<ArgumentNullException>(() => Client().RegisterOfflineBlocker(null));
        }

        [Fact]
        public void Cluster_process_needs_a_provider_with_blockers()
        {
            var client = Client();
            string previous = Environment.GetEnvironmentVariable("CLUSTER_NODE_ID");
            Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", "node-1");
            try
            {
                Assert.Null(client.RegisterOfflineBlocker(entity => null)); // unknown, not a silent no-op
                var provider = new BlockerProvider();
                Assert.True(PluginCluster.Register(provider));
                try
                {
                    Func<long, string> blocker = entity => null;
                    using var registration = client.RegisterOfflineBlocker(blocker);
                    Assert.NotNull(registration);
                    Assert.Equal((Id, blocker), provider.Registered);
                }
                finally
                {
                    PluginCluster.Unregister(provider);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", previous);
            }
        }

        private sealed class BlockerProvider : IPluginClusterProvider, IPluginClusterOfflineProvider
        {
            public (string, Func<long, string>) Registered;
            public NodeContext Context => new NodeContext { Node = "node-1", Available = true };
            public event Action ContextChanged
            {
                add { }
                remove { }
            }

            public Task<PluginResult> ExecuteAsync(
                PluginRequest request,
                CancellationToken cancellationToken
            ) => Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));

            public Task<PluginResult> RequestAsync(
                string plugin,
                PluginTarget target,
                string topic,
                byte[] payload,
                Guid operationId,
                TimeSpan timeout,
                CancellationToken cancellationToken
            ) => Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));

            public IDisposable RegisterHandler(
                string plugin,
                string topic,
                Func<PluginMessage, Task<byte[]>> handler
            ) => null;

            public IDisposable RegisterOfflineBlocker(string plugin, Func<long, string> blocker)
            {
                Registered = (plugin, blocker);
                return new System.IO.MemoryStream();
            }
        }
    }
}
