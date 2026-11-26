using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRage.Game.ModAPI;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Holds every registered command, grouped by <c>!prefix</c> root. Build it
    /// once, register each plugin's module types or assemblies, then hand it to
    /// a <see cref="CommandDispatcher"/>.
    /// <para>
    /// Safe to use from any thread: registration may happen at any time, from
    /// any thread, while commands run. One lock guards the roots and every
    /// tree under them, and is held only for short in-memory edits and reads.
    /// No handler, responder or other plugin code runs under it, and a type is
    /// reflected over before it is taken. A call that registers an assembly or
    /// one module adds all of its commands or none.
    /// </para>
    /// </summary>
    public sealed class CommandRegistry
    {
        private const MyPromoteLevel DefaultPermission = MyPromoteLevel.Admin;

        // The one lock. Every root is handed this same object, so it guards the
        // root map and every command tree below it.
        private readonly object sync = new object();

        // Prefix -> root, case-insensitive. Guarded by sync.
        private readonly Dictionary<string, CommandRoot> roots = new Dictionary<
            string,
            CommandRoot
        >(StringComparer.OrdinalIgnoreCase);

        /// <summary>The prefixes registered when this was called.</summary>
        public IReadOnlyCollection<string> Prefixes
        {
            get
            {
                lock (sync)
                    return roots.Keys.ToArray();
            }
        }

        /// <summary>
        /// Registers every <see cref="CommandModule"/> in
        /// <paramref name="assembly"/> that carries a
        /// <see cref="CommandRootAttribute"/>, attributing them to that
        /// assembly. Returns the number of modules registered. Commands that
        /// collide with ones already registered are overridden (last
        /// registration wins). If any module is invalid, nothing from the
        /// assembly is registered.
        /// </summary>
        public int RegisterAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            string ownerId = assembly.GetName().Name;

            // Validate and reflect over every module first, outside the lock. If any
            // one throws, nothing has been registered yet, so the assembly adds all
            // of its commands or none.
            var modules = new List<PendingModule>();
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(CommandModule).IsAssignableFrom(type))
                    continue;
                if (!type.IsDefined(typeof(CommandRootAttribute), false))
                    continue;
                modules.Add(Build(type, ownerId, assembly));
            }

            // Then add them all in one hold of the lock, so a reader never sees half
            // an assembly.
            lock (sync)
            {
                foreach (PendingModule module in modules)
                    Apply(module);
            }
            return modules.Count;
        }

        /// <summary>
        /// Registers a single <see cref="CommandModule"/> type. The type must
        /// carry a <see cref="CommandRootAttribute"/>. The owner is identified
        /// by <paramref name="ownerId"/> itself, compared as an ordinal string; a
        /// null <paramref name="ownerId"/> is the same owner as the empty string.
        /// </summary>
        public void RegisterModule(Type moduleType, string ownerId) =>
            Add(Build(moduleType, ownerId, ownerId ?? string.Empty));

        /// <summary>
        /// Registers a single module type for <paramref name="assembly"/>. The
        /// owner is the assembly itself, not its name, so two plugins that share
        /// an assembly name stay separate.
        /// </summary>
        internal void RegisterModule(Type moduleType, Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            Add(Build(moduleType, assembly.GetName().Name, assembly));
        }

        /// <summary>
        /// Removes every command <paramref name="assembly"/> registered, and
        /// brings back any command it had overridden. Returns the number of
        /// commands removed.
        /// </summary>
        internal int Unregister(Assembly assembly) => RemoveOwner(assembly);

        /// <summary>
        /// Removes every command registered under <paramref name="ownerId"/>
        /// through <see cref="RegisterModule(Type, string)"/>. Returns the number
        /// of commands removed.
        /// </summary>
        internal int Unregister(string ownerId) => RemoveOwner(ownerId ?? string.Empty);

        /// <summary>
        /// Finds the root for <paramref name="prefix"/>. The root stays usable if it
        /// is removed afterwards; it then simply has nothing registered.
        /// </summary>
        internal bool TryGetRoot(string prefix, out CommandRoot root)
        {
            lock (sync)
                return roots.TryGetValue(prefix, out root);
        }

        /// <summary>Every registered root when called, used to build the global <c>!help</c> listing.</summary>
        internal IEnumerable<CommandRoot> EnumerateRoots()
        {
            lock (sync)
                return roots.Values.ToArray();
        }

        /// <summary>Removes everything <paramref name="ownerKey"/> registered, in every root,
        /// and returns how many commands that was.</summary>
        private int RemoveOwner(object ownerKey)
        {
            lock (sync)
            {
                int removed = 0;
                List<string> emptied = null;
                foreach (KeyValuePair<string, CommandRoot> pair in roots)
                {
                    removed += pair.Value.RemoveOwner(ownerKey, out bool touched);

                    // Only a root this call changed can have become empty, so a
                    // root registered with a heading and no commands stays.
                    if (touched && pair.Value.IsEmpty)
                        (emptied ?? (emptied = new List<string>())).Add(pair.Key);
                }

                // Removed after the loop: the map can't change while it is enumerated.
                if (emptied != null)
                    foreach (string prefix in emptied)
                        roots.Remove(prefix);
                return removed;
            }
        }

        /// <summary>Adds one validated module under the lock.</summary>
        private void Add(PendingModule module)
        {
            lock (sync)
                Apply(module);
        }

        /// <summary>Adds a validated module. The caller holds the lock.</summary>
        private void Apply(PendingModule module)
        {
            if (!roots.TryGetValue(module.Prefix, out CommandRoot root))
            {
                root = new CommandRoot(module.Prefix, sync);
                roots[module.Prefix] = root;
            }

            root.AddHeading(module.OwnerKey, module.Title, module.Description);
            foreach (RegisteredCommand command in module.Commands)
                root.Add(command);
        }

        /// <summary>
        /// Validates a module and reflects over it. Takes no lock and changes
        /// nothing, so a failure leaves the registry as it was.
        /// </summary>
        private static PendingModule Build(Type moduleType, string ownerId, object ownerKey)
        {
            if (!typeof(CommandModule).IsAssignableFrom(moduleType))
                throw new CommandRegistrationException($"{moduleType.Name} is not a CommandModule");

            var rootAttr = moduleType.GetCustomAttribute<CommandRootAttribute>(false);
            if (rootAttr == null)
                throw new CommandRegistrationException($"{moduleType.Name} has no [CommandRoot]");

            string prefix = rootAttr.Prefix?.Trim();
            if (string.IsNullOrEmpty(prefix) || prefix.IndexOf(' ') >= 0)
                throw new CommandRegistrationException(
                    $"{moduleType.Name} has an invalid command prefix '{rootAttr.Prefix}'"
                );

            // '!help' is the built-in command that lists every top-level command,
            // so plugins may not claim it as a root prefix.
            if (string.Equals(prefix, "help", StringComparison.OrdinalIgnoreCase))
                throw new CommandRegistrationException(
                    $"{moduleType.Name} uses the reserved prefix 'help'; '!help' is the built-in command list"
                );

            var commands = new List<RegisteredCommand>();
            foreach (
                MethodInfo method in moduleType.GetMethods(
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
                )
            )
            {
                var cmdAttr = method.GetCustomAttribute<CommandAttribute>(false);
                if (cmdAttr == null)
                    continue;

                // An empty path denotes the root-level (default) command, run
                // for a bare '!prefix'. Non-empty paths must not start with the
                // reserved 'help' word.
                List<string> path = SplitPath(cmdAttr.Command);
                if (
                    path.Count > 0
                    && string.Equals(path[0], "help", StringComparison.OrdinalIgnoreCase)
                )
                    throw new CommandRegistrationException(
                        $"'!{prefix} help' is reserved and cannot be defined by {moduleType.Name}.{method.Name}"
                    );

                var permAttr = method.GetCustomAttribute<PermissionAttribute>(false);
                MyPromoteLevel level = permAttr?.Level ?? DefaultPermission;

                commands.Add(
                    new RegisteredCommand(
                        ownerId,
                        ownerKey,
                        prefix,
                        path,
                        cmdAttr.Description,
                        cmdAttr.HelpText,
                        level,
                        moduleType,
                        method
                    )
                );
            }

            return new PendingModule(prefix, rootAttr.Title, rootAttr.Description, ownerKey, commands);
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

        /// <summary>
        /// A module that has been validated and reflected over but not yet added,
        /// so it can be added later in one step. Holds everything <see cref="Apply"/> needs.
        /// </summary>
        private sealed class PendingModule
        {
            public readonly string Prefix;
            public readonly string Title;
            public readonly string Description;
            public readonly object OwnerKey;
            public readonly List<RegisteredCommand> Commands;

            public PendingModule(
                string prefix,
                string title,
                string description,
                object ownerKey,
                List<RegisteredCommand> commands
            )
            {
                Prefix = prefix;
                Title = title;
                Description = description;
                OwnerKey = ownerKey;
                Commands = commands;
            }
        }
    }
}
