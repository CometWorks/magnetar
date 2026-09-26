using System;
using System.IO;
using PluginSdk.Clustering;
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

        // PluginClusterTests binds this test assembly under the same id; the loader identity is per assembly.
        private const string Id = "Cluster State.dll";
        private static string Folder => PluginStorage.FolderName(Id);

        public PluginStorageTests() => PluginCluster.BindOwner(Id, typeof(PluginStorageTests).Assembly);

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
                    string directory = PluginStorage.GetSharedDirectory(Id);
                    Assert.Equal(Path.Combine(Path.GetFullPath(root), "plugins", Folder), directory);
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
                WithEnvironment("node-1", shared, () => fromNode1 = PluginStorage.GetSharedDirectory(Id));
                WithEnvironment("wa-1", shared, () =>
                {
                    Assert.True(PluginStorage.IsClusterShared);
                    fromWa = PluginStorage.GetSharedDirectory(Id);
                });
                Assert.Equal(Path.Combine(Path.GetFullPath(shared), "plugins", Folder), fromNode1);
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
                    Assert.Throws<InvalidOperationException>(() => PluginStorage.GetSharedDirectory(Id));
                });
                Assert.False(Directory.Exists(local));
            }
            finally { if (Directory.Exists(local)) Directory.Delete(local, true); }
        }

        [Fact]
        public void AnotherPluginsIdIsRefused()
        {
            string root = TempRoot();
            try
            {
                PluginStorage.ConfigureStandalone(root);
                WithEnvironment(null, null, () =>
                    Assert.Throws<InvalidOperationException>(() => PluginStorage.GetSharedDirectory("someone-else")));
                Assert.False(Directory.Exists(Path.Combine(root, "plugins", "someone-else")));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("bad\\id")]
        public void InvalidIdsAreRefused(string pluginId)
        {
            Assert.Throws<ArgumentException>(() => PluginStorage.GetSharedDirectory(pluginId));
        }

        [Theory]
        [InlineData("my.plugin")]
        [InlineData("cluster-test-node")]
        [InlineData("Cluster State.dll")]
        [InlineData("Owner/Repo")]
        [InlineData("../escape")]
        [InlineData("a:b")]
        [InlineData("x..y")]
        public void EveryIdBecomesASafePrefixPlusItsHash(string pluginId)
        {
            string folder = PluginStorage.FolderName(pluginId);
            Assert.Matches("^[A-Za-z0-9._-]+-[0-9a-f]{16}$", folder);
            Assert.DoesNotContain("..", folder);
            Assert.Equal(folder, PluginStorage.FolderName(pluginId));
            Assert.NotEqual(folder, PluginStorage.FolderName(pluginId + " "));
        }

        // Gap analysis: a plain id passed through unchanged could spell another id's hashed folder, and
        // ids differing only in case shared one folder on a case-insensitive filesystem (Windows).
        [Theory]
        [InlineData("Cluster State.dll", "Cluster_State.dll-6a7d851f")]
        [InlineData("my.plugin", "My.Plugin")]
        [InlineData("my.plugin", "MY.PLUGIN")]
        [InlineData("a b", "a_b")]
        [InlineData("plugin", "...")]   // an all-dot id falls back to the "plugin" prefix
        public void DistinctIdsNeverShareAFolderEvenIgnoringCase(string first, string second)
        {
            string a = PluginStorage.FolderName(first), b = PluginStorage.FolderName(second);
            Assert.False(string.Equals(a, b, StringComparison.OrdinalIgnoreCase), a + " == " + b);
        }
    }
}
