using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using Xunit;

namespace PluginSdk.Tests
{
    [Collection("ServerControl")]
    public sealed class PluginHandoverTests
    {
        private const string Id = "Cluster State.dll";

        private static PluginClusterClient Client()
        {
            PluginCluster.BindOwner(Id, typeof(PluginHandoverTests).Assembly);
            return PluginCluster.ForPlugin(Id);
        }

        private sealed class Hooks : IPluginGridHandover
        {
            public byte[] Leaving(long gridId) => null;
            public void Left(long gridId) { }
            public void Stayed(long gridId) { }
            public void Arrived(long gridId, byte[] state) { }
        }

        [Fact]
        public void Plain_server_registration_succeeds_and_never_fires()
        {
            var registration = Client().RegisterGridHandover(new Hooks());
            Assert.NotNull(registration);
            registration.Dispose();
            Assert.Throws<ArgumentNullException>(() => Client().RegisterGridHandover(null));
        }

        [Fact]
        public void Cluster_process_needs_a_provider_with_hooks()
        {
            var client = Client();
            string previous = Environment.GetEnvironmentVariable("CLUSTER_NODE_ID");
            Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", "node-1");
            try
            {
                Assert.Null(client.RegisterGridHandover(new Hooks()));   // unknown, not a silent no-op
                var provider = new HookProvider();
                Assert.True(PluginCluster.Register(provider));
                try
                {
                    var hooks = new Hooks();
                    using var registration = client.RegisterGridHandover(hooks);
                    Assert.NotNull(registration);
                    Assert.Equal((Id, (IPluginGridHandover)hooks), provider.Registered);
                }
                finally { PluginCluster.Unregister(provider); }
            }
            finally { Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", previous); }
        }

        private sealed class HookProvider : IPluginClusterProvider, IPluginClusterHandoverProvider
        {
            public (string, IPluginGridHandover) Registered;
            public NodeContext Context => new NodeContext { Node = "node-1", Available = true };
            public event Action ContextChanged { add { } remove { } }
            public Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public Task<PluginResult> RequestAsync(string plugin, PluginTarget target, string topic, byte[] payload,
                Guid operationId, TimeSpan timeout, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public IDisposable RegisterHandler(string plugin, string topic, Func<PluginMessage, Task<byte[]>> handler) => null;
            public IDisposable RegisterGridHandover(string plugin, IPluginGridHandover handler)
            {
                Registered = (plugin, handler);
                return new System.IO.MemoryStream();
            }
        }
    }
}
