using System;
using System.IO;
using PluginSdk.Storage;
using Xunit;

namespace PluginSdkTests
{
    // Same collection as the other tests that read the CLUSTER_* process environment, so none of them run
    // while this one has a cluster variable set.
    [Collection("ServerControl")]
    public sealed class PluginStorageTests
    {
        private static void WithEnvironment(string nodeId, string sharedRoot, Action body)
        {
            string oldNode = Environment.GetEnvironmentVariable("CLUSTER_NODE_ID");
            string oldShared = Environment.GetEnvironmentVariable(PluginStorage.SharedRootVariable);
            string oldRegistry = Environment.GetEnvironmentVariable("CLUSTER_GATEWAY_REGISTRY");
            try
            {
                Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", nodeId);
                Environment.SetEnvironmentVariable(PluginStorage.SharedRootVariable, sharedRoot);
                Environment.SetEnvironmentVariable("CLUSTER_GATEWAY_REGISTRY", null);
                body();
            }
            finally
            {
                Environment.SetEnvironmentVariable("CLUSTER_NODE_ID", oldNode);
                Environment.SetEnvironmentVariable(PluginStorage.SharedRootVariable, oldShared);
                Environment.SetEnvironmentVariable("CLUSTER_GATEWAY_REGISTRY", oldRegistry);
            }
        }

        private static string TempRoot() => Path.Combine(Path.GetTempPath(), "plugin-storage-" + Guid.NewGuid().ToString("N"));

        [Fact]
        public void PlainServerUsesTheLaunchersLocalRoot()
        {
            string root = TempRoot();
            try
            {
                WithEnvironment(null, Path.Combine(root, "ignored-shared"), () =>
                {
                    PluginStorage.ConfigureStandalone(root);
                    Assert.False(PluginStorage.IsClusterShared);
                    string directory = PluginStorage.GetSharedDirectory("my.plugin");
                    Assert.Equal(Path.Combine(Path.GetFullPath(root), "plugins", "my.plugin"), directory);
                    Assert.True(Directory.Exists(directory));
                });
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Fact]
        public void ClusterProcessUsesTheSharedRootOnEveryNode()
        {
            string local = TempRoot(), shared = TempRoot();
            try
            {
                PluginStorage.ConfigureStandalone(local);
                string fromNode1 = null, fromWa = null;
                WithEnvironment("node-1", shared, () => fromNode1 = PluginStorage.GetSharedDirectory("my.plugin"));
                WithEnvironment("wa-1", shared, () =>
                {
                    Assert.True(PluginStorage.IsClusterShared);
                    fromWa = PluginStorage.GetSharedDirectory("my.plugin");
                });
                Assert.Equal(Path.Combine(Path.GetFullPath(shared), "plugins", "my.plugin"), fromNode1);
                Assert.Equal(fromNode1, fromWa);
                Assert.False(Directory.Exists(local));
            }
            finally
            {
                foreach (string root in new[] { local, shared })
                    if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Fact]
        public void ClusterProcessWithoutSharedStorageRefusesInsteadOfGoingLocal()
        {
            string local = TempRoot();
            try
            {
                PluginStorage.ConfigureStandalone(local);
                WithEnvironment("node-1", null, () =>
                {
                    Assert.Null(PluginStorage.Root);
                    Assert.Throws<InvalidOperationException>(() => PluginStorage.GetSharedDirectory("my.plugin"));
                });
                Assert.False(Directory.Exists(local));
            }
            finally { if (Directory.Exists(local)) Directory.Delete(local, true); }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("../escape")]
        [InlineData("a/b")]
        [InlineData(".hidden")]
        [InlineData("x..y")]
        public void PluginIdCannotLeaveItsDirectory(string pluginId)
        {
            Assert.Throws<ArgumentException>(() => PluginStorage.GetSharedDirectory(pluginId));
        }
    }
}
