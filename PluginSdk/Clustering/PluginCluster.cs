#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PluginSdk.Clustering
{
    public interface IPluginClusterProvider
    {
        NodeContext Context { get; }
        event Action ContextChanged;
        Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken);
        Task<PluginResult> RequestAsync(string plugin, PluginTarget target, string topic, byte[] payload,
            Guid operationId, TimeSpan timeout, CancellationToken cancellationToken);
        IDisposable RegisterHandler(string plugin, string topic, Func<PluginMessage, Task<byte[]>> handler);
    }

    /// <summary>Opt-in plugin data services. Providers belong to the launcher/runtime, never Quasar or Agent.</summary>
    public static class PluginCluster
    {
        private static IPluginClusterProvider current;
        private static readonly object StandaloneSync = new object();
        private static Func<IPluginClusterProvider> standaloneFactory;
        private static Action<Exception> standaloneFailure;

        /// <summary>Configure storage without opening it. Only explicit plugin opt-in acquires the lease.</summary>
        public static void ConfigureStandalone(Func<IPluginClusterProvider> factory, Action<Exception> failure)
        {
            if (IsClusterProcess) throw new InvalidOperationException("Standalone provider is forbidden in cluster mode.");
            lock (StandaloneSync) { standaloneFactory = factory; standaloneFailure = failure; }
        }

        private static void EnsureStandalone()
        {
            lock (StandaloneSync)
            {
                if (Current != null || IsClusterProcess || standaloneFactory == null) return;
                var factory = standaloneFactory;
                standaloneFactory = null; // A failed durable store stays unavailable until restart.
                try
                {
                    var provider = factory();
                    if (!Register(provider)) (provider as IDisposable)?.Dispose();
                }
                catch (Exception error)
                {
                    try { standaloneFailure?.Invoke(error); } catch { /* Logging cannot break ordinary startup. */ }
                }
            }
        }
        private static readonly Dictionary<Assembly, string> Owners = new Dictionary<Assembly, string>();
        public static bool IsClusterProcess => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLUSTER_GATEWAY_REGISTRY"))
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLUSTER_NODE_ID"));
        public static IPluginClusterProvider Current => Volatile.Read(ref current);
        public static event Action ContextChanged;
        public static void BindOwner(string pluginId, Assembly assembly)
        {
            if (string.IsNullOrWhiteSpace(pluginId) || assembly == null) throw new ArgumentException("Invalid plugin owner.");
            lock (Owners) {
                if (Owners.TryGetValue(assembly, out var existing) && existing != pluginId)
                    throw new InvalidOperationException("Assembly already belongs to another plugin.");
                Owners[assembly] = pluginId;
            }
        }
        /// <summary>True when the loader bound <paramref name="assembly"/> to <paramref name="pluginId"/>; the
        /// identity rule of <see cref="ForPlugin"/>, shared with PluginStorage's per-plugin directory.</summary>
        internal static bool IsBoundOwner(Assembly assembly, string pluginId)
        {
            lock (Owners)
                return assembly != null && Owners.TryGetValue(assembly, out var owner) && owner == pluginId;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static PluginClusterClient ForPlugin(string pluginId)
        {
            lock (Owners)
                if (!Owners.TryGetValue(Assembly.GetCallingAssembly(), out var owner) || owner != pluginId)
                    throw new InvalidOperationException("Plugin namespace does not match its loader identity.");
            if (!PluginRecordStore.ValidName(pluginId))
                throw new ArgumentException("Plugin identity cannot be used as a shared-state namespace.", nameof(pluginId));
            EnsureStandalone();
            return new PluginClusterClient(pluginId);
        }
        public static bool Register(IPluginClusterProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (Interlocked.CompareExchange(ref current, provider, null) != null) return false;
            provider.ContextChanged += Changed;
            Changed(); return true;
        }
        public static bool Unregister(IPluginClusterProvider provider)
        {
            if (provider == null || Interlocked.CompareExchange(ref current, null, provider) != provider) return false;
            provider.ContextChanged -= Changed; Changed(); return true;
        }
        private static void Changed()
        {
            foreach (Action handler in ContextChanged?.GetInvocationList() ?? Array.Empty<Delegate>())
                try { handler(); } catch { /* Plugin observers cannot break provider fencing. */ }
        }
    }
    public sealed class PluginClusterClient
    {
        public string PluginId { get; }
        internal PluginClusterClient(string id) { PluginId = id; }
        public NodeContext Context => PluginCluster.Current?.Context ?? new NodeContext { Available = false };
        public event Action ContextChanged { add { PluginCluster.ContextChanged += value; } remove { PluginCluster.ContextChanged -= value; } }
        public Task<PluginResult> ReadAsync(string key, CancellationToken cancellationToken = default) =>
            Execute(new PluginRequest { Action = "read", Key = key }, cancellationToken);
        public Task<PluginResult> CompareExchangeAsync(string key, RecordVersion expected, int schemaVersion, byte[] payload,
            Guid operationId, OwnerFence fence = null, bool deleted = false, CancellationToken cancellationToken = default) =>
            Execute(new PluginRequest { Action = "cas", Key = key, Expected = expected, SchemaVersion = schemaVersion,
                Payload = payload, OperationId = operationId, Fence = fence, Deleted = deleted }, cancellationToken);
        public Task<PluginResult> ResolveOwnerAsync(PluginTarget target, CancellationToken cancellationToken = default) =>
            Execute(new PluginRequest { Action = "owner", Target = target }, cancellationToken);
        private Task<PluginResult> Execute(PluginRequest request, CancellationToken token)
        {
            request.Plugin = PluginId;
            var provider = PluginCluster.Current;
            return Protected(() => provider == null ? Task.FromResult(PluginResult.Failure(PluginResultCode.Unavailable))
                : provider.ExecuteAsync(request, token));
        }
        public Task<PluginResult> RequestAsync(PluginTarget target, string topic, byte[] payload, Guid operationId,
            TimeSpan timeout, CancellationToken cancellationToken = default) => Protected(() =>
                PluginCluster.Current?.RequestAsync(PluginId, target, topic, payload, operationId, timeout, cancellationToken)
                    ?? Task.FromResult(PluginResult.Failure(PluginResultCode.Unavailable)));
        public IDisposable RegisterHandler(string topic, Func<PluginMessage, Task<byte[]>> handler) =>
            (PluginCluster.Current ?? throw new InvalidOperationException("Plugin services are unavailable."))
                .RegisterHandler(PluginId, topic, handler);
        private static async Task<PluginResult> Protected(Func<Task<PluginResult>> action)
        {
            try { return await action().ConfigureAwait(false) ?? PluginResult.Failure(PluginResultCode.Unavailable); }
            catch (OperationCanceledException) { return PluginResult.Failure(PluginResultCode.Timeout); }
            catch { return PluginResult.Failure(PluginResultCode.Unavailable); }
        }
    }
}
