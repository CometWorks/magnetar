namespace PluginSdk.Logging
{
    /// <summary>
    /// Destination for log entries produced by a <see cref="Logger"/>. Two
    /// implementations ship with the SDK: <see cref="MagnetarLogSink"/>
    /// (forwards to the game's <c>MyLog.Default</c>) and
    /// <see cref="QuasarLogSink"/> (emits one JSON object per entry). The sink
    /// appropriate for the current process is chosen by
    /// <see cref="LogEnvironment.CreateDefaultSink"/>.
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// Writes a single entry. Implementations must be safe to call from
        /// multiple threads concurrently.
        /// </summary>
        void Write(in LogEntry entry);
    }
}
