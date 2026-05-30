using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PluginSdk.Logging
{
    /// <summary>
    /// Renders each log entry as a single-line JSON object for ingestion by the
    /// Quasar Agent. The timestamp is UTC, ISO 8601, with microsecond (6-digit)
    /// precision; the plugin name, managed thread id and severity accompany the
    /// message, an optional <c>data</c> payload and an optional exception.
    ///
    /// <para>Example (one object per line):</para>
    /// <code>
    /// {"timestamp":"2026-05-30T12:34:56.123456Z","level":"Info","plugin":"MyPlugin","thread":12,"message":"Loaded","data":{"count":42}}
    /// </code>
    ///
    /// <para>
    /// Until the Quasar Agent transport is wired up, lines are written to
    /// standard output (which the agent captures from the managed process).
    /// Inject a different line writer through the constructor to redirect.
    /// </para>
    /// </summary>
    /// <summary>
    /// Creates a sink that writes each JSON line via <paramref name="writeLine"/>.
    /// </summary>
    public sealed class QuasarLogSink(Action<string> writeLine) : ILogSink
    {
        // ISO 8601, UTC, microsecond precision. 'T' and 'Z' are quoted as
        // literals; "ffffff" emits exactly six fractional-second digits.
        private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'";

        private readonly Action<string> writeLine = writeLine ?? throw new ArgumentNullException(nameof(writeLine));

        /// <summary>
        /// Creates a sink that writes JSON lines to standard output. This is the
        /// placeholder transport until the Quasar Agent integration lands.
        /// </summary>
        public QuasarLogSink() : this(line => Console.Out.WriteLine(line)) { }

        public void Write(in LogEntry entry) => writeLine(Format(in entry));

        /// <summary>
        /// Formats <paramref name="entry"/> as a compact, single-line JSON
        /// object. Exposed for tests and for callers supplying their own
        /// transport.
        /// </summary>
        public static string Format(in LogEntry entry)
        {
            var record = new LogRecord
            {
                Timestamp = entry.UtcTimestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture),
                Level = entry.Level.ToString(),
                Plugin = entry.PluginName,
                Thread = entry.ThreadId,
                Message = entry.Message,
                Data = entry.Data is null ? null : LogJson.ToElement(entry.Data),
                Exception = entry.Exception?.ToString(),
            };
            return JsonSerializer.Serialize(record, LogJson.Options);
        }

        // Field order here is the JSON property order.
        private sealed class LogRecord
        {
            [JsonPropertyName("timestamp")] public string Timestamp { get; set; }
            [JsonPropertyName("level")] public string Level { get; set; }
            [JsonPropertyName("plugin")] public string Plugin { get; set; }
            [JsonPropertyName("thread")] public int Thread { get; set; }
            [JsonPropertyName("message")] public string Message { get; set; }
            [JsonPropertyName("data")] public JsonElement? Data { get; set; }
            [JsonPropertyName("exception")] public string Exception { get; set; }
        }
    }
}
