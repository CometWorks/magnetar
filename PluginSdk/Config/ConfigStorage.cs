using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace PluginSdk.Config
{
    /// <summary>
    /// Save and load <see cref="PluginConfig"/>-derived instances.
    ///
    /// <para><b>XML</b> — local on-disk format. Atomic write via temp file.
    /// Only properties whose value differs from the default are written
    /// (driven by <see cref="PluginConfig"/>'s
    /// <see cref="System.Xml.Serialization.IXmlSerializable"/> impl).
    /// Missing properties stay at their defaults when loaded back.</para>
    ///
    /// <para><b>JSON</b> — remote management wire format. The document is a
    /// three-part envelope:</para>
    /// <code>
    /// {
    ///   "schema":   { layout, properties, structs, enums },
    ///   "defaults": { ... all options at default values ... },
    ///   "values":   { ... all options at current values  ... }
    /// }
    /// </code>
    /// <para>
    /// Loading reads only the <c>values</c> section; the schema and defaults
    /// are regenerated on every save. For backward compatibility a flat
    /// values-only document is also accepted.
    /// </para>
    /// </summary>
    public static class ConfigStorage
    {
        private static readonly JsonSerializerOptions JsonOptions = BuildJsonOptions();

        private static JsonSerializerOptions BuildJsonOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = null,
                IncludeFields = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
            // Persist enum config values by member name, never as integers, so
            // renumbering an enum does not silently change stored values. Also
            // covers VRageMath.Base6Directions.Direction.
            options.Converters.Add(new JsonStringEnumConverter());
            // VRage value types — Color (hex string), Vector2D/3D, Vector2I/3I
            // and MyPositionAndOrientation — need bespoke JSON shapes.
            foreach (var converter in TypeSerialization.JsonConverters)
                options.Converters.Add(converter);
            return options;
        }

        // -------- XML ---------------------------------------------------

        /// <summary>
        /// Writes <paramref name="config"/> to <paramref name="path"/> as XML
        /// via a temporary file + rename, so a crash mid-write cannot leave
        /// a truncated config behind. Only non-default values are emitted.
        /// </summary>
        public static void SaveXml<T>(T config, string path) where T : PluginConfig
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (path == null) throw new ArgumentNullException(nameof(path));

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var serializer = new XmlSerializer(typeof(T));
            var tmp = path + ".tmp";
            using (var fs = File.Create(tmp))
                serializer.Serialize(fs, config);

            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        /// <summary>
        /// Reads a <typeparamref name="T"/> from <paramref name="path"/>. If
        /// the file does not exist a default-constructed instance is returned.
        /// Missing elements in the file leave the corresponding property at
        /// its default value.
        /// </summary>
        public static T LoadXml<T>(string path) where T : PluginConfig, new()
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) return new T();

            var serializer = new XmlSerializer(typeof(T));
            using (var fs = File.OpenRead(path))
                return (T)serializer.Deserialize(fs);
        }

        // -------- JSON --------------------------------------------------

        /// <summary>
        /// Serialises <paramref name="config"/> to a JSON document carrying
        /// the layout schema, the default values and the current values.
        /// Every option is present in the <c>values</c> section even when
        /// its current value equals the default.
        /// </summary>
        public static string SaveJson<T>(T config) where T : PluginConfig
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            var type = typeof(T);
            var defaults = (T)Activator.CreateInstance(type);
            var schema = ConfigSchema.Build(type);

            var schemaEl = JsonSerializer.SerializeToElement(schema, JsonOptions);
            var defaultsEl = JsonSerializer.SerializeToElement(defaults, type, JsonOptions);
            var valuesEl = JsonSerializer.SerializeToElement(config, type, JsonOptions);

            var envelope = new ConfigEnvelope
            {
                Schema = schemaEl,
                Defaults = defaultsEl,
                Values = valuesEl,
            };

            return JsonSerializer.Serialize(envelope, JsonOptions);
        }

        /// <summary>
        /// Deserialises a <typeparamref name="T"/> from a JSON document.
        /// Reads only the <c>values</c> section; <c>schema</c> and
        /// <c>defaults</c> (if present) are ignored. A flat values-only
        /// document is also accepted as a fallback.
        /// </summary>
        public static T LoadJson<T>(string json) where T : PluginConfig, new()
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            using (var doc = JsonDocument.Parse(json))
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("values", out var values))
                {
                    return JsonSerializer.Deserialize<T>(values.GetRawText(), JsonOptions);
                }
            }

            // Fallback: treat the whole document as a flat values dump.
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }

        private sealed class ConfigEnvelope
        {
            public JsonElement Schema { get; set; }
            public JsonElement Defaults { get; set; }
            public JsonElement Values { get; set; }
        }
    }
}
