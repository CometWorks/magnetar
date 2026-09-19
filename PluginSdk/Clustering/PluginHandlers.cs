#nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PluginSdk.Clustering
{
    /// <summary>Bounded game-thread dispatch shared by standalone and cluster providers.</summary>
    public sealed class PluginHandlers : IDisposable
    {
        private readonly object sync = new object();
        private readonly Dictionary<string, Func<PluginMessage, Task<byte[]>>> handlers = new Dictionary<string, Func<PluginMessage, Task<byte[]>>>();
        private readonly Queue<Action> pending = new Queue<Action>();
        private bool disposed;
        private int inFlight;
        public IDisposable Register(string plugin, string topic, Func<PluginMessage, Task<byte[]>> handler)
        {
            if (!PluginRecordStore.ValidName(plugin) || !PluginRecordStore.ValidName(topic) || handler == null) throw new ArgumentException("Invalid handler.");
            string key = plugin + "\n" + topic;
            lock (sync) { if (disposed) throw new ObjectDisposedException(nameof(PluginHandlers)); handlers.Add(key, handler); }
            return new Registration(() => { lock (sync) handlers.Remove(key); });
        }
        public Task<PluginResult> Enqueue(PluginMessage message, Func<bool> stillValid)
        {
            var completion = new TaskCompletionSource<PluginResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (sync)
            {
                if (disposed || inFlight >= 128) return Task.FromResult(PluginResult.Failure(disposed ? PluginResultCode.Unavailable : PluginResultCode.Capacity));
                inFlight++;
                pending.Enqueue(async () =>
                {
                    try
                    {
                        Func<PluginMessage, Task<byte[]>> handler;
                        lock (sync) handlers.TryGetValue(message.Plugin + "\n" + message.Topic, out handler);
                        if (disposed || !stillValid()) completion.TrySetResult(PluginResult.Failure(PluginResultCode.Fenced));
                        else if (handler == null) completion.TrySetResult(PluginResult.Failure(PluginResultCode.Unsupported));
                        else
                        {
                            byte[] result = await handler(message);
                            completion.TrySetResult((result?.Length ?? 0) <= PluginRecordStore.MaxPayload
                                ? new PluginResult { Code = PluginResultCode.Success, Payload = result }
                                : PluginResult.Failure(PluginResultCode.Capacity));
                        }
                    }
                    catch { completion.TrySetResult(PluginResult.Failure(PluginResultCode.Unavailable)); }
                    finally { lock (sync) inFlight--; }
                });
            }
            return completion.Task;
        }
        public void Update()
        {
            // Limit per-frame work; async continuations belong to the plugin and are not game-thread guaranteed.
            for (int i = 0; i < 8; i++)
            {
                Action action;
                lock (sync) { if (pending.Count == 0) break; action = pending.Dequeue(); }
                action();
            }
        }
        public void Dispose()
        {
            lock (sync) { disposed = true; handlers.Clear(); }
            while (true) { Action action; lock (sync) { if (pending.Count == 0) break; action = pending.Dequeue(); } action(); }
        }
        private sealed class Registration : IDisposable
        {
            private Action remove;
            public Registration(Action action) { remove = action; }
            public void Dispose() { System.Threading.Interlocked.Exchange(ref remove, null)?.Invoke(); }
        }
    }
}
