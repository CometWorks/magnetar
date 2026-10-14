namespace PluginSdk.Commands
{
    /// <summary>
    /// Optional host capability beside <see cref="ICommandRegistrar"/>: runs a
    /// command line through the host's registered commands. Plugins reach it
    /// through <see cref="ServerCommands.Execute"/>.
    /// </summary>
    public interface ICommandExecutor
    {
        /// <summary>Runs <paramref name="commandLine"/> as
        /// <paramref name="caller"/>, sending its replies to
        /// <paramref name="responder"/>. Returns <c>false</c> when the line
        /// names no registered command.</summary>
        bool Execute(string commandLine, in CommandCaller caller, ICommandResponder responder);
    }
}
