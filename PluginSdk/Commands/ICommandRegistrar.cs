using System;
using System.Reflection;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Host-provided sink that registers a plugin's command modules. A plugin
    /// does not implement or hold this directly; it calls the static
    /// <see cref="ServerCommands"/> facade, which the host backs with a single
    /// long-lived instance. Ownership is attributed to the assembly the plugin
    /// passes in, so registration may happen at any time, not only during
    /// <c>Init()</c>.
    /// </summary>
    public interface ICommandRegistrar
    {
        /// <summary>Registers every <see cref="CommandModule"/> in
        /// <paramref name="assembly"/> that carries a
        /// <see cref="CommandRootAttribute"/>, owned by that assembly. If any module is
        /// invalid, none of the assembly's commands are registered.</summary>
        void Register(Assembly assembly);

        /// <summary>Registers the given <see cref="CommandModule"/> types
        /// explicitly (skipping the assembly scan), owned by
        /// <paramref name="assembly"/>. Each type is registered on its own.</summary>
        void Register(Assembly assembly, params Type[] moduleTypes);
    }
}
