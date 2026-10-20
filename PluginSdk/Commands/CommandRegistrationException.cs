using System;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Thrown by <see cref="CommandRegistry"/> when a module cannot be
    /// registered — for example a prefix already owned by a different plugin or
    /// a duplicate command path. The host catches it per plugin so one bad
    /// module does not abort the others.
    /// </summary>
    public sealed class CommandRegistrationException : Exception
    {
        public CommandRegistrationException(string message)
            : base(message) { }
    }
}
