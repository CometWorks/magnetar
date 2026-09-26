using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using Xunit;

namespace PluginSdk.Tests
{
    [Collection("ServerControl")]
    public sealed class PluginGlobalCommandTests
    {
        private const string Id = "Cluster State.dll";

        private static PluginClusterClient Client()
        {
            PluginCluster.BindOwner(Id, typeof(PluginGlobalCommandTests).Assembly);
            return PluginCluster.ForPlugin(Id);
        }

        private static async Task<PluginGlobalCommandResult> Run(StandalonePluginProvider provider, Task<PluginGlobalCommandResult> call)
        {
            for (int i = 0; i < 200 && !call.IsCompleted; i++) { provider.Update(); await Task.Delay(5); }
            return await call;
        }

        [Fact]
        public async Task Plain_server_runs_a_global_command_once_per_operation_id()
        {
            string root = Path.Combine(Path.GetTempPath(), "plugin-global-" + Guid.NewGuid());
            var client = Client();
            var provider = new StandalonePluginProvider(root);
            Assert.True(PluginCluster.Register(provider));
            try
            {
                int runs = 0;
                using var command = client.RegisterGlobalCommand("grant", payload =>
                {
                    runs++;
                    if (payload[0] == 0) throw new InvalidOperationException("refused");
                    if (payload[0] == 1) return new byte[5000];
                    return new[] { (byte)(payload[0] * 2) };
                });
                Assert.NotNull(command);
                Guid a = Guid.NewGuid(), b = Guid.NewGuid();
                TimeSpan t = TimeSpan.FromSeconds(2);
                var first = await Run(provider, client.ExecuteGlobalAsync("grant", new byte[] { 21 }, a, t));
                Assert.Equal(PluginResultCode.Success, first.Code);
                Assert.Equal(new byte[] { 42 }, first.Payload);
                var retry = await Run(provider, client.ExecuteGlobalAsync("grant", new byte[] { 21 }, a, t));
                Assert.Equal(new byte[] { 42 }, retry.Payload);
                Assert.Equal(1, runs);                                            // the retry replayed, did not run
                var reused = await Run(provider, client.ExecuteGlobalAsync("grant", new byte[] { 22 }, a, t));
                Assert.Equal(PluginResultCode.Conflict, reused.Code);
                var failed = await Run(provider, client.ExecuteGlobalAsync("grant", new byte[] { 0 }, b, t));
                Assert.Equal(PluginResultCode.Invalid, failed.Code);
                Assert.Equal("refused", failed.Error);
                var failedAgain = await Run(provider, client.ExecuteGlobalAsync("grant", new byte[] { 0 }, b, t));
                Assert.Equal("refused", failedAgain.Error);
                Assert.Equal(2, runs);                                            // a stored failure replays too
                var large = await Run(provider, client.ExecuteGlobalAsync("grant", new byte[] { 1 }, Guid.NewGuid(), t));
                Assert.Equal(PluginResultCode.Invalid, large.Code);
                Assert.Contains("4096", large.Error);
                var missing = await Run(provider, client.ExecuteGlobalAsync("nobody", null, Guid.NewGuid(), t));
                Assert.Equal(PluginResultCode.Unsupported, missing.Code);
                Assert.Equal(PluginResultCode.Invalid, (await client.ExecuteGlobalAsync("bad\\name", null, Guid.NewGuid(), t)).Code);
            }
            finally { PluginCluster.Unregister(provider); provider.Dispose(); Directory.Delete(root, true); }
        }

        [Fact]
        public void Cluster_without_a_ledger_offers_no_global_command()
        {
            var client = Client();
            string previous = Environment.GetEnvironmentVariable("CLUSTER_NODE_ID");
            Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", "node-1");
            var provider = new NoLedgerProvider();
            try
            {
                Assert.True(PluginCluster.Register(provider));
                Assert.Null(client.RegisterGlobalCommand("grant", payload => payload));
                Assert.Throws<ArgumentException>(() => client.RegisterGlobalCommand("", payload => payload));
            }
            finally { PluginCluster.Unregister(provider); Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", previous); }
        }

        private sealed class NoLedgerProvider : IPluginClusterProvider
        {
            public NodeContext Context => new NodeContext { Node = "node-1", Available = true };
            public event Action ContextChanged { add { } remove { } }
            public Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public Task<PluginResult> RequestAsync(string plugin, PluginTarget target, string topic, byte[] payload,
                Guid operationId, TimeSpan timeout, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public IDisposable RegisterHandler(string plugin, string topic, Func<PluginMessage, Task<byte[]>> handler) => new MemoryStream();
        }
    }
}
