using System;

namespace PluginSdk.Logging
{
    /// <summary>
    /// Unified logger a plugin uses regardless of whether it runs on standalone
    /// Magnetar or is managed by Quasar. The logger captures the plugin name
    /// once, stamps every entry with the UTC time and the calling thread's
    /// managed id, and hands it to an <see cref="ILogSink"/> that renders it for
    /// the active environment.
    ///
    /// <para>
    /// Obtain one with <see cref="Create"/>, which auto-selects the sink via
    /// <see cref="LogEnvironment.CreateDefaultSink"/>:
    /// </para>
    /// <code>
    /// private static readonly Logger Log = Logger.Create("MyPlugin");
    /// ...
    /// Log.Info("Loaded 42 definitions");
    /// Log.Error("Failed to patch method", exception);
    /// </code>
    ///
    /// <para>
    /// Every method has an overload taking a JSON-serializable
    /// <c>data</c> payload. The standalone sink appends the serialized JSON to
    /// the log line; the Quasar sink nests it under a <c>data</c> field:
    /// </para>
    /// <code>
    /// Log.Info("Mods downloaded", new { count = 12, totalBytes = 8_421_344 });
    /// </code>
    ///
    /// <para>
    /// Pass an explicit sink to the constructor to redirect output — e.g. in
    /// tests, or when wiring the Quasar Agent transport later.
    /// </para>
    /// </summary>
    public sealed class Logger(string pluginName, ILogSink sink)
    {
        private readonly string pluginName = pluginName ?? throw new ArgumentNullException(nameof(pluginName));
        private readonly ILogSink sink = sink ?? throw new ArgumentNullException(nameof(sink));

        /// <summary>
        /// Creates a logger for <paramref name="pluginName"/> using the sink
        /// appropriate for the current environment (see
        /// <see cref="LogEnvironment"/>).
        /// </summary>
        public static Logger Create(string pluginName)
            => new(pluginName, LogEnvironment.CreateDefaultSink());

        /// <summary>Name of the plugin this logger stamps onto every entry.</summary>
        public string PluginName => pluginName;

        public void Debug(string message, object data = null) => Write(LogLevel.Debug, message, null, data);
        public void Info(string message, object data = null) => Write(LogLevel.Info, message, null, data);
        public void Warning(string message, object data = null) => Write(LogLevel.Warning, message, null, data);
        public void Error(string message, object data = null) => Write(LogLevel.Error, message, null, data);
        public void Error(string message, Exception exception, object data = null) => Write(LogLevel.Error, message, exception, data);
        public void Critical(string message, object data = null) => Write(LogLevel.Critical, message, null, data);
        public void Critical(string message, Exception exception, object data = null) => Write(LogLevel.Critical, message, exception, data);

        /// <summary>
        /// Writes an entry at <paramref name="level"/> with an optional
        /// <paramref name="exception"/> and optional JSON-serializable
        /// <paramref name="data"/> payload. The convenience methods
        /// (<see cref="Info"/>, <see cref="Error(string, object)"/>, ...)
        /// delegate here.
        /// </summary>
        public void Log(LogLevel level, string message, Exception exception = null, object data = null)
            => Write(level, message, exception, data);

        private void Write(LogLevel level, string message, Exception exception, object data)
        {
            var entry = new LogEntry(
                DateTime.UtcNow,
                level,
                pluginName,
                Environment.CurrentManagedThreadId,
                message ?? string.Empty,
                exception,
                data);
            sink.Write(in entry);
        }
    }
}
