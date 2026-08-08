using System;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk;
using PluginSdk.Clustering;
using Xunit;

namespace PluginSdk.Tests
{
    [Collection("ServerControl")]
    public sealed class ClusterLifecycleTests
    {
        [Fact]
        public async Task Registered_provider_routes_all_paths_without_local_termination()
        {
            int localCalls = 0;
            var provider = new RecordingProvider();
            ServerControl.Bind(null, null, () => localCalls++, () => localCalls++,
                () => localCalls++, () => localCalls++);

            Assert.True(ClusterLifecycle.Register(provider));
            try
            {
                ServerControl.SaveAndQuit();
                ServerControl.SaveAndRestart();
                ServerControl.QuitWithoutSaving();
                ServerControl.RestartWithoutSaving();
                await provider.AllReceived.Task.WaitAsync(TimeSpan.FromSeconds(2));

                Assert.Equal(0, localCalls);
                Assert.Collection(provider.Requests,
                    request => AssertRequest(request, ServerTerminationKind.Shutdown, true),
                    request => AssertRequest(request, ServerTerminationKind.Restart, true),
                    request => AssertRequest(request, ServerTerminationKind.Shutdown, false),
                    request => AssertRequest(request, ServerTerminationKind.Restart, false));
            }
            finally
            {
                Assert.True(ClusterLifecycle.Unregister(provider));
            }
        }

        [Fact]
        public async Task Provider_registration_is_single_owner_and_fail_closed()
        {
            var broken = new BrokenProvider();
            var replacement = new RecordingProvider();
            var request = new ClusterLifecycleRequest(Guid.NewGuid(), ServerTerminationKind.Restart,
                ClusterLifecycleOrigin.Plugin, true);

            Assert.True(ClusterLifecycle.Register(broken));
            try
            {
                Assert.False(ClusterLifecycle.Register(replacement));
                Assert.False(ClusterLifecycle.Unregister(replacement));
                Assert.True(ClusterLifecycle.TryRequest(request, out Task<ClusterLifecycleAcknowledgement> pending));
                ClusterLifecycleAcknowledgement acknowledgement = await pending;
                Assert.Equal(ClusterLifecycleDisposition.Unavailable, acknowledgement.Disposition);
                Assert.Equal("provider_failure", acknowledgement.ReasonCode);
            }
            finally
            {
                Assert.True(ClusterLifecycle.Unregister(broken));
            }
        }

        [Fact]
        public void No_provider_preserves_standalone_behavior()
        {
            var request = new ClusterLifecycleRequest(Guid.NewGuid(), ServerTerminationKind.Restart,
                ClusterLifecycleOrigin.Plugin, true);

            Assert.False(ClusterLifecycle.TryRequest(request, out Task<ClusterLifecycleAcknowledgement> pending));
            Assert.Null(pending);
        }

        private static void AssertRequest(ClusterLifecycleRequest request,
            ServerTerminationKind kind, bool saveFirst)
        {
            Assert.Equal(kind, request.Kind);
            Assert.Equal(saveFirst, request.SaveFirst);
            Assert.Equal(ClusterLifecycleOrigin.Plugin, request.Origin);
            Assert.NotEqual(Guid.Empty, request.RequestId);
        }

        private sealed class RecordingProvider : IClusterLifecycleProvider
        {
            public readonly System.Collections.Generic.List<ClusterLifecycleRequest> Requests = new();
            public readonly TaskCompletionSource AllReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task<ClusterLifecycleAcknowledgement> RequestAsync(
                ClusterLifecycleRequest request, CancellationToken cancellationToken)
            {
                lock (Requests)
                {
                    Requests.Add(request);
                    if (Requests.Count == 4)
                        AllReceived.TrySetResult();
                }
                return Task.FromResult(new ClusterLifecycleAcknowledgement(request.RequestId,
                    ClusterLifecycleDisposition.Accepted, request.RequestId,
                    "node_restart_accepted", "Accepted.", "Closed"));
            }
        }

        private sealed class BrokenProvider : IClusterLifecycleProvider
        {
            public Task<ClusterLifecycleAcknowledgement> RequestAsync(
                ClusterLifecycleRequest request, CancellationToken cancellationToken) =>
                throw new InvalidOperationException("test");
        }
    }
}
