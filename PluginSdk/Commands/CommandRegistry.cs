using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game.ModAPI;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Holds every registered command, grouped by <c>!prefix</c> root. Build it
    /// once, register each plugin's module types or assemblies, then hand it to
    /// a <see cref="CommandDispatcher"/>.
    /// </summary>
    public sealed class CommandRegistry
    {
        private const MyPromoteLevel DefaultPermission = MyPromoteLevel.Admin;

        private readonly Dictionary<string, CommandRoot> roots =
            new Dictionary<string, CommandRoot>(StringComparer.OrdinalIgnoreCase);

        /// <summary>The prefixes currently registered.</summary>
        public IReadOnlyCollection<string> Prefixes => roots.Keys;

        /// <summary>
        /// Registers every <see cref="CommandModule"/> in
        /// <paramref name="assembly"/> that carries a
        /// <see cref="CommandRootAttribute"/>, attributing them to that
        /// assembly. Returns the number of modules registered. Commands that
        /// collide with ones already registered are overwritten (last
        /// registration wins).
        /// </summary>
        public int RegisterAssembly(Assembly assembly)
        {
            string ownerId = assembly.GetName().Name;
            int count = 0;
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(CommandModule).IsAssignableFrom(type))
                    continue;
                if (!type.IsDefined(typeof(CommandRootAttribute), false))
                    continue;
                RegisterModule(type, ownerId);
                count++;
            }
            return count;
        }

        /// <summary>
        /// Registers a single <see cref="CommandModule"/> type. The type must
        /// carry a <see cref="CommandRootAttribute"/>.
        /// </summary>
        public void RegisterModule(Type moduleType, string ownerId)
        {
            if (!typeof(CommandModule).IsAssignableFrom(moduleType))
                throw new CommandRegistrationException($"{moduleType.Name} is not a CommandModule");

            var rootAttr = moduleType.GetCustomAttribute<CommandRootAttribute>(false);
            if (rootAttr == null)
                throw new CommandRegistrationException($"{moduleType.Name} has no [CommandRoot]");

            string prefix = rootAttr.Prefix?.Trim();
            if (string.IsNullOrEmpty(prefix) || prefix.IndexOf(' ') >= 0)
                throw new CommandRegistrationException(
                    $"{moduleType.Name} has an invalid command prefix '{rootAttr.Prefix}'");

            CommandRoot root = GetOrCreateRoot(prefix, rootAttr, ownerId);

            foreach (MethodInfo method in moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var cmdAttr = method.GetCustomAttribute<CommandAttribute>(false);
                if (cmdAttr == null)
                    continue;

                // An empty path denotes the root-level (default) command, run
                // for a bare '!prefix'. Non-empty paths must not start with the
                // reserved 'help' word.
                List<string> path = SplitPath(cmdAttr.Command);
                if (path.Count > 0 && string.Equals(path[0], "help", StringComparison.OrdinalIgnoreCase))
                    throw new CommandRegistrationException(
                        $"'!{prefix} help' is reserved and cannot be defined by {moduleType.Name}.{method.Name}");

                var permAttr = method.GetCustomAttribute<PermissionAttribute>(false);
                MyPromoteLevel level = permAttr?.Level ?? DefaultPermission;

                root.Add(new RegisteredCommand(ownerId, prefix, path,
                    cmdAttr.Description, cmdAttr.HelpText, level, moduleType, method));
            }
        }

        internal bool TryGetRoot(string prefix, out CommandRoot root)
            => roots.TryGetValue(prefix, out root);

        private CommandRoot GetOrCreateRoot(string prefix, CommandRootAttribute attr, string ownerId)
        {
            if (roots.TryGetValue(prefix, out CommandRoot existing))
                return existing;

            var root = new CommandRoot(prefix, attr.Title ?? prefix, attr.Description, ownerId);
            roots[prefix] = root;
            return root;
        }

        private static List<string> SplitPath(string command)
        {
            var path = new List<string>();
            if (string.IsNullOrEmpty(command))
                return path;
            foreach (string part in command.Split(' '))
            {
                if (part.Length > 0)
                    path.Add(part);
            }
            return path;
        }
    }
}
