using System;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Marks a method of a <see cref="CommandModule"/> as a chat-command
    /// handler. The <paramref name="command"/> string is split on spaces to
    /// form a nested path under the module's
    /// <see cref="CommandRootAttribute.Prefix"/>; for example
    /// <c>[Command("grid list")]</c> in a module rooted at <c>"adm"</c> is
    /// invoked as <c>!adm grid list</c>. An empty command string
    /// (<c>[Command("")]</c>) denotes the root-level (default) command, run for
    /// a bare <c>!adm</c> with no sub-path.
    ///
    /// <para><b>Handler signature</b></para>
    /// <para>
    /// The method's parameters are bound positionally from the remaining
    /// command words. Supported parameter types are <c>string</c>, the integer
    /// and floating-point primitives, <c>bool</c>, and any <c>enum</c>. A
    /// trailing <c>params string[]</c> captures all remaining words. Parameters
    /// with C# default values are optional. Quoted <c>"multi word"</c> tokens
    /// are kept together.
    /// </para>
    /// <para><b>Replying</b></para>
    /// <para>
    /// The return value determines the reply: <c>void</c> sends nothing (call
    /// <see cref="CommandContext.Respond(string)"/> yourself), a <c>string</c>
    /// is sent privately to the caller, an <see cref="CommandReply"/> gives
    /// full control over colour/font/broadcast, and an
    /// <c>IEnumerable&lt;string&gt;</c> sends one line each.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CommandAttribute : Attribute
    {
        /// <summary>Space-separated command path relative to the module root
        /// (e.g. <c>"grid list"</c>). Matched case-insensitively.</summary>
        public string Command { get; }

        /// <summary>One-line description shown in overview and help listings.</summary>
        public string Description { get; }

        /// <summary>Longer help text shown for <c>!{prefix} help {command}</c>.
        /// Defaults to <see cref="Description"/> when null.</summary>
        public string HelpText { get; }

        public CommandAttribute(string command, string description = null, string helpText = null)
        {
            Command = command;
            Description = description;
            HelpText = helpText;
        }
    }
}
