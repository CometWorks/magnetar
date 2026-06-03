using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Parses a chat line, resolves it against a <see cref="CommandRegistry"/>
    /// and invokes the matching handler, including the built-in per-root
    /// overview and <c>help</c>. The whole pipeline is host-independent: it
    /// runs against a <see cref="CommandCaller"/> and an
    /// <see cref="ICommandResponder"/>, so it can be exercised in tests without
    /// a live game.
    /// </summary>
    public sealed class CommandDispatcher
    {
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

            string prefix = tokens[0];
            if (!registry.TryGetRoot(prefix, out CommandRoot root))
                return false;

            var rest = tokens.GetRange(1, tokens.Count - 1);

            if (rest.Count == 0)
            {
                // A bare '!prefix' runs the root's default command when it has
                // one the caller may use; otherwise it prints the overview.
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
                Reply(responder, caller, CommandReply.Error(
                    $"Unknown command. Try !{root.Prefix} help").WithAuthor(root.Title));
                return true;
            }

            if (!command.IsVisibleTo(caller.PromoteLevel))
            {
                Reply(responder, caller, CommandReply.Error(
                    "You do not have permission to use that command.").WithAuthor(root.Title));
                return true;
            }

            var args = rest.GetRange(consumed, rest.Count - consumed);
            ExecuteCommand(root, command, args, caller, responder);
            return true;
        }

        private void ExecuteCommand(CommandRoot root, RegisteredCommand command,
            List<string> args, in CommandCaller caller, ICommandResponder responder)
        {
            if (!ArgumentBinder.TryBind(command.Parameters, args, out object[] values, out string bindError))
            {
                Reply(responder, caller, CommandReply.Error(
                    $"{bindError}. Usage: {command.Syntax}").WithAuthor(root.Title));
                return;
            }

            var context = new CommandContext(caller, root.Prefix, args, string.Join(" ", args), responder);
            try
            {
                object result = command.Invoke(context, values);
                DispatchResult(result, context);
            }
            catch (Exception ex)
            {
                Exception actual = (ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null)
                    ? tie.InnerException : ex;
                onError?.Invoke($"Command '{command.Syntax}' failed", actual);
                Reply(responder, caller, CommandReply.Error(
                    $"Command failed: {actual.Message}").WithAuthor(root.Title));
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

        private static void SendOverview(CommandRoot root, CommandCaller caller, ICommandResponder responder)
        {
            var visible = root.EnumerateCommands().Where(c => c.IsVisibleTo(caller.PromoteLevel)).ToList();

            string header = string.IsNullOrEmpty(root.Description)
                ? $"=== {root.Title} ==="
                : $"=== {root.Title} — {root.Description} ===";
            Reply(responder, caller, CommandReply.Info(header).WithAuthor(root.Title));

            if (visible.Count == 0)
            {
                Reply(responder, caller, CommandReply.Info("(no commands available to you)").WithAuthor(root.Title));
                return;
            }

            foreach (RegisteredCommand c in visible)
            {
                string line = string.IsNullOrEmpty(c.Description)
                    ? $"!{root.Prefix} {string.Join(" ", c.Path)}"
                    : $"!{root.Prefix} {string.Join(" ", c.Path)} — {c.Description}";
                Reply(responder, caller, CommandReply.Info(line).WithAuthor(root.Title));
            }

            Reply(responder, caller, CommandReply.Info($"Type !{root.Prefix} help <command> for details.").WithAuthor(root.Title));
        }

        private static void SendHelp(CommandRoot root, List<string> path, CommandCaller caller, ICommandResponder responder)
        {
            if (path.Count == 0)
            {
                SendOverview(root, caller, responder);
                return;
            }

            if (!root.TryResolve(path, out RegisteredCommand command, out _) || !command.IsVisibleTo(caller.PromoteLevel))
            {
                Reply(responder, caller, CommandReply.Error(
                    $"No such command: !{root.Prefix} {string.Join(" ", path)}").WithAuthor(root.Title));
                return;
            }

            Reply(responder, caller, CommandReply.Info($"Usage: {command.Syntax}").WithAuthor(root.Title));
            if (!string.IsNullOrEmpty(command.HelpText))
                Reply(responder, caller, CommandReply.Info(command.HelpText).WithAuthor(root.Title));
        }

        private static void Reply(ICommandResponder responder, CommandCaller caller, in CommandReply reply)
            => responder.Send(reply, caller);
    }
}
