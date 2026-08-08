using System;
using PluginSdk.Clustering;
using Xunit;

namespace PluginSdk.Tests
{
    public sealed class ClusterNodeLinkTests
    {
        [Fact]
        public void Register_and_unregister_require_the_same_provider()
        {
            var first = new TestNodeLink();
            var second = new TestNodeLink();

            Assert.True(ClusterNodeLink.Register(first));
            try
            {
                Assert.Same(first, ClusterNodeLink.Current);
                Assert.False(ClusterNodeLink.Register(second));
                Assert.False(ClusterNodeLink.Unregister(second));
                Assert.Same(first, ClusterNodeLink.Current);
            }
            finally
            {
                Assert.True(ClusterNodeLink.Unregister(first));
            }

            Assert.Null(ClusterNodeLink.Current);
        }

        private sealed class TestNodeLink : IClusterNodeLink
        {
            public bool IsConnected => false;
            public event Action<byte[]> MessageReceived { add { } remove { } }
            public event Action<bool> ConnectionChanged { add { } remove { } }
            public event Action<ulong> ClientDetached { add { } remove { } }
            public Func<ulong, byte[], bool> AttachValidator { get; set; }
            public bool Send(byte[] payload) => false;
        }
    }
}
