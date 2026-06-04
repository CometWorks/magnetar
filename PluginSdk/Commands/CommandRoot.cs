using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace PluginSdk.Commands
{
    /// <summary>
    /// One <c>!prefix</c> namespace and the tree of commands registered under
    /// it. Multi-word command paths form nested <see cref="Node"/>s, so
    /// intermediate words act as sub-namespaces.
    /// </summary>
    internal sealed class CommandRoot
    {
        public string Prefix { get; }
        public string Title { get; }
        public string Description { get; }
        public string OwnerId { get; }

        /// <summary>
        /// The root-level (default) command, run for a bare <c>!prefix</c> with
        /// no sub-path. Null when the root has none, in which case a bare
        /// <c>!prefix</c> prints the overview instead. Registered via an empty
        /// <see cref="CommandAttribute.Command"/> path.
        /// </summary>
        public RegisteredCommand Default { get; private set; }

        private readonly Node root = new Node();

        public CommandRoot(string prefix, string title, string description, string ownerId)
        {
            Prefix = prefix;
            Title = title;
            Description = description;
            OwnerId = ownerId;
        }

        /// <summary>
        /// Registers <paramref name="command"/> at its path. If a command is
        /// already registered at that path it is overwritten — the last
        /// registration wins, so a later plugin may override an earlier one.
        /// </summary>
        public void Add(RegisteredCommand command)
        {
            if (command.Path.Count == 0)
            {
                Default = command;
                return;
            }

            Node node = root;
            foreach (string segment in command.Path)
            {
                if (!node.Children.TryGetValue(segment, out Node child))
                {
                    child = new Node();
                    node.Children[segment] = child;
                }
                node = child;
            }

            node.Command = command;
        }

        /// <summary>
        /// Walks <paramref name="tokens"/> from the root and returns the
        /// deepest command reached, along with how many tokens its path
        /// consumed (the remainder are arguments).
        /// </summary>
        public bool TryResolve(IReadOnlyList<string> tokens, out RegisteredCommand command, out int consumed)
        {
            command = null;
            consumed = 0;

            Node node = root;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (!node.Children.TryGetValue(tokens[i], out Node child))
                    break;
                node = child;
                if (node.Command != null)
                {
                    command = node.Command;
                    consumed = i + 1;
                }
            }

            return command != null;
        }

        /// <summary>
        /// True when the given promote <paramref name="level"/> may use at least
        /// one command in this root — its default command or any command in the
        /// tree. Used by the global <c>!help</c> listing to decide whether this
        /// top-level command appears for the caller.
        /// </summary>
        public bool IsAvailableTo(MyPromoteLevel level)
        {
            if (Default != null && Default.IsVisibleTo(level))
                return true;
            foreach (RegisteredCommand command in EnumerateCommands())
                if (command.IsVisibleTo(level))
                    return true;
            return false;
        }

        /// <summary>All commands in declaration-independent (sorted) order.</summary>
        public IEnumerable<RegisteredCommand> EnumerateCommands()
        {
            var stack = new Stack<Node>();
            stack.Push(root);
            var found = new List<RegisteredCommand>();
            while (stack.Count > 0)
            {
                Node node = stack.Pop();
                if (node.Command != null)
                    found.Add(node.Command);
                foreach (Node child in node.Children.Values)
                    stack.Push(child);
            }

            found.Sort((a, b) => string.Compare(
                string.Join(" ", a.Path), string.Join(" ", b.Path), StringComparison.OrdinalIgnoreCase));
            return found;
        }

        private sealed class Node
        {
            public readonly Dictionary<string, Node> Children =
                new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase);
            public RegisteredCommand Command;
        }
    }
}
