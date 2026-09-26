using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using Xunit;

namespace PluginSdk.Tests
{
    [Collection("ServerControl")]
    public sealed class PluginClockTests
    {
        private const string Id = "Cluster State.dll";

        private static PluginClusterClient Client()
        {
            PluginCluster.BindOwner(Id, typeof(PluginClockTests).Assembly);
            return PluginCluster.ForPlugin(Id);
        }

        [Fact]
        public void Plain_server_time_is_the_local_game_time_and_a_monotonic_clock()
        {
            var client = Client();
            var old = MyAPIGateway.Session;
            try
            {
                var game = TimeSpan.FromHours(30.5);
                MyAPIGateway.Session = PluginViewTests.Fake.Create<IMySession>(new Dictionary<string, Func<object[], object>> {
                    ["GameDateTime"] = _ => new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc) + game });
                var first = client.Time();
                Thread.Sleep(20);
                var second = client.Time();
                Assert.Equal(game, first.GameTime);
                Assert.Equal(new DateTime(2081, 1, 2, 6, 30, 0, DateTimeKind.Utc), first.GameDateTime);
                Assert.True(first.Synchronized && first.Authoritative);
                Assert.True(second.ClockMilliseconds - first.ClockMilliseconds >= 15);
                MyAPIGateway.Session = null;
                Assert.Equal(TimeSpan.Zero, client.Time().GameTime);   // before a session: zero, not null
            }
            finally { MyAPIGateway.Session = old; }
        }

        [Fact]
        public void Cluster_process_reads_the_provider_clock_never_the_local_one()
        {
            var client = Client();
            string previous = Environment.GetEnvironmentVariable("CLUSTER_NODE_ID");
            Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", "node-1");
            try
            {
                Assert.Null(client.Time());
                var provider = new ClockProvider();
                Assert.True(PluginCluster.Register(provider));
                try
                {
                    var time = client.Time();
                    Assert.Equal(1234, time.ClockMilliseconds);
                    Assert.False(time.Synchronized);
                }
                finally { PluginCluster.Unregister(provider); }
            }
            finally { Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", previous); }
        }

        private sealed class ClockProvider : IPluginClusterProvider, IPluginClusterClockProvider
        {
            public NodeContext Context => new NodeContext { Node = "node-1", Available = true };
            public event Action ContextChanged { add { } remove { } }
            public Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public Task<PluginResult> RequestAsync(string plugin, PluginTarget target, string topic, byte[] payload,
                Guid operationId, TimeSpan timeout, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public IDisposable RegisterHandler(string plugin, string topic, Func<PluginMessage, Task<byte[]>> handler) => null;
            public PluginClusterTime Time() => new PluginClusterTime { ClockMilliseconds = 1234, Synchronized = false };
        }
    }
}
