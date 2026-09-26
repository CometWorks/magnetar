#nullable disable
using System;

namespace PluginSdk.Clustering
{
    /// <summary>
    /// A plugin's view of grids moving between cluster nodes (T-0256). Every call is on the game thread and
    /// names a top-level grid; the grid keeps its entity id across the move. Never called on a plain server
    /// or on the World Authority, which hold no moving grids.
    /// </summary>
    public interface IPluginGridHandover
    {
        /// <summary>
        /// Source node: the grid is frozen and being captured for another node. Return the plugin's state
        /// for it (at most <see cref="PluginGridHandover.MaxStateBytes"/>) to receive it in
        /// <see cref="Arrived"/> on the target, or null. Keep it fast: the capture waits for it.
        /// </summary>
        byte[] Leaving(long gridId);
        /// <summary>Source node: the move committed; the grid now lives on another node and is gone here.</summary>
        void Left(long gridId);
        /// <summary>Source node: the move was abandoned; the grid stays here and resumes.</summary>
        void Stayed(long gridId);
        /// <summary>
        /// Target node: the grid arrived and is live here. <paramref name="state"/> is what
        /// <see cref="Leaving"/> returned on the source, or null (none, or a node restart lost it).
        /// </summary>
        void Arrived(long gridId, byte[] state);
    }

    /// <summary>Optional provider capability: deliver grid handover hooks.</summary>
    public interface IPluginClusterHandoverProvider
    {
        IDisposable RegisterGridHandover(string plugin, IPluginGridHandover handler);
    }

    public static class PluginGridHandover
    {
        /// <summary>The largest state one plugin may carry for one grid; a larger one is dropped (logged).</summary>
        public const int MaxStateBytes = 16 * 1024;

        internal sealed class NoRegistration : IDisposable
        {
            public static readonly NoRegistration Instance = new NoRegistration();
            public void Dispose() { }
        }
    }
}
