using System;

namespace Magnetar.ConfigTerminal.Logs;

/// <summary>How a log line should be highlighted in the viewer, if at all.</summary>
internal enum LogHighlightKind
{
    None,
    Ready,
    Exception,
}

/// <summary>
/// Classifies a log line for colour highlighting in <see cref="LogViewerView"/>.
/// Two markers matter to an operator scanning a log: the DS "Game ready" line
/// (the same readiness marker <see cref="ReadinessDetector"/> watches for) that
/// says a world finished loading, and any "Exception" line that flags a fault.
/// </summary>
internal static class LogHighlight
{
    /// <summary>The DS prints "Game ready..." once a world is loaded and joinable.</summary>
    public const string ReadyMarker = "Game ready";

    /// <summary>.NET fault lines and stack-trace headers carry the type suffix "Exception".</summary>
    public const string ExceptionMarker = "Exception";

    /// <summary>
    /// Returns the highlight for a line. Exception wins over readiness when both
    /// somehow appear, since a fault is the more urgent thing to surface.
    /// Case-sensitive: both markers are printed capitalised, so matching the exact
    /// casing avoids colouring incidental prose like a lowercase "exception".
    /// </summary>
    public static LogHighlightKind Classify(string line)
    {
        if (string.IsNullOrEmpty(line))
            return LogHighlightKind.None;
        if (line.IndexOf(ExceptionMarker, StringComparison.Ordinal) >= 0)
            return LogHighlightKind.Exception;
        if (line.IndexOf(ReadyMarker, StringComparison.Ordinal) >= 0)
            return LogHighlightKind.Ready;
        return LogHighlightKind.None;
    }
}
