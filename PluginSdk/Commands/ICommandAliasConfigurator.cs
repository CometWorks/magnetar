using System.Collections.Generic;

namespace PluginSdk.Commands
{
    /// <summary>Optional host capability for replacing server-wide administrator command aliases.</summary>
    public interface ICommandAliasConfigurator
    {
        /// <summary>Replaces aliases atomically; see <see cref="CommandDispatcher.SetAliases"/>.</summary>
        void SetAliases(IEnumerable<KeyValuePair<string, string>> aliases);
    }
}
