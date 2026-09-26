using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using Xunit;

namespace PluginSdk.Tests
{
    [Collection("ServerControl")]
    public sealed class PluginViewTests
    {
        private const string Id = "Cluster State.dll";

        private static PluginClusterClient Client()
        {
            PluginCluster.BindOwner(Id, typeof(PluginViewTests).Assembly);
            return PluginCluster.ForPlugin(Id);
        }

        // A ModAPI interface answering from a member-name table; enough of a game session for the plain-server views.
        public class Fake : DispatchProxy
        {
            public Dictionary<string, Func<object[], object>> Members = new Dictionary<string, Func<object[], object>>();
            protected override object Invoke(MethodInfo method, object[] args)
            {
                string name = method.Name.StartsWith("get_") ? method.Name.Substring(4) : method.Name;
                if (Members.TryGetValue(name, out var member)) return member(args);
                return method.ReturnType.IsValueType && method.ReturnType != typeof(void) ? Activator.CreateInstance(method.ReturnType) : null;
            }
            public static T Create<T>(Dictionary<string, Func<object[], object>> members)
            {
                object proxy = DispatchProxy.Create<T, Fake>();
                ((Fake)proxy).Members = members;
                return (T)proxy;
            }
        }

        private static IMyEntity Entity(long id, Vector3D position, Func<IMyEntity> parent = null)
        {
            IMyEntity self = null;
            self = Fake.Create<IMyEntity>(new Dictionary<string, Func<object[], object>> {
                ["EntityId"] = _ => id,
                ["WorldMatrix"] = _ => MatrixD.CreateTranslation(position),
                ["GetTopMostParent"] = _ => parent?.Invoke() ?? self,
            });
            return self;
        }

        private static IMyPlayer Player(ulong steam, long identity, string name, MyPromoteLevel level, bool bot, IMyCharacter character) =>
            Fake.Create<IMyPlayer>(new Dictionary<string, Func<object[], object>> {
                ["SteamUserId"] = _ => steam, ["IdentityId"] = _ => identity, ["DisplayName"] = _ => name,
                ["PromoteLevel"] = _ => level, ["IsBot"] = _ => bot, ["Character"] = _ => character,
                ["GetPosition"] = _ => new Vector3D(-1, -1, -1),
            });

        private static void WithSession(IMyPlayer[] players, IMyEntity[] entities, Action body)
        {
            var oldPlayers = MyAPIGateway.Players; var oldEntities = MyAPIGateway.Entities;
            try
            {
                MyAPIGateway.Players = Fake.Create<IMyPlayerCollection>(new Dictionary<string, Func<object[], object>> {
                    ["GetPlayers"] = args => {
                        var list = (List<IMyPlayer>)args[0]; var collect = (Func<IMyPlayer, bool>)args[1];
                        foreach (var player in players) if (collect == null || collect(player)) list.Add(player);
                        return null;
                    },
                });
                MyAPIGateway.Entities = Fake.Create<IMyEntities>(new Dictionary<string, Func<object[], object>> {
                    ["GetEntityById"] = args => Array.Find(entities, e => e.EntityId == (long)args[0]),
                });
                body();
            }
            finally { MyAPIGateway.Players = oldPlayers; MyAPIGateway.Entities = oldEntities; }
        }

        [Fact]
        public void Plain_server_views_are_the_local_players_and_entities()
        {
            var character = Fake.Create<IMyCharacter>(new Dictionary<string, Func<object[], object>> {
                ["EntityId"] = _ => 77L, ["WorldMatrix"] = _ => MatrixD.CreateTranslation(1, 2, 3) });
            var grid = Entity(500, Vector3D.Zero);
            var block = Entity(501, Vector3D.Zero, () => grid);
            var players = new[] {
                Player(11, 1001, "Ann", MyPromoteLevel.Admin, false, character),
                Player(12, 1002, "Bob", MyPromoteLevel.None, false, null),
                Player(0, 1003, "Bot", MyPromoteLevel.None, true, null),
            };
            var client = Client();
            WithSession(players, new[] { grid, block }, () =>
            {
                var online = client.OnlinePlayers();
                Assert.Equal(new[] { "Ann", "Bob" }, Array.ConvertAll(new List<PluginPlayerInfo>(online).ToArray(), p => p.Name));
                Assert.All(online, p => Assert.Equal("standalone", p.Node));
                Assert.True(online[0].IsAdmin); Assert.False(online[1].IsAdmin);
                Assert.Equal(11UL, online[0].SteamId); Assert.Equal(1001, online[0].IdentityId);
                var positions = client.PlayerPositions();
                Assert.Equal(2, positions.Count);
                Assert.Equal(77, positions[0].EntityId); Assert.Equal(new Vector3D(1, 2, 3), positions[0].Position);
                Assert.Equal(0, positions[1].EntityId); Assert.Equal(new Vector3D(-1, -1, -1), positions[1].Position);
                var placed = client.LocateEntity(501);
                Assert.Equal(PluginEntityResidence.Local, placed.Residence);
                Assert.Equal(500, placed.EntityId); Assert.Equal("standalone", placed.Node); Assert.Equal(0UL, placed.Partition);
                Assert.Equal(PluginEntityResidence.Absent, client.LocateEntity(9).Residence);
                Assert.Null(client.LocateEntity(9).Node);
            });
        }

        [Fact]
        public void Plain_server_before_a_session_has_empty_views_not_null()
        {
            var client = Client();
            var oldPlayers = MyAPIGateway.Players; var oldEntities = MyAPIGateway.Entities;
            try
            {
                MyAPIGateway.Players = null; MyAPIGateway.Entities = null;
                Assert.Empty(client.OnlinePlayers());
                Assert.Empty(client.PlayerPositions());
                Assert.Equal(PluginEntityResidence.Absent, client.LocateEntity(1).Residence);
            }
            finally { MyAPIGateway.Players = oldPlayers; MyAPIGateway.Entities = oldEntities; }
        }

        [Fact]
        public void Cluster_process_uses_the_provider_and_never_the_local_views()
        {
            var client = Client();
            string previous = Environment.GetEnvironmentVariable("CLUSTER_NODE_ID");
            Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", "node-1");
            try
            {
                WithSession(new[] { Player(11, 1001, "Local", MyPromoteLevel.None, false, null) }, new IMyEntity[0], () =>
                {
                    Assert.Null(PluginCluster.Current);
                    Assert.Null(client.OnlinePlayers());           // no provider: unknown, not the local list
                    Assert.Null(client.LocateEntity(1));
                    var legacy = new NoViewsProvider();
                    Assert.True(PluginCluster.Register(legacy));
                    try { Assert.Null(client.PlayerPositions()); }
                    finally { PluginCluster.Unregister(legacy); }
                    var views = new ViewProvider();
                    Assert.True(PluginCluster.Register(views));
                    try
                    {
                        Assert.Equal("node-2", Assert.Single(client.OnlinePlayers()).Node);
                        Assert.Equal(5, Assert.Single(client.PlayerPositions()).EntityId);
                        Assert.Equal(PluginEntityResidence.Frozen, client.LocateEntity(42).Residence);
                        Assert.Equal(42, views.Located);
                    }
                    finally { PluginCluster.Unregister(views); }
                });
            }
            finally { Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", previous); }
        }

        private class NoViewsProvider : IPluginClusterProvider
        {
            public NodeContext Context => new NodeContext { Node = "node-1", Available = true };
            public event Action ContextChanged { add { } remove { } }
            public Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public Task<PluginResult> RequestAsync(string plugin, PluginTarget target, string topic, byte[] payload,
                Guid operationId, TimeSpan timeout, CancellationToken cancellationToken) =>
                Task.FromResult(PluginResult.Failure(PluginResultCode.Unsupported));
            public IDisposable RegisterHandler(string plugin, string topic, Func<PluginMessage, Task<byte[]>> handler) => null;
        }

        private sealed class ViewProvider : NoViewsProvider, IPluginClusterViewProvider
        {
            public long Located;
            public IReadOnlyList<PluginPlayerInfo> OnlinePlayers() => new[] { new PluginPlayerInfo { Node = "node-2", Name = "Remote" } };
            public IReadOnlyList<PluginPlayerPosition> PlayerPositions() => new[] { new PluginPlayerPosition { EntityId = 5 } };
            public PluginEntityPlacement LocateEntity(long entityId)
            {
                Located = entityId;
                return new PluginEntityPlacement { EntityId = entityId, Residence = PluginEntityResidence.Frozen, Partition = 3, Node = "node-1" };
            }
        }
    }
}
