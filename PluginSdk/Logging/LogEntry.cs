using System;

namespace PluginSdk.Logging
{
    /// <summary>
    /// One immutable log record handed to an <see cref="ILogSink"/>. Carries
    /// everything a sink needs to render a line: the UTC timestamp, severity,
    /// originating plugin, managed thread id, the message, an optional
    /// exception and an optional JSON-serializable data payload.
    /// <see cref="Logger"/> fills this in and passes it by <c>in</c> reference,
    /// so logging a message allocates nothing for the entry itself.
    /// </summary>
    public readonly struct LogEntry(
        DateTime utcTimestamp,
        LogLevel level,
        string pluginName,
        int threadId,
        string message,
        Exception exception,
        object data)
    {
        /// <summary>UTC instant the entry was created (<see cref="DateTime.UtcNow"/>).</summary>
        public DateTime UtcTimestamp { get; } = utcTimestamp;

        /// <summary>Severity of the entry.</summary>
        public LogLevel Level { get; } = level;

        /// <summary>Name of the plugin that emitted the entry.</summary>
        public string PluginName { get; } = pluginName;

        /// <summary>Managed id of the thread that emitted the entry.</summary>
        public int ThreadId { get; } = threadId;

        /// <summary>The log message. Never null (empty when none was supplied).</summary>
        public string Message { get; } = message;

        /// <summary>Optional exception associated with the entry; null when none.</summary>
        public Exception Exception { get; } = exception;

        /// <summary>
        /// Optional JSON-serializable data payload; null when none. Sinks
        /// serialize it through <see cref="LogJson"/>: the Magnetar sink
        /// appends the JSON text to the message, the Quasar sink nests it under
        /// a <c>data</c> field.
        /// </summary>
        public object Data { get; } = data;
    }
}
