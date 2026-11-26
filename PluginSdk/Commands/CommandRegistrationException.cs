using System;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Thrown by <see cref="CommandRegistry"/> when a module cannot be
    /// registered — for example the reserved <c>help</c> prefix or command, or an
    /// invalid prefix. The host catches it per plugin. When a plugin's whole
    /// assembly is registered, one bad module means none of that assembly's
    /// commands are registered.
    /// </summary>
    public sealed class CommandRegistrationException : Exception
    {
        public CommandRegistrationException(string message)
            : base(message) { }
    }
}
