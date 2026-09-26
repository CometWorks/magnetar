#nullable disable
using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
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
        /// The plugin's own directory, <c>&lt;root&gt;/plugins/&lt;folder&gt;</c>, created if missing. The folder is
        /// <see cref="FolderName"/> of the id: a file-safe prefix of it plus a hash of the exact id, so two ids
        /// never share a folder, not even ids that differ only in case on a case-insensitive filesystem.
        ///
        /// Call it from the plugin's own assembly with the id Magnetar loaded it under: the same identity rule
        /// as <see cref="PluginCluster.ForPlugin"/>, so a plugin cannot take another plugin's folder.
        ///
        /// In a cluster every node and the World Authority get the same directory with the same content, and
        /// they write it concurrently: nothing locks it. Write a file under a temporary name in the same
        /// directory and rename it into place, and give each file one writer (e.g. name it by the node id
        /// in <see cref="PluginCluster"/>'s context) or coordinate through the plugin's own messages.
        /// </summary>
        /// <exception cref="InvalidOperationException">The id is not the calling assembly's loader identity, or
        /// a cluster process has no configured shared storage (handing out a directory only this node sees would
        /// split the plugin's state silently).</exception>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetSharedDirectory(string pluginId)
        {
            if (!PluginRecordStore.ValidName(pluginId))
                throw new ArgumentException("Plugin identity cannot be used as a storage folder.", nameof(pluginId));
            if (!PluginCluster.IsBoundOwner(Assembly.GetCallingAssembly(), pluginId))
                throw new InvalidOperationException("Plugin namespace does not match its loader identity.");
            string root = Root;
            if (root == null)
                throw new InvalidOperationException(PluginCluster.IsClusterProcess
                    ? "This cluster has no shared storage configured (" + SharedRootVariable + ")"
                    : "No local storage directory is available");
            string directory = Path.Combine(root, "plugins", FolderName(pluginId));
            Directory.CreateDirectory(directory);
            return directory;
        }

        /// <summary>
        /// The folder name of a plugin id under <c>&lt;root&gt;/plugins</c>: a readable, file-safe prefix of the id,
        /// <c>-</c>, and 16 hex digits of the SHA-256 of the exact id.
        ///
        /// Every id goes through the same rule. Passing a plain id through unchanged let it spell another id's
        /// hashed folder ("Cluster_State.dll-6a7d851f" was also the folder of "Cluster State.dll"), and ids that
        /// differ only in case shared a folder on a case-insensitive filesystem. Here the hash is taken over the
        /// exact, case-sensitive id and written in lower case, so distinct ids differ in the hash part itself.
        /// </summary>
        public static string FolderName(string pluginId)
        {
            if (pluginId == null) throw new ArgumentNullException(nameof(pluginId));
            var safe = new StringBuilder();
            foreach (char c in pluginId)
            {
                bool dot = c == '.' && (safe.Length == 0 || safe[safe.Length - 1] != '.');
                safe.Append(char.IsLetterOrDigit(c) && c < 128 || dot || c == '_' || c == '-' ? c : '_');
            }
            string prefix = safe.ToString().Trim('.');
            if (prefix.Length > 48) prefix = prefix.Substring(0, 48).TrimEnd('.');
            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(pluginId));
                return (prefix.Length == 0 ? "plugin" : prefix) + "-" + BitConverter.ToString(hash, 0, 8).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
