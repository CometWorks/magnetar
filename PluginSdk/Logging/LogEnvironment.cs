using System;

namespace PluginSdk.Logging
{
    /// <summary>
    /// Decides which <see cref="ILogSink"/> a <see cref="Logger"/> uses, based
    /// on how the Space Engineers process was launched.
    ///
    /// <para>
    /// When the Quasar Agent launches and manages a server it sets the
    /// <see cref="QuasarEnvironmentVariable"/> environment variable. Its mere
    /// presence (any non-empty value) switches logging to the structured
    /// <see cref="QuasarLogSink"/>; otherwise — standalone Magnetar — logging is
    /// forwarded to the game log via <see cref="MagnetarLogSink"/>.
    /// </para>
    /// </summary>
    public static class LogEnvironment
    {
        /// <summary>
        /// Environment variable the Quasar Agent sets on a managed server
        /// process. Presence (non-empty value) selects the Quasar JSON sink.
        /// </summary>
        public const string QuasarEnvironmentVariable = "QUASAR_AGENT";

        /// <summary>
        /// True when the current process is managed by the Quasar Agent.
        /// </summary>
        public static bool IsManagedByQuasar()
            => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(QuasarEnvironmentVariable));

        /// <summary>
        /// Process-wide tap on the structured Quasar log stream. The in-process
        /// Quasar agent subscribes to this so it can ship plugin log lines to
        /// Quasar over its network channel, which survives Quasar restarts and
        /// agent reconnects to detached server daemons — unlike standard-output
        /// capture, which only works for a child process Quasar itself started.
        /// Each argument is one formatted JSON line, identical to what
        /// <see cref="QuasarLogSink"/> writes to standard output.
        /// </summary>
        public static event Action<string> LineEmitted;

        /// <summary>
        /// Raises <see cref="LineEmitted"/>. Subscriber exceptions are not caught
        /// here — they propagate to the caller (see the body comment for why).
        /// </summary>
        internal static void RaiseLineEmitted(string line)
        {
            // Handler errors must fail explicitly, since there would be no way to log such an exception
            LineEmitted?.Invoke(line);
        }

        /// <summary>
        /// Creates the sink appropriate for the current environment:
        /// <see cref="QuasarLogSink"/> when managed by Quasar, otherwise
        /// <see cref="MagnetarLogSink"/>.
        /// </summary>
        public static ILogSink CreateDefaultSink()
            => IsManagedByQuasar() ? new QuasarLogSink() : (ILogSink)new MagnetarLogSink();
    }
}
