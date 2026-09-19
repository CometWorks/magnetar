#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace PluginSdk.Clustering
{
    public enum PluginResultCode { Success, NotFound, Conflict, Fenced, Unavailable, Timeout, Unsupported, Capacity, Invalid }
    public enum PluginTargetKind { Node, WorldAuthority, Partition }
    public sealed class RecordVersion
    {
        public Guid StoreId { get; set; }
        public long Revision { get; set; }
    }
    public sealed class PluginRecord
    {
        public RecordVersion Version { get; set; }
        public int SchemaVersion { get; set; }
        public byte[] Payload { get; set; }
        public bool Deleted { get; set; }
    }
    public sealed class NodeContext
    {
        public string ClusterId { get; set; }
        public string Node { get; set; }
        public long Incarnation { get; set; }
        public string Role { get; set; }
        public bool Available { get; set; }
    }
    public sealed class PluginTarget
    {
        public PluginTargetKind Kind { get; set; }
        public string Node { get; set; }
        public long Incarnation { get; set; }
        public ulong Partition { get; set; }
    }
    public sealed class OwnerFence
    {
        public Guid StoreId { get; set; }
        public PluginTarget Target { get; set; }
        public string Node { get; set; }
        public long Incarnation { get; set; }
        public long Generation { get; set; }
    }
    public sealed class PluginResult
    {
        public PluginResultCode Code { get; set; }
        // Remaining ownership lease at validation; receivers subtract elapsed request time.
        public double ValidForMilliseconds { get; set; }
        public PluginRecord Record { get; set; }
        public OwnerFence Owner { get; set; }
        public byte[] Payload { get; set; }
        public static PluginResult Failure(PluginResultCode code) => new PluginResult { Code = code };
    }
    // Protocol 1: shared verbatim with Gateway, independent of game/SDK assemblies.
    public sealed class PluginRequest
    {
        public int Protocol { get; set; } = 1;
        public string Action { get; set; }
        public string Plugin { get; set; }
        public string Node { get; set; }
        public long Incarnation { get; set; }
        public string Key { get; set; }
        public RecordVersion Expected { get; set; }
        public int SchemaVersion { get; set; }
        public byte[] Payload { get; set; }
        public bool Deleted { get; set; }
        public Guid OperationId { get; set; }
        public OwnerFence Fence { get; set; }
        public PluginTarget Target { get; set; }
        public bool SaveFirst { get; set; } = true;
    }
    public sealed class PluginMessage
    {
        public int Protocol { get; set; } = 1;
        public Guid Id { get; set; }
        public Guid OperationId { get; set; }
        public string Plugin { get; set; }
        public string Topic { get; set; }
        public string Source { get; set; }
        public long Incarnation { get; set; }
        public OwnerFence Destination { get; set; }
        public bool Reply { get; set; }
        public PluginResultCode Code { get; set; }
        public byte[] Payload { get; set; }
    }

    /// <summary>Durable state; the owner serializes access, validates authority, and flushes before acknowledgement.</summary>
    public sealed class PluginRecordStore
    {
        public const int MaxPayload = 64 * 1024;
        public Guid StoreId { get; set; } = Guid.NewGuid();
        public long ReplaySequence { get; set; }
        public Dictionary<string, PluginRecord> Records { get; set; } = new Dictionary<string, PluginRecord>();
        public Dictionary<string, PluginReplay> Replays { get; set; } = new Dictionary<string, PluginReplay>();
        public static bool ValidName(string text) => !string.IsNullOrWhiteSpace(text) && text.Length <= 200
            && text.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' || c == '/' || c == ':');

        public void Validate()
        {
            if (StoreId == Guid.Empty || Records == null || Replays == null || Records.Count > 4096 || Replays.Count > 128
                || Records.Values.Concat(Replays.Values.Select(r => r.Record)).Any(r => r == null || r.Version == null
                    || r.Version.Revision <= 0 || r.SchemaVersion <= 0 || (r.Payload?.Length ?? 0) > MaxPayload
                    || r.Deleted && r.Payload != null)) throw new System.IO.InvalidDataException("Invalid durable plugin store.");
        }

        public void Restored() { StoreId = Guid.NewGuid(); Replays.Clear(); }

        private static string Fingerprint(PluginRequest request)
        {
            byte[] fingerprintBytes = JsonSerializer.SerializeToUtf8Bytes(new { request.Key, request.Expected,
                request.SchemaVersion, request.Payload, request.Deleted, request.Fence });
            string fingerprint;
            using (var hash = SHA256.Create()) fingerprint = Convert.ToBase64String(hash.ComputeHash(fingerprintBytes));
            return fingerprint;
        }
        // Replay is an observation of an already durable write, not fresh ownership authority.
        public PluginResult Replay(PluginRequest request)
        {
            if (request?.Action != "cas" || request.Expected?.StoreId != StoreId || request.OperationId == Guid.Empty) return null;
            if (!Replays.TryGetValue(request.Plugin + "\n" + request.OperationId.ToString("N"), out var replay)) return null;
            if (replay.Fingerprint != Fingerprint(request)) return PluginResult.Failure(PluginResultCode.Conflict);
            return new PluginResult { Code = PluginResultCode.Success, Record = new PluginRecord {
                Version = new RecordVersion { StoreId = StoreId, Revision = replay.Record.Version.Revision },
                Payload = replay.Record.Payload?.ToArray(), Deleted = replay.Record.Deleted, SchemaVersion = replay.Record.SchemaVersion } };
        }

        public PluginResult Execute(PluginRequest request)
        {
            if (request == null || request.Protocol != 1 || !ValidName(request.Plugin) || !ValidName(request.Key)
                || (request.Action != "read" && request.Action != "cas")) return PluginResult.Failure(PluginResultCode.Invalid);
            string key = request.Plugin + "\n" + request.Key;
            Records.TryGetValue(key, out var current);
            PluginRecord Copy(PluginRecord record) => new PluginRecord {
                Version = new RecordVersion { StoreId = StoreId, Revision = record?.Version.Revision ?? 0 },
                SchemaVersion = record?.SchemaVersion ?? 0, Payload = record?.Payload?.ToArray(), Deleted = record == null || record.Deleted };
            if (request.Action == "read") return new PluginResult {
                Code = current == null || current.Deleted ? PluginResultCode.NotFound : PluginResultCode.Success, Record = Copy(current) };
            if (request.OperationId == Guid.Empty || request.Expected == null || request.SchemaVersion < 1
                || (request.Payload?.Length ?? 0) > MaxPayload || request.Deleted && request.Payload != null)
                return PluginResult.Failure(PluginResultCode.Invalid);
            if (request.Expected.StoreId != StoreId) return PluginResult.Failure(PluginResultCode.Fenced);
            string replayKey = request.Plugin + "\n" + request.OperationId.ToString("N");
            string fingerprint = Fingerprint(request);
            var replayResult = Replay(request);
            if (replayResult != null) return replayResult;
            if (request.Expected.Revision != (current?.Version.Revision ?? 0))
                return new PluginResult { Code = PluginResultCode.Conflict, Record = Copy(current) };
            // Bound both current data and retained outcomes below Registry's recovery bound.
            while (Replays.Count >= 128 || Replays.Count > 0 && Replays.Values.Sum(r => (long)(r.Record.Payload?.Length ?? 0)) > 1024 * 1024)
                Replays.Remove(Replays.OrderBy(r => r.Value.Sequence).First().Key);
            if (Records.Count >= 4096 && current == null
                || Records.Values.Sum(r => (long)(r.Payload?.Length ?? 0)) + Replays.Values.Sum(r => (long)(r.Record.Payload?.Length ?? 0))
                    + 2L * (request.Payload?.Length ?? 0) > 8 * 1024 * 1024)
                return PluginResult.Failure(PluginResultCode.Capacity);
            var next = new PluginRecord { Version = new RecordVersion { StoreId = StoreId, Revision = checked((current?.Version.Revision ?? 0) + 1) },
                SchemaVersion = request.SchemaVersion, Payload = request.Payload?.ToArray(), Deleted = request.Deleted };
            Records[key] = next;
            Replays[replayKey] = new PluginReplay { Sequence = ++ReplaySequence, Fingerprint = fingerprint, Record = next };
            return new PluginResult { Code = PluginResultCode.Success, Record = Copy(next) };
        }
    }
    public sealed class PluginReplay
    {
        public long Sequence { get; set; }
        public string Fingerprint { get; set; }
        public PluginRecord Record { get; set; }
    }
}
