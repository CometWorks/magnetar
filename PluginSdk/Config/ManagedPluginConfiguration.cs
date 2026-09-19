using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace PluginSdk.Config
{
    /// <summary>
    /// Loader-owned canonical configuration for managed servers. Ordinary plugins keep using
    /// ConfigStorage. The cluster runtime reads readiness and drift independently of Quasar.
    /// </summary>
    public static class ManagedPluginConfiguration
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Entry> Entries = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<Assembly, string> Owners = new Dictionary<Assembly, string>();
        private static readonly Dictionary<string, object> Instances = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> ExpectedInstances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool configured;
        private static bool initialized;
        private static string failure;
        public static string Revision { get; private set; }
        public static bool Required => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CLUSTER_PLUGIN_CONFIGURATION"));
        public static string Failure { get { lock (Sync) return failure; } }

        /// <summary>Called by the loader before loading plugin assemblies or invoking preload hooks.</summary>
        public static void ConfigureFromEnvironment()
        {
            if (!Required) return;
            lock (Sync)
            {
                if (configured) throw new InvalidOperationException("Managed plugin configuration is already installed.");
                string path = Environment.GetEnvironmentVariable("CLUSTER_PLUGIN_CONFIGURATION");
                if (new FileInfo(path).Length > 16 * 1024 * 1024) throw new InvalidDataException("Plugin configuration exceeds 16 MiB.");
                byte[] bytes = File.ReadAllBytes(path);
                using (var hash = SHA256.Create())
                    if (!Hex(hash.ComputeHash(bytes)).Equals(Environment.GetEnvironmentVariable("CLUSTER_PLUGIN_CONFIGURATION_SHA256"), StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Canonical plugin configuration hash mismatch.");
                using (var document = JsonDocument.Parse(bytes))
                {
                    var root = document.RootElement;
                    if (root.GetProperty("schemaVersion").GetInt32() != 1) throw new InvalidDataException("Unsupported canonical configuration schema.");
                    Revision = root.GetProperty("revision").GetString();
                    if (string.IsNullOrWhiteSpace(Revision)) throw new InvalidDataException("Canonical revision is required.");
                    foreach (var plugin in root.GetProperty("plugins").EnumerateArray())
                    {
                        string id = plugin.GetProperty("id").GetString();
                        string assemblyHash = plugin.GetProperty("assemblySha256").GetString();
                        string type = plugin.TryGetProperty("configType", out var typeElement) ? typeElement.GetString() : null;
                        string json = plugin.TryGetProperty("configuration", out var config) && config.ValueKind != JsonValueKind.Null
                            ? config.GetRawText() : null;
                        if (string.IsNullOrWhiteSpace(id) || assemblyHash?.Length != 64
                            || json != null && string.IsNullOrWhiteSpace(type) || Entries.ContainsKey(id))
                            throw new InvalidDataException("Invalid or duplicate canonical plugin entry.");
                        Entries.Add(id, new Entry(assemblyHash, type, json));
                    }
                }
                configured = true;
            }
        }

        /// <summary>Bind all owners before any plugin constructor, static injection or preload hook runs.</summary>
        public static void BindOwner(string id, Assembly assembly)
        {
            if (!Required) return;
            lock (Sync)
            {
                RequireConfigured();
                if (!Entries.TryGetValue(id, out var entry)) throw new InvalidDataException("Unexpected managed plugin: " + id);
                using (var file = File.OpenRead(assembly.Location))
                using (var hash = SHA256.Create())
                    if (!Hex(hash.ComputeHash(file)).Equals(entry.AssemblyHash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Managed plugin assembly mismatch: " + id);
                if (Owners.TryGetValue(assembly, out var owner) && !owner.Equals(id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Ambiguous plugin configuration ownership.");
                Owners[assembly] = id;
            }
        }

        internal static string Resolve(Type type)
        {
            if (!Required) return null;
            lock (Sync)
            {
                RequireConfigured();
                if (!Owners.TryGetValue(type.Assembly, out var id))
                    throw new InvalidOperationException("Configuration type has no managed plugin owner: " + type.FullName);
                var entry = Entries[id];
                if (entry.Json == null || entry.Type != type.FullName)
                    throw new InvalidOperationException("No canonical configuration for " + id + ":" + type.FullName);
                return entry.Json;
            }
        }

        internal static void ValidateEnvelope(Type type, string actual)
        {
            string expected = Resolve(type);
            if (expected != null && !Equivalent(expected, actual))
                throw new InvalidDataException("Canonical schema, defaults or values differ for " + type.FullName);
        }

        /// <summary>Declare a runtime plugin instance before construction; preloader-only assemblies need none.</summary>
        public static void ExpectInstance(string id)
        {
            if (!Required) return;
            lock (Sync) { RequireConfigured(); ExpectedInstances.Add(id); }
        }

        /// <summary>Record initialized instances; validation includes values read back from the live plugin.</summary>
        public static void Initialized(string id, object instance)
        {
            if (!Required) return;
            lock (Sync)
            {
                RequireConfigured();
                if (!Entries.ContainsKey(id)) throw new InvalidDataException("Unexpected initialized plugin: " + id);
                Instances[id] = instance;
                ValidateInstance(id, instance);
            }
        }

        /// <summary>Called once after all plugin initialization, including failed/disabled-plugin handling.</summary>
        public static void CompleteInitialization()
        {
            if (!Required) return;
            lock (Sync)
            {
                RequireConfigured();
                if (Entries.Keys.Any(id => !Owners.Values.Contains(id, StringComparer.OrdinalIgnoreCase))
                    || ExpectedInstances.Any(id => !Instances.ContainsKey(id))
                    || Entries.Any(pair => pair.Value.Json != null && !Instances.ContainsKey(pair.Key)))
                    throw new InvalidDataException("Managed plugin inventory is incomplete.");
                initialized = true;
            }
        }

        /// <summary>Recheck live configuration, including collections mutated without notifications.
        /// Drift is sticky for this process; re-admission requires a new verified launch.</summary>
        public static bool Observe()
        {
            if (!Required) return true;
            lock (Sync)
            {
                if (!configured || !initialized || failure != null) return false;
                try
                {
                    foreach (var pair in Instances) ValidateInstance(pair.Key, pair.Value);
                    return true;
                }
                catch (Exception error) { failure = error.GetBaseException().Message; return false; }
            }
        }

        private static void ValidateInstance(string id, object instance)
        {
            var entry = Entries[id];
            var property = instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && typeof(PluginConfig).IsAssignableFrom(p.PropertyType))
                .OrderBy(p => p.Name == "PluginConfig" ? 0 : 1).ThenBy(p => p.Name, StringComparer.Ordinal).FirstOrDefault();
            if (entry.Json == null)
            {
                if (property != null) throw new InvalidDataException("Canonical configuration missing for " + id);
                return;
            }
            var value = property?.GetValue(instance) as PluginConfig;
            if (value == null || value.GetType().FullName != entry.Type)
                throw new InvalidDataException("Canonical configuration type mismatch for " + id);
            string json = (string)typeof(ConfigStorage).GetMethod(nameof(ConfigStorage.SaveJson))
                .MakeGenericMethod(value.GetType()).Invoke(null, new object[] { value });
            ValidateEnvelope(value.GetType(), json);
        }

        private static void RequireConfigured()
        {
            if (!configured) throw new InvalidOperationException("Managed plugin configuration is not installed.");
        }

        internal static bool Equivalent(string left, string right)
        {
            using (var a = JsonDocument.Parse(left))
            using (var b = JsonDocument.Parse(right)) return Equal(a.RootElement, b.RootElement);
        }

        private static bool Equal(JsonElement a, JsonElement b)
        {
            if (a.ValueKind != b.ValueKind) return false;
            switch (a.ValueKind)
            {
                case JsonValueKind.Object:
                    var ap = a.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
                    var bp = b.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).ToArray();
                    return ap.Length == bp.Length && ap.Select((p, i) => p.Name == bp[i].Name && Equal(p.Value, bp[i].Value)).All(v => v);
                case JsonValueKind.Array:
                    return a.GetArrayLength() == b.GetArrayLength() && a.EnumerateArray().Select((e, i) => Equal(e, b[i])).All(v => v);
                case JsonValueKind.String: return a.GetString() == b.GetString();
                case JsonValueKind.Number: return a.GetRawText() == b.GetRawText();
                default: return true;
            }
        }

        private static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        private sealed class Entry
        {
            public readonly string AssemblyHash, Type, Json;
            public Entry(string assemblyHash, string type, string json) { AssemblyHash = assemblyHash; Type = type; Json = json; }
        }
    }
}
