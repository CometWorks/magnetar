using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace PluginSdk.Commands
{
    /// <summary>
    /// How owners are told apart. An assembly is the same owner only as itself,
    /// and a string only as an equal string. A plugin's own <c>Equals</c> is never
    /// called, because it would run under the registry lock.
    /// </summary>
    internal static class OwnerKeys
    {
        /// <summary>True when <paramref name="a"/> and <paramref name="b"/> are the same owner.</summary>
        public static bool Same(object a, object b) =>
            ReferenceEquals(a, b)
            || (a is string x && b is string y && string.Equals(x, y, StringComparison.Ordinal));
    }

    /// <summary>
    /// One <c>!prefix</c> namespace and the tree of commands registered under
    /// it. Multi-word command paths form nested nodes, so intermediate words
    /// act as sub-namespaces.
    /// <para>
    /// Every registration is kept at its path: the last one is the command that
    /// runs, and removing an owner brings back whatever it had overridden. All
    /// members take the lock shared with the owning <see cref="CommandRegistry"/>,
    /// so callers never see a half-edited tree. No handler, responder or other
    /// plugin code runs while it is held.
    /// </para>
    /// </summary>
    internal sealed class CommandRoot
    {
        // The registry's lock, shared by every root, so one lock covers the root
        // map and every tree under it. Every member below takes it.
        private readonly object sync;

        // One entry per owner that registered a module on this prefix, in
        // registration order. The first is the one shown as title/description.
        private readonly List<Heading> headings = new List<Heading>(1);

        // The registrations of the root-level (default) command.
        private Slot defaults;

        private readonly Node root = new Node();

        /// <summary>The prefix this root answers to, without the <c>!</c>. Never changes.</summary>
        public string Prefix { get; }

        /// <summary>The title from the earliest owner still registered.</summary>
        public string Title
        {
            get
            {
                lock (sync)
                    return headings.Count > 0 ? headings[0].Title : Prefix;
            }
        }

        /// <summary>The description from the earliest owner still registered.</summary>
        public string Description
        {
            get
            {
                lock (sync)
                    return headings.Count > 0 ? headings[0].Description : null;
            }
        }

        /// <summary>
        /// The root-level (default) command, run for a bare <c>!prefix</c> and
        /// for <c>!prefix args</c> when no subcommand matches. Null when the
        /// root has none, in which case a bare <c>!prefix</c> prints the
        /// overview instead. Registered via an empty
        /// <see cref="CommandAttribute.Command"/> path.
        /// </summary>
        public RegisteredCommand Default
        {
            get
            {
                lock (sync)
                    return defaults.Active;
            }
        }

        /// <summary>True when nothing is registered under this root: no command and no heading.</summary>
        public bool IsEmpty
        {
            get
            {
                lock (sync)
                    return defaults.IsEmpty && root.IsEmpty && headings.Count == 0;
            }
        }

        /// <param name="prefix">The prefix this root answers to.</param>
        /// <param name="sync">The registry's lock, which this root takes for every read and write.</param>
        public CommandRoot(string prefix, object sync)
        {
            Prefix = prefix;
            this.sync = sync;
        }

        /// <summary>
        /// Records the title and description an owner gave this root. An owner's
        /// first one stays, so several modules of one plugin on this prefix show
        /// the first module's heading, and the earliest owner still registered
        /// is the one displayed.
        /// </summary>
        public void AddHeading(object ownerKey, string title, string description)
        {
            lock (sync)
            {
                foreach (Heading heading in headings)
                    if (OwnerKeys.Same(heading.OwnerKey, ownerKey))
                        return;
                headings.Add(new Heading(ownerKey, title ?? Prefix, description));
            }
        }

        /// <summary>
        /// Registers <paramref name="command"/> at its path. If a command is
        /// already registered at that path it is overridden — the last
        /// registration wins, so a later plugin may override an earlier one —
        /// but the earlier one is kept and returns if the later owner is removed.
        /// An owner registering the same path again replaces its own entry.
        /// </summary>
        public void Add(RegisteredCommand command)
        {
            lock (sync)
            {
                if (command.Path.Count == 0)
                {
                    defaults.Push(command);
                    return;
                }

                Node node = root;
                foreach (string segment in command.Path)
                    node = node.GetOrAddChild(segment);

                node.Slot.Push(command);
            }
        }

        /// <summary>
        /// Removes everything <paramref name="ownerKey"/> registered here and
        /// returns how many commands that was. Commands it had overridden
        /// become active again, and namespaces left empty are dropped.
        /// <paramref name="touched"/> is true when anything at all changed,
        /// including a heading with no commands.
        /// </summary>
        public int RemoveOwner(object ownerKey, out bool touched)
        {
            lock (sync)
            {
                touched = false;
                for (int i = headings.Count - 1; i >= 0; i--)
                {
                    if (!OwnerKeys.Same(headings[i].OwnerKey, ownerKey))
                        continue;
                    headings.RemoveAt(i);
                    touched = true;
                }

                int removed = defaults.Remove(ownerKey);
                removed += root.RemoveOwner(ownerKey);
                touched |= removed > 0;
                return removed;
            }
        }

        /// <summary>
        /// Walks <paramref name="tokens"/> from the root and returns the
        /// deepest command reached, along with how many tokens its path
        /// consumed (the remainder are arguments). When no subcommand matches,
        /// the root's default command takes all tokens as its arguments, so
        /// <c>!restart 30</c> works. It does so only if it can take that many,
        /// otherwise a typo like <c>!stop nwo</c> would run <c>!stop</c>.
        /// </summary>
        public bool TryResolve(
            IReadOnlyList<string> tokens,
            out RegisteredCommand command,
            out int consumed
        )
        {
            // The whole walk and the default fallback happen in one hold, so a
            // concurrent registration can't change the tree between steps. The
            // returned command is immutable and safe to use after the lock is gone.
            lock (sync)
            {
                command = null;
                consumed = 0;

                Node node = root;
                for (int i = 0; i < tokens.Count; i++)
                {
                    if (!node.TryGetChild(tokens[i], out Node child))
                        break;
                    node = child;
                    if (node.Slot.Active != null)
                    {
                        command = node.Slot.Active;
                        consumed = i + 1;
                    }
                }

                if (command != null)
                    return true;

                RegisteredCommand fallback = defaults.Active;
                if (fallback == null || !fallback.CanTake(tokens.Count))
                    return false;

                command = fallback;
                return true;
            }
        }

        /// <summary>
        /// True when the given promote <paramref name="level"/> may use at least
        /// one command in this root — its default command or any command in the
        /// tree. Used by the global <c>!help</c> listing to decide whether this
        /// top-level command appears for the caller. Allocates nothing.
        /// </summary>
        public bool IsAvailableTo(MyPromoteLevel level)
        {
            lock (sync)
            {
                RegisteredCommand fallback = defaults.Active;
                return (fallback != null && fallback.IsVisibleTo(level))
                    || root.AnyVisibleTo(level);
            }
        }

        /// <summary>
        /// This root's heading, default command and every command, read in one
        /// step so a help screen never mixes two registry states. The command
        /// list is a sorted copy.
        /// </summary>
        public Overview GetOverview()
        {
            var found = new List<RegisteredCommand>();
            string title;
            string description;
            RegisteredCommand fallback;
            lock (sync)
            {
                root.Collect(found);
                title = headings.Count > 0 ? headings[0].Title : Prefix;
                description = headings.Count > 0 ? headings[0].Description : null;
                fallback = defaults.Active;
            }

            // Commands never change once built, so sorting needs no lock.
            var keys = new string[found.Count];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = string.Join(" ", found[i].Path);
            RegisteredCommand[] items = found.ToArray();
            Array.Sort(keys, items, StringComparer.OrdinalIgnoreCase);

            return new Overview(title, description, fallback, new List<RegisteredCommand>(items));
        }

        /// <summary>A consistent read of one root, see <see cref="GetOverview"/>.</summary>
        public readonly struct Overview
        {
            /// <summary>The root's title, or its prefix when no owner gave one.</summary>
            public readonly string Title;

            /// <summary>The root's description, or null.</summary>
            public readonly string Description;

            /// <summary>The root-level (default) command, or null.</summary>
            public readonly RegisteredCommand Default;

            /// <summary>Every command below the root, sorted by path. A copy the caller may keep.</summary>
            public readonly List<RegisteredCommand> Commands;

            public Overview(
                string title,
                string description,
                RegisteredCommand @default,
                List<RegisteredCommand> commands
            )
            {
                Title = title;
                Description = description;
                Default = @default;
                Commands = commands;
            }
        }

        /// <summary>What one owner called this root. Kept per owner so the display
        /// falls back to the next owner when the first one is removed.</summary>
        private readonly struct Heading
        {
            public readonly object OwnerKey;
            public readonly string Title;
            public readonly string Description;

            public Heading(object ownerKey, string title, string description)
            {
                OwnerKey = ownerKey;
                Title = title;
                Description = description;
            }
        }

        /// <summary>
        /// The registrations at one path, one per owner. The last one runs. The
        /// overridden ones sit in a list that exists only once a second owner
        /// registers the same path, so the common case allocates nothing extra.
        /// <para>
        /// This is a mutable struct on purpose, to avoid one object per path. It
        /// must only be used through the field that holds it, such as
        /// <c>node.Slot.Push(...)</c>, and never copied into a local, or the
        /// edits would be lost. Invariant: one owner appears at most once across
        /// <see cref="Active"/> and the overridden list, and the list is null
        /// whenever <see cref="Active"/> is null. Its owner comparisons are cheap
        /// and run no plugin code, so it is safe to call under the registry lock.
        /// </para>
        /// </summary>
        private struct Slot
        {
            /// <summary>The command that runs, or null when nothing is registered here.</summary>
            public RegisteredCommand Active;

            // Overridden commands, oldest first. The last one becomes Active again
            // when the current Active's owner is removed.
            private List<RegisteredCommand> shadowed;

            public bool IsEmpty => Active == null;

            /// <summary>
            /// Makes <paramref name="command"/> the active one. The previous active
            /// command is kept as overridden, unless it is the same owner's, in
            /// which case it is replaced. An owner already overridden here moves to the top.
            /// </summary>
            public void Push(RegisteredCommand command)
            {
                if (Active != null)
                {
                    if (OwnerKeys.Same(Active.OwnerKey, command.OwnerKey))
                    {
                        Active = command;
                        return;
                    }

                    if (shadowed == null)
                        shadowed = new List<RegisteredCommand>(1);
                    else
                        RemoveFrom(shadowed, command.OwnerKey);
                    shadowed.Add(Active);
                }

                Active = command;
            }

            /// <summary>
            /// Removes <paramref name="ownerKey"/>'s registration, active or overridden,
            /// and returns how many it removed. If the active one goes, the most
            /// recently overridden command becomes active again.
            /// </summary>
            public int Remove(object ownerKey)
            {
                int removed = 0;
                if (shadowed != null)
                {
                    removed = RemoveFrom(shadowed, ownerKey);
                    if (shadowed.Count == 0)
                        shadowed = null;
                }

                if (Active != null && OwnerKeys.Same(Active.OwnerKey, ownerKey))
                {
                    removed++;
                    Active = null;
                    if (shadowed != null)
                    {
                        Active = shadowed[shadowed.Count - 1];
                        shadowed.RemoveAt(shadowed.Count - 1);
                        if (shadowed.Count == 0)
                            shadowed = null;
                    }
                }

                return removed;
            }

            // A plain loop rather than List.RemoveAll with a lambda: the lambda would
            // allocate a closure on every call, including the common no-op ones.
            private static int RemoveFrom(List<RegisteredCommand> list, object ownerKey)
            {
                int removed = 0;
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (!OwnerKeys.Same(list[i].OwnerKey, ownerKey))
                        continue;
                    list.RemoveAt(i);
                    removed++;
                }
                return removed;
            }
        }

        /// <summary>
        /// One word of a command path. Holds the registrations of the command that
        /// ends here, if any, and the nodes of the words that can follow.
        /// </summary>
        private sealed class Node
        {
            /// <summary>The registrations of the command that ends at this word.</summary>
            public Slot Slot;

            // Created on first use: most nodes are leaves and never need one.
            private Dictionary<string, Node> children;

            /// <summary>True when no command ends here or below.</summary>
            public bool IsEmpty => Slot.IsEmpty && (children == null || children.Count == 0);

            /// <summary>Finds the node for the next word, without creating it.</summary>
            public bool TryGetChild(string segment, out Node child)
            {
                if (children == null)
                {
                    child = null;
                    return false;
                }

                return children.TryGetValue(segment, out child);
            }

            /// <summary>Finds the node for the next word, creating it when missing.</summary>
            public Node GetOrAddChild(string segment)
            {
                if (children == null)
                    children = new Dictionary<string, Node>(1, StringComparer.OrdinalIgnoreCase);
                if (!children.TryGetValue(segment, out Node child))
                {
                    child = new Node();
                    children[segment] = child;
                }
                return child;
            }

            /// <summary>True when any command here or below is visible at <paramref name="level"/>.
            /// Walks the tree and allocates nothing.</summary>
            public bool AnyVisibleTo(MyPromoteLevel level)
            {
                if (Slot.Active != null && Slot.Active.IsVisibleTo(level))
                    return true;
                if (children != null)
                    foreach (Node child in children.Values)
                        if (child.AnyVisibleTo(level))
                            return true;
                return false;
            }

            /// <summary>Adds the active command of every path here and below to <paramref name="into"/>, unsorted.</summary>
            public void Collect(List<RegisteredCommand> into)
            {
                if (Slot.Active != null)
                    into.Add(Slot.Active);
                if (children != null)
                    foreach (Node child in children.Values)
                        child.Collect(into);
            }

            /// <summary>
            /// Removes <paramref name="ownerKey"/>'s registrations here and below and
            /// returns how many. Nodes left with nothing are dropped.
            /// </summary>
            public int RemoveOwner(object ownerKey)
            {
                int removed = Slot.Remove(ownerKey);
                if (children == null)
                    return removed;

                // Collect first and remove afterwards: a dictionary can't be
                // changed while it is being enumerated.
                List<string> emptied = null;
                foreach (KeyValuePair<string, Node> pair in children)
                {
                    removed += pair.Value.RemoveOwner(ownerKey);
                    if (pair.Value.IsEmpty)
                        (emptied ?? (emptied = new List<string>())).Add(pair.Key);
                }

                if (emptied != null)
                    foreach (string key in emptied)
                        children.Remove(key);
                if (children.Count == 0)
                    children = null;
                return removed;
            }
        }
    }
}
