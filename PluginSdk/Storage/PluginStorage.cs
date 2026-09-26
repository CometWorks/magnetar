#nullable disable
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using PluginSdk.Clustering;

namespace PluginSdk.Storage
{
    /// <summary>
    /// A plugin's shared storage directory: one folder per plugin that every process of the server sees.
    ///
    /// On a plain dedicated server that is a local directory of this server. In a cluster it is the
    /// cluster-wide shared storage (CLUSTER_SHARED_ROOT, set by the cluster launcher for every node and the
    /// World Authority), so a file one node writes is read by the others. The call and the path layout are
    /// the same in both cases; <see cref="IsClusterShared"/> tells which one this process has.
    /// </summary>
    public static class PluginStorage
    {
        /// <summary>The cluster launcher's shared storage root, the same path on every node and the WA.</summary>
        public const string SharedRootVariable = "CLUSTER_SHARED_ROOT";

        private static readonly Regex PluginIdPattern = new Regex("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$");
        private static string standaloneRoot;

        /// <summary>
        /// The launcher's per-server storage root for a plain dedicated server. Ignored in a cluster process.
        /// Without it the root is the user's local application data (Magnetar/PluginShared).
        /// </summary>
        public static void ConfigureStandalone(string root)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentException("root is required", nameof(root));
            Volatile.Write(ref standaloneRoot, Path.GetFullPath(root));
        }

        /// <summary>True in a cluster node or World Authority: the directory is shared cluster-wide.</summary>
        public static bool IsClusterShared => PluginCluster.IsClusterProcess;

        /// <summary>
        /// The storage root, or null in a cluster process whose launcher configured no shared storage.
        /// </summary>
        public static string Root
        {
            get
            {
                if (PluginCluster.IsClusterProcess)
                {
                    string shared = Environment.GetEnvironmentVariable(SharedRootVariable);
                    return string.IsNullOrWhiteSpace(shared) ? null : Path.GetFullPath(shared);
                }
                string configured = Volatile.Read(ref standaloneRoot);
                if (configured != null) return configured;
                string data = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return string.IsNullOrEmpty(data) ? null : Path.Combine(data, "Magnetar", "PluginShared");
            }
        }

        /// <summary>
        /// The plugin's own directory, <c>&lt;root&gt;/plugins/&lt;pluginId&gt;</c>, created if missing.
        ///
        /// In a cluster every node and the World Authority get the same directory with the same content, and
        /// they write it concurrently: nothing locks it. Write a file under a temporary name in the same
        /// directory and rename it into place, and give each file one writer (e.g. name it by the node id
        /// in <see cref="PluginCluster"/>'s context) or coordinate through the plugin's own messages.
        /// </summary>
        /// <param name="pluginId">1-64 letters, digits, '.', '_' or '-', starting with a letter or digit.</param>
        /// <exception cref="InvalidOperationException">A cluster process without configured shared storage:
        /// handing out a directory only this node sees would split the plugin's state silently.</exception>
        public static string GetSharedDirectory(string pluginId)
        {
            if (pluginId == null || !PluginIdPattern.IsMatch(pluginId) || pluginId.Contains(".."))
                throw new ArgumentException("pluginId must be 1-64 letters, digits, '.', '_' or '-', starting with a letter or digit",
                    nameof(pluginId));
            string root = Root;
            if (root == null)
                throw new InvalidOperationException(PluginCluster.IsClusterProcess
                    ? "This cluster has no shared storage configured (" + SharedRootVariable + ")"
                    : "No local storage directory is available");
            string directory = Path.Combine(root, "plugins", pluginId);
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
