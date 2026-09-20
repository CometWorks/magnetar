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
        public async Task Standalone_storage_is_lazy_and_failure_does_not_break_opt_in()
        {
            string root = Path.Combine(Path.GetTempPath(), "plugin-lazy-" + Guid.NewGuid());
            int failures = 0, attempts = 0;
            PluginCluster.BindOwner("Cluster State.dll", typeof(PluginClusterTests).Assembly);
            try
            {
                PluginCluster.ConfigureStandalone(() => { attempts++; return new StandalonePluginProvider(root); }, _ => failures++);
                Assert.Null(PluginCluster.Current);
                Assert.False(Directory.Exists(root));
                Assert.Throws<InvalidOperationException>(() => PluginCluster.ForPlugin("someone-else"));
                Assert.Equal(0, attempts);
                using (var occupied = new StandalonePluginProvider(root))
                {
                    var client = PluginCluster.ForPlugin("Cluster State.dll");
                    Assert.Equal(PluginResultCode.Unavailable, (await client.ReadAsync("a")).Code);
                    PluginCluster.ForPlugin("Cluster State.dll");
                    Assert.Equal(1, attempts);
                    Assert.Equal(1, failures);
                    Assert.Null(PluginCluster.Current);
                }
                PluginCluster.ConfigureStandalone(() => new StandalonePluginProvider(root), _ => failures++);
                var available = PluginCluster.ForPlugin("Cluster State.dll");
                Assert.Equal(PluginResultCode.NotFound, (await available.ReadAsync("a")).Code);
                Assert.Equal(1, failures);
            }
            finally
            {
                (PluginCluster.Current as IDisposable)?.Dispose();
                PluginCluster.ConfigureStandalone(null, null);
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Fact]
        public void Namespace_tombstones_cannot_starve_other_plugins_or_allow_stale_cas()
        {
            var store = new PluginRecordStore();
            PluginRequest Write(string plugin, string key, long revision = 0, byte[] payload = null) => new PluginRequest {
                Plugin = plugin, Key = key, Action = "cas", Expected = new RecordVersion { StoreId = store.StoreId, Revision = revision },
                SchemaVersion = 1, Deleted = payload == null, Payload = payload, OperationId = Guid.NewGuid() };
            for (int i = 0; i < PluginRecordStore.MaxPluginRecords; i++)
                Assert.Equal(PluginResultCode.Success, store.Execute(Write("one", "key" + i)).Code);
            Assert.Equal(PluginResultCode.Capacity, store.Execute(Write("one", "extra")).Code);
            Assert.Equal(PluginResultCode.Success, store.Execute(Write("two", "key")).Code);
            Assert.Equal(PluginResultCode.Conflict, store.Execute(Write("one", "key0")).Code);
            Assert.Equal(PluginResultCode.Success, store.Execute(Write("one", "key0", 1, new byte[] { 1 })).Code);
            Assert.True(store.Replays.Keys.Count(key => key.StartsWith("one\n")) <= PluginRecordStore.MaxPluginReplays);
        }

        [Fact]
        public void Namespace_payload_and_replay_bounds_leave_capacity_for_other_plugins()
        {
            var store = new PluginRecordStore();
            PluginRequest Write(string plugin, string key, long revision = 0) => new PluginRequest {
                Plugin = plugin, Key = key, Action = "cas", Expected = new RecordVersion { StoreId = store.StoreId, Revision = revision },
                SchemaVersion = 1, Payload = new byte[64 * 1024], OperationId = Guid.NewGuid() };
            int accepted = 0;
            for (; accepted < 32; accepted++)
                if (store.Execute(Write("one", "key" + accepted)).Code == PluginResultCode.Capacity) break;
            Assert.InRange(accepted, 1, 16);
            Assert.Equal(PluginResultCode.Success, store.Execute(Write("two", "key")).Code);
            Assert.True(store.Replays.Where(pair => pair.Key.StartsWith("one\n"))
                .Sum(pair => pair.Value.Record.Payload.Length) <= PluginRecordStore.MaxPluginReplayBytes);
            var small = new PluginRecordStore();
            for (int i = 0; i < 100; i++)
            {
                var request = Write("one", "same", i);
                request.Expected.StoreId = small.StoreId;
                Assert.Equal(PluginResultCode.Success, small.Execute(request).Code);
            }
        }

        [Fact]
        public void Existing_over_quota_namespace_can_delete_and_shrink_without_losing_records()
        {
            var store = new PluginRecordStore();
            for (int i = 0; i < 20; i++) store.Records["old\n" + i] = new PluginRecord {
                Version = new RecordVersion { StoreId = store.StoreId, Revision = 1 }, SchemaVersion = 1, Payload = new byte[64 * 1024] };
            store.Validate();
            var request = new PluginRequest { Plugin = "old", Key = "0", Action = "cas",
                Expected = new RecordVersion { StoreId = store.StoreId, Revision = 1 }, SchemaVersion = 1, Deleted = true, OperationId = Guid.NewGuid() };
            Assert.Equal(PluginResultCode.Success, store.Execute(request).Code);
            request.Key = "1"; request.Deleted = false; request.Payload = new byte[1]; request.OperationId = Guid.NewGuid();
            Assert.Equal(PluginResultCode.Success, store.Execute(request).Code);
            Assert.Equal(20, store.Records.Count);
        }

        [Fact]
        public void Context_notifications_preserve_provider_when_observer_throws()
        {
            string root = Path.Combine(Path.GetTempPath(), "plugin-context-" + Guid.NewGuid());
            int calls = 0;
            Action observer = () => { calls++; throw new InvalidOperationException("consumer"); };
            PluginCluster.ContextChanged += observer;
            try
            {
                using var provider = new StandalonePluginProvider(root);
                Assert.True(PluginCluster.Register(provider));
                Assert.Same(provider, PluginCluster.Current);
                Assert.Equal(1, provider.Context.WorldAuthorityGeneration);
                Assert.Empty(provider.Context.OwnedPartitions);
                Assert.True(PluginCluster.Unregister(provider));
                Assert.Equal(2, calls);
            }
            finally { PluginCluster.ContextChanged -= observer; Directory.Delete(root, true); }
        }

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
