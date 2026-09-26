#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PluginSdk.Clustering
{
    /// <summary>The outcome of a global command (T-0256).</summary>
    public sealed class PluginGlobalCommandResult
    {
        /// <summary>
        /// Success: the command ran once (now or earlier) and returned <see cref="Payload"/>. Invalid: it ran and
        /// threw (<see cref="Error"/>); a retry replays that outcome. Conflict: the operation id was used before
        /// with a different payload. Anything else (Unavailable, Timeout, Unsupported = no handler on the World
        /// Authority, Capacity, Fenced): the command did not reach a definitive outcome - retry with the same id.
        /// </summary>
        public PluginResultCode Code { get; set; }
        public byte[] Payload { get; set; }
        public string Error { get; set; }
    }

    /// <summary>
    /// Provider capability: the ledger that makes a global command run once per operation id. In a cluster it
    /// is the World Authority's ledger; the handler side calls it, so it runs where the command runs.
    /// </summary>
    public interface IPluginGlobalCommandLedger
    {
        PluginGlobalCommandResult Execute(string plugin, string command, Guid operationId, byte[] payload, Func<byte[], byte[]> handler);
    }

    internal static class PluginGlobalCommands
    {
        public const string TopicPrefix = "global/";
        /// <summary>A command's result is an acknowledgement: the ledger keeps thousands of them (in a cluster, inside the
        /// World Authority's checkpoint), so a larger result fails the command. Bulk data belongs in records.</summary>
        public const int MaxResultBytes = 4096;

        public static Func<byte[], byte[]> Bounded(Func<byte[], byte[]> handler) => payload =>
        {
            byte[] result = handler(payload);
            if ((result?.Length ?? 0) > MaxResultBytes)
                throw new InvalidOperationException("Global command result is " + result.Length + " bytes; the limit is " + MaxResultBytes + ".");
            return result;
        };
        private const byte Ok = 0, Failed = 1, Conflicted = 2;

        public static string Topic(string command) => TopicPrefix + command;

        /// <summary>A handler reply: one status byte, then the result payload or the UTF-8 error.</summary>
        public static byte[] Encode(PluginGlobalCommandResult result)
        {
            byte status = result.Code == PluginResultCode.Success ? Ok : result.Code == PluginResultCode.Conflict ? Conflicted : Failed;
            byte[] body = status == Ok ? result.Payload ?? Array.Empty<byte>() : Encoding.UTF8.GetBytes(result.Error ?? string.Empty);
            var reply = new byte[body.Length + 1];
            reply[0] = status;
            Buffer.BlockCopy(body, 0, reply, 1, body.Length);
            return reply;
        }

        public static PluginGlobalCommandResult Decode(PluginResult reply)
        {
            if (reply.Code != PluginResultCode.Success) return new PluginGlobalCommandResult { Code = reply.Code };
            byte[] bytes = reply.Payload;
            if (bytes == null || bytes.Length == 0) return new PluginGlobalCommandResult { Code = PluginResultCode.Unavailable };
            byte[] body = bytes.Skip(1).ToArray();
            switch (bytes[0])
            {
                case Ok: return new PluginGlobalCommandResult { Code = PluginResultCode.Success, Payload = body };
                case Conflicted: return new PluginGlobalCommandResult { Code = PluginResultCode.Conflict, Error = Encoding.UTF8.GetString(body) };
                default: return new PluginGlobalCommandResult { Code = PluginResultCode.Invalid, Error = Encoding.UTF8.GetString(body) };
            }
        }

        public static string Fingerprint(byte[] payload)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(payload ?? Array.Empty<byte>())).Replace("-", "");
        }
    }

    /// <summary>The plain-server ledger: this process is the World Authority. In memory, the 4096 latest operations.</summary>
    internal sealed class PluginLocalGlobalLedger : IPluginGlobalCommandLedger
    {
        public static readonly PluginLocalGlobalLedger Instance = new PluginLocalGlobalLedger();
        private const int MaxEntries = 4096;
        private readonly object sync = new object();
        private readonly Dictionary<string, (long Order, string Fingerprint, PluginGlobalCommandResult Result)> results =
            new Dictionary<string, (long, string, PluginGlobalCommandResult)>();
        private long order;

        public PluginGlobalCommandResult Execute(string plugin, string command, Guid operationId, byte[] payload, Func<byte[], byte[]> handler)
        {
            string key = plugin + "\n" + command + "\n" + operationId.ToString("N");
            string fingerprint = PluginGlobalCommands.Fingerprint(payload);
            lock (sync)
                if (results.TryGetValue(key, out var stored))
                    return stored.Fingerprint == fingerprint ? stored.Result
                        : new PluginGlobalCommandResult { Code = PluginResultCode.Conflict, Error = "Operation ID was reused with a different payload" };
            PluginGlobalCommandResult result;
            try { result = new PluginGlobalCommandResult { Code = PluginResultCode.Success, Payload = handler(payload) }; }
            catch (Exception error) { result = new PluginGlobalCommandResult { Code = PluginResultCode.Invalid, Error = error.Message }; }
            lock (sync)
            {
                results[key] = (++order, fingerprint, result);
                while (results.Count > MaxEntries) results.Remove(results.OrderBy(item => item.Value.Order).First().Key);
            }
            return result;
        }
    }
}
