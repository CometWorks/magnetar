using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using Xunit;

namespace PluginSdk.Tests
{
    [Collection("ServerControl")]
    public sealed class PluginBroadcastTests
    {
        private const string Id = "Cluster State.dll";

        [Fact]
        public async Task Standalone_broadcast_is_delivered_once_locally_on_update()
        {
            string root = Path.Combine(Path.GetTempPath(), "plugin-broadcast-" + Guid.NewGuid());
            try
            {
                using var provider = new StandalonePluginProvider(root);
                int thread = Environment.CurrentManagedThreadId, called = 0;
                Guid operation = Guid.NewGuid(), seen = Guid.Empty;
                using var registration = provider.RegisterHandler("test", "news", message => {
                    called++; seen = message.OperationId;
                    Assert.Equal(thread, Environment.CurrentManagedThreadId);
                    return Task.FromResult(new byte[] { (byte)(message.Payload[0] + 1) });
                });
                var broadcast = provider.BroadcastAsync("test", "news", new byte[] { 4 }, operation, true, TimeSpan.FromSeconds(2), default);
                Assert.False(broadcast.IsCompleted);
                provider.Update();
                var result = await broadcast;
                Assert.Equal(PluginResultCode.Success, result.Code);
                Assert.Equal(new byte[] { 5 }, Assert.Single(result.Nodes).Value.Payload);
                Assert.Equal("standalone", Assert.Single(result.Nodes).Key);
                provider.Update();
                Assert.Equal(1, called);
                Assert.Equal(operation, seen);
            }
            finally { Directory.Delete(root, true); }
        }

        [Fact]
        public async Task Standalone_broadcast_without_a_subscriber_reports_it_per_node()
        {
            string root = Path.Combine(Path.GetTempPath(), "plugin-broadcast-" + Guid.NewGuid());
            try
            {
                using var provider = new StandalonePluginProvider(root);
                var broadcast = provider.BroadcastAsync("test", "nobody", null, Guid.NewGuid(), true, TimeSpan.FromSeconds(2), default);
                provider.Update();
                var result = await broadcast;
                Assert.NotEqual(PluginResultCode.Success, result.Code);
                Assert.Equal(result.Code, Assert.Single(result.Nodes).Value.Code);
            }
            finally { Directory.Delete(root, true); }
        }

        [Theory]
        [InlineData("bad\\topic", 1)]
        [InlineData("news", 70000)]
        public async Task Standalone_broadcast_refuses_invalid_input(string topic, int size)
        {
            string root = Path.Combine(Path.GetTempPath(), "plugin-broadcast-" + Guid.NewGuid());
            try
            {
                using var provider = new StandalonePluginProvider(root);
                var result = await provider.BroadcastAsync("test", topic, new byte[size], Guid.NewGuid(), true, TimeSpan.FromSeconds(1), default);
                Assert.Equal(PluginResultCode.Invalid, result.Code);
                Assert.Empty(result.Nodes);
            }
            finally { Directory.Delete(root, true); }
        }

        [Fact]
        public async Task A_provider_without_broadcast_answers_unsupported_and_none_answers_unavailable()
        {
            PluginCluster.BindOwner(Id, typeof(PluginBroadcastTests).Assembly);
            var client = PluginCluster.ForPlugin(Id);
            Assert.Null(PluginCluster.Current);
            Assert.Equal(PluginResultCode.Unavailable,
                (await client.BroadcastAsync("news", null, Guid.NewGuid(), TimeSpan.FromSeconds(1))).Code);
            var legacy = new UnicastOnlyProvider();
            Assert.True(PluginCluster.Register(legacy));
            try
            {
                Assert.Equal(PluginResultCode.Unsupported,
                    (await client.BroadcastAsync("news", null, Guid.NewGuid(), TimeSpan.FromSeconds(1))).Code);
            }
            finally { PluginCluster.Unregister(legacy); }
        }

        [Fact]
        public async Task A_throwing_broadcast_provider_is_contained()
        {
            PluginCluster.BindOwner(Id, typeof(PluginBroadcastTests).Assembly);
            var client = PluginCluster.ForPlugin(Id);
            var broken = new ThrowingBroadcastProvider();
            Assert.True(PluginCluster.Register(broken));
            try
            {
                Assert.Equal(PluginResultCode.Unavailable,
                    (await client.BroadcastAsync("news", null, Guid.NewGuid(), TimeSpan.FromSeconds(1))).Code);
                Assert.Equal(Id, broken.Plugin);
            }
            finally { PluginCluster.Unregister(broken); }
        }

        private class UnicastOnlyProvider : IPluginClusterProvider
        {
            public NodeContext Context => new NodeContext { Node = "legacy", Available = true };
            public event Action ContextChanged { add { } remove { } }
            public Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public Task<PluginResult> RequestAsync(string plugin, PluginTarget target, string topic, byte[] payload,
                Guid operationId, TimeSpan timeout, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public IDisposable RegisterHandler(string plugin, string topic, Func<PluginMessage, Task<byte[]>> handler) => null;
        }

        private sealed class ThrowingBroadcastProvider : UnicastOnlyProvider, IPluginClusterBroadcastProvider
        {
            public string Plugin;
            public Task<PluginBroadcastResult> BroadcastAsync(string plugin, string topic, byte[] payload, Guid operationId,
                bool includeWorldAuthority, TimeSpan timeout, CancellationToken cancellationToken)
            {
                Plugin = plugin;
                throw new InvalidOperationException("provider fault");
            }
        }
    }
}
