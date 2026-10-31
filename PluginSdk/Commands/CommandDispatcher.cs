using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Parses a chat line, resolves it against a <see cref="CommandRegistry"/>
    /// and invokes the matching handler, including the global <c>!help</c>
    /// top-level listing and the built-in per-root overview and <c>help</c>.
    /// The whole pipeline is host-independent: it
    /// runs against a <see cref="CommandCaller"/> and an
    /// <see cref="ICommandResponder"/>, so it can be exercised in tests without
    /// a live game.
    /// </summary>
    public sealed class CommandDispatcher
    {
        private sealed class Alias
        {
            internal string[] Source;
            internal string[] Target;
        }

        private volatile Alias[] aliases = Array.Empty<Alias>();
        private readonly CommandRegistry registry;
        private readonly Action<string, Exception> onError;

        /// <summary>The character that introduces a command. Defaults to <c>!</c>.</summary>
        public char Prefix { get; set; } = '!';

        /// <summary>
        /// Creates a dispatcher. <paramref name="onError"/>, when supplied, is
        /// called with a message and the exception whenever a handler throws,
        /// so the host can log it; the caller always gets a generic error reply.
        /// </summary>
        public CommandDispatcher(CommandRegistry registry, Action<string, Exception> onError = null)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.onError = onError;
        }

        /// <summary>
        /// Replaces the alias snapshot atomically. Paths include ! and contain only letters,
        /// digits, underscores, hyphens and spaces/tabs. Longest whole-token source wins,
        /// before registered commands. Expansion happens once; caller and responder are unchanged.
        /// Invalid, duplicate, self-referencing or !help source aliases reject the entire update.
        /// Call on the game thread. An empty collection clears aliases.
        /// </summary>
        public void SetAliases(IEnumerable<KeyValuePair<string, string>> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var updated = new List<Alias>();
            var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                string[] source = AliasPath(value.Key);
                string[] target = AliasPath(value.Value);
                if (updated.Count >= 64 || !sources.Add(string.Join(" ", source))
                    || source[0].Equals("help", StringComparison.OrdinalIgnoreCase)
                    || source.SequenceEqual(target, StringComparer.OrdinalIgnoreCase))
                    throw new ArgumentException("Aliases must be unique, cannot alias themselves or replace !help, and are limited to 64.", nameof(values));
                updated.Add(new Alias { Source = source, Target = target });
            }
            aliases = updated.OrderByDescending(alias => alias.Source.Length).ToArray();
        }

        private static string[] AliasPath(string path)
        {
            if (path == null || path.Length > 512
                || !Regex.IsMatch(path, @"\A![a-zA-Z0-9_-]+(?:[ \t]+[a-zA-Z0-9_-]+)*\z"))
                throw new ArgumentException("Alias paths must start with ! and contain command words made of letters, digits, underscores or hyphens (maximum 512 characters).");
            return CommandLine.Tokenize(path.Substring(1)).ToArray();
        }

        /// <summary>
        /// Handles a chat <paramref name="message"/>. Returns <c>true</c> when
        /// it was recognised as a command for a registered root (and therefore
        /// should be consumed/suppressed from normal chat), or <c>false</c> when
        /// it is ordinary chat the host should leave alone.
        /// </summary>
        public bool Handle(string message, in CommandCaller caller, ICommandResponder responder)
        {
            if (string.IsNullOrEmpty(message) || message[0] != Prefix)
                return false;

            List<string> tokens = CommandLine.Tokenize(message.Substring(1));
            if (tokens.Count == 0)
                return false;

            bool expanded = false;
            foreach (var alias in aliases)
            {
                if (tokens.Count < alias.Source.Length
                    || !tokens.Take(alias.Source.Length).SequenceEqual(alias.Source, StringComparer.OrdinalIgnoreCase))
                    continue;
                tokens = alias.Target.Concat(tokens.Skip(alias.Source.Length)).ToList();
                expanded = true;
                break;
            }

            // '!help' is the built-in command that lists every top-level command
            // available to the caller. It is not tied to a registered root, so it
            // is handled before root resolution. '!help <prefix>' drills into that
            // root and lists its sub-commands; an unknown prefix falls back to the
            // top-level listing.
            if (string.Equals(tokens[0], "help", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Count > 1 && registry.TryGetRoot(tokens[1], out CommandRoot helpRoot))
                    SendOverview(helpRoot, caller, responder);
                else
                    SendGlobalHelp(caller, responder);
                return true;
            }

            string prefix = tokens[0];
            if (!registry.TryGetRoot(prefix, out CommandRoot root))
            {
                if (expanded)
                    Reply(responder, caller, CommandReply.Error("Alias target is unavailable. Ask an administrator to check the configured command alias."));
                return expanded;
            }

            var rest = tokens.GetRange(1, tokens.Count - 1);

            if (rest.Count == 0)
            {
                // A bare '!prefix' runs the root's default command when it has
                // one the caller may use; otherwise it prints the overview (for a
                // root with only a default, the permission reply).
                if (root.Default != null && root.Default.IsVisibleTo(caller.PromoteLevel))
                    ExecuteCommand(root, root.Default, rest, caller, responder);
                else
                    SendOverview(root, caller, responder);
                return true;
            }

            if (string.Equals(rest[0], "help", StringComparison.OrdinalIgnoreCase))
            {
                SendHelp(root, rest.GetRange(1, rest.Count - 1), caller, responder);
                return true;
            }

            if (!root.TryResolve(rest, out RegisteredCommand command, out int consumed))
            {
                Reply(
                    responder,
                    caller,
                    CommandReply
                        .Error($"Unknown command. Try !{root.Prefix} help")
                        .WithAuthor(root.Title)
                );
                return true;
            }

            if (!command.IsVisibleTo(caller.PromoteLevel))
            {
                Reply(
                    responder,
                    caller,
                    CommandReply
                        .Error("You do not have permission to use that command.")
                        .WithAuthor(root.Title)
                );
                return true;
            }

            var args = rest.GetRange(consumed, rest.Count - consumed);
            ExecuteCommand(root, command, args, caller, responder);
            return true;
        }

        private void ExecuteCommand(
            CommandRoot root,
            RegisteredCommand command,
            List<string> args,
            in CommandCaller caller,
            ICommandResponder responder
        )
        {
            if (
                !ArgumentBinder.TryBind(
                    command.Parameters,
                    args,
                    out object[] values,
                    out string bindError
                )
            )
            {
                Reply(
                    responder,
                    caller,
                    CommandReply
                        .Error($"{bindError}. Usage: {command.Syntax}")
                        .WithAuthor(root.Title)
                );
                return;
            }

            var context = new CommandContext(
                caller,
                root.Prefix,
                args,
                string.Join(" ", args),
                responder
            );
            try
            {
                object result = command.Invoke(context, values);
                DispatchResult(result, context);
            }
            catch (Exception ex)
            {
                Exception actual =
                    (
                        ex is System.Reflection.TargetInvocationException tie
                        && tie.InnerException != null
                    )
                        ? tie.InnerException
                        : ex;
                onError?.Invoke($"Command '{command.Syntax}' failed", actual);
                Reply(
                    responder,
                    caller,
                    CommandReply.Error($"Command failed: {actual.Message}").WithAuthor(root.Title)
                );
            }
        }

        private static void DispatchResult(object result, CommandContext context)
        {
            switch (result)
            {
                case null:
                    return;
                case string text:
                    if (!string.IsNullOrEmpty(text))
                        context.Respond(text);
                    return;
                case CommandReply reply:
                    if (reply.HasContent)
                        context.Respond(reply);
                    return;
                case IEnumerable enumerable:
                    foreach (object item in enumerable)
                    {
                        if (item is string line)
                        {
                            if (!string.IsNullOrEmpty(line))
                                context.Respond(line);
                        }
                        else if (item is CommandReply r)
                        {
                            if (r.HasContent)
                                context.Respond(r);
                        }
                    }
                    return;
                default:
                    context.Respond(result.ToString());
                    return;
            }
        }

        private void SendGlobalHelp(CommandCaller caller, ICommandResponder responder)
        {
            const string author = "Help";

            var roots = registry
                .EnumerateRoots()
                .Where(r => r.IsAvailableTo(caller.PromoteLevel))
                .OrderBy(r => r.Prefix, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var lines = new List<string> { "=== Server Commands ===" };

            if (roots.Count == 0)
            {
                lines.Add("No server commands available");
                if (TryShowMissionHelp(caller, "Magnetar Help", "Server Commands", lines))
                    return;

                ReplyLines(responder, caller, author, lines);
                return;
            }

            foreach (CommandRoot root in roots)
            {
                string line = string.IsNullOrEmpty(root.Description)
                    ? $"!{root.Prefix}"
                    : $"!{root.Prefix} - {root.Description}";
                lines.Add(line);
            }

            lines.Add("Type a command to run it or list its sub-commands.");
            if (TryShowMissionHelp(caller, "Magnetar Help", "Server Commands", lines))
                return;

            ReplyLines(responder, caller, author, lines);
        }

        private static void SendOverview(
            CommandRoot root,
            CommandCaller caller,
            ICommandResponder responder
        )
        {
            var visible = root.EnumerateCommands()
                .Where(c => c.IsVisibleTo(caller.PromoteLevel))
                .ToList();

            // A root with nothing but its default command, like !restart, has no
            // overview: show its usage, or tell a caller who may not use it.
            if (visible.Count == 0 && root.Default != null)
            {
                if (root.Default.IsVisibleTo(caller.PromoteLevel))
                    SendUsage(root, root.Default, caller, responder);
                else
                    Reply(
                        responder,
                        caller,
                        CommandReply
                            .Error("You do not have permission to use that command.")
                            .WithAuthor(root.Title)
                    );
                return;
            }

            string header = string.IsNullOrEmpty(root.Description)
                ? $"=== {root.Title} ==="
                : $"=== {root.Title} - {root.Description} ===";
            var lines = new List<string> { header };

            if (visible.Count == 0)
            {
                lines.Add("No server commands available");
                if (TryShowMissionHelp(caller, root.Title, "Command Overview", lines))
                    return;

                ReplyLines(responder, caller, root.Title, lines);
                return;
            }

            foreach (RegisteredCommand c in visible)
            {
                string line = string.IsNullOrEmpty(c.Description)
                    ? $"!{root.Prefix} {string.Join(" ", c.Path)}"
                    : $"!{root.Prefix} {string.Join(" ", c.Path)} - {c.Description}";
                lines.Add(line);
            }

            lines.Add($"Type !{root.Prefix} help <command> for details.");
            if (TryShowMissionHelp(caller, root.Title, "Command Overview", lines))
                return;

            ReplyLines(responder, caller, root.Title, lines);
        }

        private static void SendHelp(
            CommandRoot root,
            List<string> path,
            CommandCaller caller,
            ICommandResponder responder
        )
        {
            if (path.Count == 0)
            {
                SendOverview(root, caller, responder);
                return;
            }

            if (
                !root.TryResolve(path, out RegisteredCommand command, out _)
                || !command.IsVisibleTo(caller.PromoteLevel)
            )
            {
                Reply(
                    responder,
                    caller,
                    CommandReply
                        .Error($"No such command: !{root.Prefix} {string.Join(" ", path)}")
                        .WithAuthor(root.Title)
                );
                return;
            }

            SendUsage(root, command, caller, responder);
        }

        private static void SendUsage(
            CommandRoot root,
            RegisteredCommand command,
            CommandCaller caller,
            ICommandResponder responder
        )
        {
            var lines = new List<string> { $"Usage: {command.Syntax}" };
            if (!string.IsNullOrEmpty(command.HelpText))
                lines.Add(command.HelpText);

            if (TryShowMissionHelp(caller, root.Title, "Command Help", lines))
                return;

            ReplyLines(responder, caller, root.Title, lines);
        }

        private static bool TryShowMissionHelp(
            CommandCaller caller,
            string screenTitle,
            string currentObjective,
            List<string> lines
        )
        {
            if (!PluginSdk.MissionScreens.IsHostSenderAvailable)
                return false;

            var body = new StringBuilder();
            foreach (string line in lines)
                body.AppendLine(line);

            return PluginSdk.MissionScreens.ShowToPlayer(
                caller.IdentityId,
                screenTitle,
                null,
                currentObjective,
                body.ToString(),
                "Close"
            );
        }

        private static void ReplyLines(
            ICommandResponder responder,
            CommandCaller caller,
            string author,
            List<string> lines
        )
        {
            foreach (string line in lines)
                Reply(responder, caller, CommandReply.Info(line).WithAuthor(author));
        }

        private static void Reply(
            ICommandResponder responder,
            CommandCaller caller,
            in CommandReply reply
        ) => responder.Send(reply, caller);
    }
}
