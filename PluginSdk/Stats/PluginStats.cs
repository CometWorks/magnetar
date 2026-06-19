using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using PluginSdk.Logging;

namespace PluginSdk.Stats
{
    /// <summary>
    /// Process-wide registry and pub/sub hub for plugin statistics. A plugin
    /// publishes a self-describing <see cref="StatsSnapshot"/> under a provider
    /// name; a consumer (Quasar.Agent, or another plugin) reads the latest
    /// snapshot by name, enumerates the active providers, or subscribes to
    /// <see cref="Updated"/> to receive every publication as it happens.
    ///
    /// <para>
    /// This is the generic transport the bandwidth plugin (and any future
    /// telemetry producer) uses; it has no knowledge of what the numbers mean —
    /// the schema carried in each <see cref="StatGroup"/> describes them. The API
    /// is intentionally <c>public</c> so plugins call it directly, with no
    /// <c>InternalsVisibleTo</c> coupling.
    /// </para>
    /// </summary>
    public static class PluginStats
    {
        private static readonly Logger Log = Logger.Create("PluginStats");

        private static readonly ConcurrentDictionary<string, StatsSnapshot> Current
            = new ConcurrentDictionary<string, StatsSnapshot>();

        /// <summary>
        /// Raised after each <see cref="Publish"/>, with the provider name and
        /// the snapshot. Subscribers are invoked synchronously on the publishing
        /// thread; a subscriber that throws is logged and does not affect the
        /// others or the publisher.
        /// </summary>
        public static event Action<string, StatsSnapshot> Updated;

        /// <summary>
        /// Publishes the latest snapshot for <paramref name="provider"/>,
        /// replacing any previous one, and notifies <see cref="Updated"/>
        /// subscribers.
        /// </summary>
        public static void Publish(string provider, StatsSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(provider)) throw new ArgumentException("Provider name is required.", nameof(provider));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            Current[provider] = snapshot;

            var handlers = Updated;
            if (handlers == null)
                return;

            // Invoke each subscriber in isolation: telemetry must never let one
            // consumer's exception break another consumer or the publisher.
            foreach (Action<string, StatsSnapshot> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(provider, snapshot);
                }
                catch (Exception ex)
                {
                    Log.Error($"Stats subscriber threw for provider '{provider}'", ex);
                }
            }
        }

        /// <summary>Gets the latest snapshot for <paramref name="provider"/>.
        /// Returns <c>false</c> when the provider has published nothing (or was
        /// cleared).</summary>
        public static bool TryGetSnapshot(string provider, out StatsSnapshot snapshot)
            => Current.TryGetValue(provider, out snapshot);

        /// <summary>The provider names that currently have a snapshot. The list
        /// is a point-in-time copy.</summary>
        public static IReadOnlyList<string> Providers
            => new List<string>(Current.Keys);

        /// <summary>Removes <paramref name="provider"/>'s snapshot, e.g. when its
        /// plugin is disabled at runtime. Idempotent; does not raise
        /// <see cref="Updated"/>.</summary>
        public static void Clear(string provider)
        {
            if (string.IsNullOrEmpty(provider))
                return;
            Current.TryRemove(provider, out _);
        }
    }
}
