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
        /// <summary>Display name of the owner, for logs and messages only.</summary>
        public string OwnerId { get; }

        /// <summary>
        /// Identity of the owner: the registering <see cref="System.Reflection.Assembly"/>
        /// (by reference), or the caller's string for <see cref="CommandRegistry.RegisterModule(Type, string)"/>.
        /// Two owners with the same display name stay distinct.
        /// </summary>
        public object OwnerKey { get; }

        public string Prefix { get; }

        /// <summary>Command path relative to the root, e.g. <c>["grid","list"]</c>.</summary>
        public IReadOnlyList<string> Path { get; }

        public string Description { get; }
        public string HelpText { get; }
        public MyPromoteLevel MinPromoteLevel { get; }

        public Type ModuleType { get; }
        public MethodInfo Method { get; }
        public ParameterInfo[] Parameters { get; }

        // Whether the last parameter is a params array. Worked out once here
        // because CanTake runs under the registry lock and should not reflect.
        private readonly bool takesParamsArray;

        /// <summary>Auto-generated usage string, e.g. <c>!ess tp &lt;target&gt; [distance]</c>.</summary>
        public string Syntax { get; }

        public RegisteredCommand(
            string ownerId,
            object ownerKey,
            string prefix,
            IReadOnlyList<string> path,
            string description,
            string helpText,
            MyPromoteLevel minPromoteLevel,
            Type moduleType,
            MethodInfo method
        )
        {
            OwnerId = ownerId;
            OwnerKey = ownerKey ?? throw new ArgumentNullException(nameof(ownerKey));
            Prefix = prefix;
            Path = path;
            Description = description;
            HelpText = string.IsNullOrEmpty(helpText) ? description : helpText;
            MinPromoteLevel = minPromoteLevel;
            ModuleType = moduleType;
            Method = method;
            Parameters = method.GetParameters();
            takesParamsArray =
                Parameters.Length > 0
                && ArgumentBinder.IsParamsArray(Parameters[Parameters.Length - 1]);
            Syntax = BuildSyntax(prefix, path, Parameters);
        }

        public bool IsVisibleTo(MyPromoteLevel level) => level >= MinPromoteLevel;

        /// <summary>True when the handler has a parameter for each of
        /// <paramref name="count"/> arguments, or a <c>params</c> array.</summary>
        public bool CanTake(int count) => count <= Parameters.Length || takesParamsArray;

        public object Invoke(CommandContext context, object[] values)
        {
            var module = (CommandModule)Activator.CreateInstance(ModuleType);
            module.Context = context;
            return Method.Invoke(module, values);
        }

        private static string BuildSyntax(
            string prefix,
            IReadOnlyList<string> path,
            ParameterInfo[] parameters
        )
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
