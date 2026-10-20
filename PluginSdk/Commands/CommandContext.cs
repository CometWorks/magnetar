using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace PluginSdk.Commands
{
    /// <summary>
    /// The per-invocation environment handed to a command handler through
    /// <see cref="CommandModule.Context"/>. Exposes the caller, the parsed
    /// arguments, and convenience methods for replying. A fresh instance is
    /// created for every command execution.
    /// </summary>
    public sealed class CommandContext
    {
        private readonly ICommandResponder responder;

        /// <summary>Who issued the command.</summary>
        public CommandCaller Caller { get; }

        /// <summary>The command root prefix that was matched (e.g. <c>"ess"</c>),
        /// also used as the default reply author.</summary>
        public string Prefix { get; }

        /// <summary>The argument words after the command path, in order. Quoted
        /// tokens are already unwrapped.</summary>
        public IReadOnlyList<string> Args { get; }

        /// <summary>The argument words joined by single spaces.</summary>
        public string RawArgs { get; }

        public CommandContext(
            in CommandCaller caller,
            string prefix,
            IReadOnlyList<string> args,
            string rawArgs,
            ICommandResponder responder
        )
        {
            Caller = caller;
            Prefix = prefix;
            Args = args;
            RawArgs = rawArgs;
            this.responder = responder;
        }

        /// <summary>Sends a plain reply to the caller.</summary>
        public void Respond(string text) =>
            responder.Send(new CommandReply(text, font: MyFontEnum.White, author: Prefix), Caller);

        /// <summary>Sends a coloured reply to the caller.</summary>
        public void Respond(string text, Color color, string font = MyFontEnum.White) =>
            responder.Send(new CommandReply(text, color, font, Prefix), Caller);

        /// <summary>Sends a fully specified reply.</summary>
        public void Respond(in CommandReply reply)
        {
            CommandReply r = reply.Author is null ? reply.WithAuthor(Prefix) : reply;
            responder.Send(r, Caller);
        }
    }
}
