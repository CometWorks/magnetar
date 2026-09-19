using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk.Clustering;
using Xunit;

namespace PluginSdk.Tests
{
    [Collection("ServerControl")]
    public sealed class PluginClusterTests
    {
        [Fact]
        public async Task Queued_handlers_recheck_authority_and_disposal_before_execution()
        {
            var handlers = new PluginHandlers();
            int calls = 0;
            using var registration = handlers.Register("test", "echo", message => { calls++; return Task.FromResult(message.Payload); });
            bool valid = true;
            var message = new PluginMessage { Plugin = "test", Topic = "echo" };
            var queued = handlers.Enqueue(message, () => valid);
            valid = false;
            handlers.Update();
            Assert.Equal(PluginResultCode.Fenced, (await queued).Code);
            queued = handlers.Enqueue(message, () => true);
            handlers.Dispose();
            Assert.Equal(PluginResultCode.Fenced, (await queued).Code);
            Assert.Equal(0, calls);
            Assert.Equal(PluginResultCode.Unavailable, (await handlers.Enqueue(message, () => true)).Code);
        }

        [Fact]
        public void Cas_replay_conflict_tombstone_and_restore_fence()
        {
            var store = new PluginRecordStore();
            var missing = store.Execute(new PluginRequest { Plugin = "test", Key = "record", Action = "read" });
            Assert.Equal(PluginResultCode.NotFound, missing.Code);
            var write = new PluginRequest { Plugin = "test", Key = "record", Action = "cas", Expected = missing.Record.Version,
                SchemaVersion = 1, Payload = new byte[] { 42 }, OperationId = Guid.NewGuid() };
            var first = store.Execute(write);
            Assert.Equal(PluginResultCode.Success, first.Code);
            Assert.Equal(1, store.Execute(write).Record.Version.Revision);
            write.OperationId = Guid.NewGuid();
            Assert.Equal(PluginResultCode.Conflict, store.Execute(write).Code);
            write.Expected = first.Record.Version; write.Deleted = true; write.Payload = null;
            Assert.Equal(2, store.Execute(write).Record.Version.Revision);
            var deleted = store.Execute(new PluginRequest { Plugin = "test", Key = "record", Action = "read" });
            Assert.Equal(PluginResultCode.NotFound, deleted.Code);
            Assert.Equal(2, deleted.Record.Version.Revision);
            store.Restored();
            Assert.Equal(PluginResultCode.Fenced, store.Execute(write).Code);
        }

        [Fact]
        public async Task Standalone_persists_conflicts_across_restart_and_dispatches_on_update()
        {
            string root = Path.Combine(Path.GetTempPath(), "plugin-store-" + Guid.NewGuid());
            RecordVersion version;
            try
            {
                using (var provider = new StandalonePluginProvider(root))
                {
                    var read = await provider.ExecuteAsync(new PluginRequest { Plugin = "test", Key = "a", Action = "read" }, default);
                    version = read.Record.Version;
                    var writes = Enumerable.Range(0, 8).Select(i => Task.Run(() => provider.ExecuteAsync(new PluginRequest {
                        Plugin = "test", Key = "a", Action = "cas", Expected = version, SchemaVersion = 1,
                        Payload = new byte[] { (byte)i }, OperationId = Guid.NewGuid() }, default)));
                    Assert.Single((await Task.WhenAll(writes)).Where(r => r.Code == PluginResultCode.Success));
                    int thread = Environment.CurrentManagedThreadId, called = 0;
                    using var registration = provider.RegisterHandler("test", "echo", message => {
                        called++; Assert.Equal(thread, Environment.CurrentManagedThreadId); return Task.FromResult(message.Payload); });
                    var reply = provider.RequestAsync("test", new PluginTarget { Kind = PluginTargetKind.WorldAuthority }, "echo",
                        new byte[] { 9 }, Guid.NewGuid(), TimeSpan.FromSeconds(2), default);
                    Assert.False(reply.IsCompleted);
                    provider.Update();
                    Assert.Equal(new byte[] { 9 }, (await reply).Payload);
                    Assert.Equal(1, called);
                    using var cancel = new CancellationTokenSource(); cancel.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.RequestAsync("test",
                        new PluginTarget { Kind = PluginTargetKind.WorldAuthority }, "echo", null, Guid.NewGuid(), TimeSpan.FromSeconds(1), cancel.Token));
                }
                using var recovered = new StandalonePluginProvider(root);
                var result = await recovered.ExecuteAsync(new PluginRequest { Plugin = "test", Key = "a", Action = "read" }, default);
                Assert.Equal(version.StoreId, result.Record.Version.StoreId);
                Assert.Equal(1, result.Record.Version.Revision);
                Assert.Throws<IOException>(() => new StandalonePluginProvider(root));
            }
            finally { Directory.Delete(root, true); }
        }

        [Fact]
        public async Task Cluster_without_provider_cannot_fall_back_to_local_termination()
        {
            string previous = Environment.GetEnvironmentVariable("CLUSTER_NODE_ID");
            Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", "fixture");
            try
            {
                var request = new ClusterLifecycleRequest(Guid.NewGuid(), ServerTerminationKind.Shutdown, ClusterLifecycleOrigin.Plugin, true);
                Assert.True(ClusterLifecycle.TryRequest(request, out var task));
                Assert.Equal(ClusterLifecycleDisposition.Unavailable, (await task).Disposition);
                Assert.Throws<InvalidOperationException>(() => new StandalonePluginProvider("unused"));
            }
            finally { Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", previous); }
        }
    }
}
