using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using VRage.Game.ModAPI;

namespace PluginSdk.Commands
{
    /// <summary>
    /// A single chat command resolved from a <see cref="CommandAttribute"/> on a
    /// <see cref="CommandModule"/> method, together with the reflection metadata
    /// needed to validate permission, bind arguments and invoke the handler.
    /// </summary>
    internal sealed class RegisteredCommand
    {
        public string OwnerId { get; }
        public string Prefix { get; }

        /// <summary>Command path relative to the root, e.g. <c>["grid","list"]</c>.</summary>
        public IReadOnlyList<string> Path { get; }

        public string Description { get; }
        public string HelpText { get; }
        public MyPromoteLevel MinPromoteLevel { get; }

        public Type ModuleType { get; }
        public MethodInfo Method { get; }
        public ParameterInfo[] Parameters { get; }

        /// <summary>Auto-generated usage string, e.g. <c>!ess tp &lt;target&gt; [distance]</c>.</summary>
        public string Syntax { get; }

        public RegisteredCommand(string ownerId, string prefix, IReadOnlyList<string> path,
            string description, string helpText, MyPromoteLevel minPromoteLevel,
            Type moduleType, MethodInfo method)
        {
            OwnerId = ownerId;
            Prefix = prefix;
            Path = path;
            Description = description;
            HelpText = string.IsNullOrEmpty(helpText) ? description : helpText;
            MinPromoteLevel = minPromoteLevel;
            ModuleType = moduleType;
            Method = method;
            Parameters = method.GetParameters();
            Syntax = BuildSyntax(prefix, path, Parameters);
        }

        public bool IsVisibleTo(MyPromoteLevel level) => level >= MinPromoteLevel;

        /// <summary>True when the handler has a parameter for each of
        /// <paramref name="count"/> arguments, or a <c>params</c> array.</summary>
        public bool CanTake(int count)
            => count <= Parameters.Length
               || (Parameters.Length > 0 && ArgumentBinder.IsParamsArray(Parameters[Parameters.Length - 1]));

        public object Invoke(CommandContext context, object[] values)
        {
            var module = (CommandModule)Activator.CreateInstance(ModuleType);
            module.Context = context;
            return Method.Invoke(module, values);
        }

        private static string BuildSyntax(string prefix, IReadOnlyList<string> path, ParameterInfo[] parameters)
        {
            var sb = new StringBuilder();
            sb.Append('!').Append(prefix);
            foreach (string segment in path)
                sb.Append(' ').Append(segment);

            foreach (ParameterInfo p in parameters)
            {
                if (ArgumentBinder.IsParamsArray(p))
                    sb.Append(" [").Append(p.Name).Append("...]");
                else if (p.HasDefaultValue)
                    sb.Append(" [").Append(p.Name).Append(']');
                else
                    sb.Append(" <").Append(p.Name).Append('>');
            }

            return sb.ToString();
        }
    }
}
