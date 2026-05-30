namespace PluginSdk.Logging
{
    /// <summary>
    /// Severity of a log entry. Mirrors VRage's <c>MyLogSeverity</c> one to one
    /// (same members, same order) so <see cref="MagnetarLogSink"/> can forward
    /// to the game log without remapping levels.
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical,
    }
}
