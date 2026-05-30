using System;
using VRage.Utils;

namespace PluginSdk.Logging
{
    /// <summary>
    /// Forwards log entries to the game's <c>MyLog.Default</c> — the standalone
    /// Magnetar log destination. Each line is prefixed with the plugin name and
    /// the managed thread id, then logged at the matching
    /// <see cref="MyLogSeverity"/>. A data payload, when present, is appended as
    /// JSON text; an attached exception is appended on a new line. <c>MyLog</c>
    /// adds its own timestamp, OS thread id and severity prefix in front of
    /// this.
    ///
    /// <para>
    /// Safe to call before the game log exists: when <c>MyLog.Default</c> is
    /// null or disabled the call is a no-op.
    /// </para>
    /// </summary>
    public sealed class MagnetarLogSink : ILogSink
    {
        public void Write(in LogEntry entry)
        {
            var log = MyLog.Default;
            if (log is null) return;

            // Pass the rendered line as a format argument (not as the format
            // string) so any '{' or '}' in the message cannot break the
            // string.Format that MyLog.Log runs internally.
            log.Log(ToSeverity(entry.Level), "{0}", Format(in entry));
        }

        /// <summary>
        /// Builds the line body: <c>[plugin] [thread N] message</c>, with the
        /// data payload appended as JSON text when present, and the exception
        /// appended on a following line when present.
        /// </summary>
        public static string Format(in LogEntry entry)
        {
            var line = $"[{entry.PluginName}] [thread {entry.ThreadId}] {entry.Message}";
            if (entry.Data is not null)
                line += " " + LogJson.Serialize(entry.Data);
            if (entry.Exception is not null)
                line += Environment.NewLine + entry.Exception;
            return line;
        }

        private static MyLogSeverity ToSeverity(LogLevel level) => level switch
        {
            LogLevel.Debug => MyLogSeverity.Debug,
            LogLevel.Info => MyLogSeverity.Info,
            LogLevel.Warning => MyLogSeverity.Warning,
            LogLevel.Error => MyLogSeverity.Error,
            LogLevel.Critical => MyLogSeverity.Critical,
            _ => MyLogSeverity.Info,
        };
    }
}
