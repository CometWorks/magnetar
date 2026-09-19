#nullable disable
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PluginSdk.Clustering
{
    /// <summary>Launcher-owned, single-process durable provider. Cluster processes must never use this fallback.</summary>
    public sealed class StandalonePluginProvider : IPluginClusterProvider, IDisposable
    {
        private readonly object sync = new object();
        private readonly string path;
        private readonly FileStream lease;
        private PluginRecordStore store;
        private bool failed;
        private readonly PluginServiceDiagnostics diagnostics = new PluginServiceDiagnostics();
        private readonly PluginHandlers handlers = new PluginHandlers();
        public event Action ContextChanged;
        public NodeContext Context => new NodeContext { Node = "standalone", Incarnation = 1, Role = "Standalone", WorldAuthorityGeneration = 1, Available = !failed };
        public StandalonePluginProvider(string directory)
        {
            if (PluginCluster.IsClusterProcess) throw new InvalidOperationException("Standalone provider is forbidden in cluster mode.");
            CreateDirectory(directory);
            path = Path.Combine(directory, "records.json");
            lease = new FileStream(Path.Combine(directory, "owner.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            try {
                if (File.Exists(path) && new FileInfo(path).Length > 32 * 1024 * 1024) throw new InvalidDataException("Plugin store exceeds limit.");
                store = File.Exists(path) ? JsonSerializer.Deserialize<PluginRecordStore>(File.ReadAllBytes(path))
                    ?? throw new InvalidDataException("Empty plugin store.") : new PluginRecordStore();
                store.Validate();
                Save();
            } catch { lease.Dispose(); throw; }
        }
        private void Save()
        {
            using (var output = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            { byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(store); output.Write(bytes, 0, bytes.Length); output.Flush(true); }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH.
                if (!MoveFileEx(path + ".tmp", path, 9)) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            else
            {
                if (File.Exists(path)) File.Replace(path + ".tmp", path, null); else File.Move(path + ".tmp", path);
                SyncDirectory(Path.GetDirectoryName(path));
            }
        }
        private static void CreateDirectory(string path)
        {
            path = Path.GetFullPath(path);
            if (Directory.Exists(path)) return;
            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) CreateDirectory(parent);
            Directory.CreateDirectory(path);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !string.IsNullOrEmpty(parent)) SyncDirectory(parent);
        }
        private static void SyncDirectory(string path)
        {
            int directory = open(path, 0);
            if (directory < 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            try { if (fsync(directory) != 0) throw new Win32Exception(Marshal.GetLastWin32Error()); }
            finally { close(directory); }
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string source, string destination, int flags);
        [DllImport("libc", SetLastError = true)] private static extern int open(string path, int flags);
        [DllImport("libc", SetLastError = true)] private static extern int fsync(int descriptor);
        [DllImport("libc")] private static extern int close(int descriptor);

        public Task<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lock (sync)
            {
                if (failed) return Task.FromResult(PluginResult.Failure(PluginResultCode.Unavailable));
                if (request.Action == "owner") return Task.FromResult(Resolve(request.Target));
                if (request.Fence != null && !Matches(request.Fence)) return Task.FromResult(PluginResult.Failure(PluginResultCode.Fenced));
                var result = store.Execute(request);
                if (request.Action == "cas" && result.Code == PluginResultCode.Success)
                    try { Save(); } catch { failed = true; ContextChanged?.Invoke(); throw; }
                return Task.FromResult(diagnostics.Observe(result));
            }
        }
        private PluginResult Resolve(PluginTarget target)
        {
            if (target == null || !Enum.IsDefined(typeof(PluginTargetKind), target.Kind)) return PluginResult.Failure(PluginResultCode.Invalid);
            if (target.Kind == PluginTargetKind.Node && (target.Node != "standalone" || target.Incarnation != 1))
                return PluginResult.Failure(PluginResultCode.Unsupported);
            return new PluginResult { Code = PluginResultCode.Success, Owner = new OwnerFence {
                StoreId = store.StoreId, Target = target, Node = "standalone", Incarnation = 1, Generation = 1 } };
        }
        private bool Matches(OwnerFence fence) => fence.StoreId == store.StoreId && fence.Node == "standalone"
            && fence.Incarnation == 1 && fence.Generation == 1 && Resolve(fence.Target).Code == PluginResultCode.Success;
        public IDisposable RegisterHandler(string plugin, string topic, Func<PluginMessage, Task<byte[]>> handler) => handlers.Register(plugin, topic, handler);
        public async Task<PluginResult> RequestAsync(string plugin, PluginTarget target, string topic, byte[] payload,
            Guid operationId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (!PluginRecordStore.ValidName(plugin) || !PluginRecordStore.ValidName(topic) || operationId == Guid.Empty
                || (payload?.Length ?? 0) > PluginRecordStore.MaxPayload || timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(30))
                return PluginResult.Failure(PluginResultCode.Invalid);
            var resolved = await ExecuteAsync(new PluginRequest { Action = "owner", Target = target }, cancellationToken);
            if (resolved.Code != PluginResultCode.Success) return resolved;
            DateTime expires = DateTime.UtcNow + timeout;
            var task = handlers.Enqueue(new PluginMessage { Id = Guid.NewGuid(), OperationId = operationId, Plugin = plugin,
                Topic = topic, Payload = payload?.Clone() as byte[], Source = "standalone", Incarnation = 1, Destination = resolved.Owner },
                () => !failed && !cancellationToken.IsCancellationRequested && DateTime.UtcNow < expires);
            if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken)) != task) return PluginResult.Failure(PluginResultCode.Timeout);
            return await task;
        }
        public void Update() { diagnostics.Publish(Context, 0); handlers.Update(); }
        public void Dispose()
        {
            lock (sync) { failed = true; handlers.Dispose(); lease.Dispose(); }
            PluginCluster.Unregister(this); ContextChanged?.Invoke();
        }
    }
}
