using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PluginSdk.Logging
{
    /// <summary>
    /// Shared System.Text.Json configuration and helpers for rendering log
    /// payloads. Both sinks serialize the optional structured data object
    /// through here, so the JSON shape is identical in either logging mode.
    ///
    /// <para>
    /// Serialisation never throws: a payload that cannot be serialized (for
    /// example a type with a cyclic graph) is replaced by a small error object
    /// carrying the failure message and the payload's type, so a bad payload
    /// can never take down logging.
    /// </para>
    /// </summary>
    internal static class LogJson
    {
        internal static readonly JsonSerializerOptions Options = new()
        {
            // Omit null properties (e.g. an absent "data" or "exception").
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Keep messages and data readable instead of escaping '+', '<', '&'.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = false,
        };

        /// <summary>Serializes <paramref name="data"/> to compact JSON text.</summary>
        internal static string Serialize(object data)
        {
            try { return JsonSerializer.Serialize(data, Options); }
            catch (Exception ex) { return JsonSerializer.Serialize(Error(data, ex), Options); }
        }

        /// <summary>
        /// Serializes <paramref name="data"/> to a JSON element, so it can be
        /// nested as a value inside another JSON object.
        /// </summary>
        internal static JsonElement ToElement(object data)
        {
            try { return JsonSerializer.SerializeToElement(data, Options); }
            catch (Exception ex) { return JsonSerializer.SerializeToElement(Error(data, ex), Options); }
        }

        private static object Error(object data, Exception ex)
            => new { error = ex.Message, type = data?.GetType().FullName };
    }
}
