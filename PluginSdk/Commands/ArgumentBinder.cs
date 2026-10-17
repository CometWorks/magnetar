using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace PluginSdk.Commands
{
    /// <summary>
    /// Converts the string arguments of a command into the typed parameter
    /// values of its handler method. Supports the common scalar types, enums,
    /// optional parameters (via C# defaults) and a trailing
    /// <c>params</c> array. Words beyond the parameters are an error.
    /// </summary>
    internal static class ArgumentBinder
    {
        public static bool TryBind(ParameterInfo[] parameters, IReadOnlyList<string> args, out object[] values, out string error)
        {
            values = new object[parameters.Length];
            error = null;

            int argIndex = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo p = parameters[i];

                if (IsParamsArray(p))
                {
                    Type elementType = p.ParameterType.GetElementType();
                    int remaining = args.Count - argIndex;
                    Array array = Array.CreateInstance(elementType, remaining < 0 ? 0 : remaining);
                    for (int j = 0; argIndex < args.Count; j++, argIndex++)
                    {
                        if (!TryConvert(args[argIndex], elementType, out object element))
                        {
                            error = $"'{args[argIndex]}' is not a valid {FriendlyTypeName(elementType)} for <{p.Name}>";
                            return false;
                        }
                        array.SetValue(element, j);
                    }
                    values[i] = array;
                    continue;
                }

                if (argIndex < args.Count)
                {
                    if (!TryConvert(args[argIndex], p.ParameterType, out object value))
                    {
                        error = $"'{args[argIndex]}' is not a valid {FriendlyTypeName(p.ParameterType)} for <{p.Name}>";
                        return false;
                    }
                    values[i] = value;
                    argIndex++;
                }
                else if (p.HasDefaultValue)
                {
                    values[i] = p.DefaultValue;
                }
                else
                {
                    error = $"missing argument <{p.Name}>";
                    return false;
                }
            }

            // Dropping the rest would run "remove Big Ship" on "Big"
            if (argIndex < args.Count)
            {
                error = $"unexpected argument '{args[argIndex]}'";
                if (Array.Exists(parameters, p => p.ParameterType == typeof(string)))
                    error += " (put text with spaces in quotes)";
                return false;
            }

            return true;
        }

        public static bool IsParamsArray(ParameterInfo p)
            => p.ParameterType.IsArray && p.IsDefined(typeof(ParamArrayAttribute), false);

        public static string FriendlyTypeName(Type type)
        {
            if (type.IsEnum)
                return type.Name;
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
                type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) ||
                type == typeof(byte) || type == typeof(sbyte))
                return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool))
                return "true/false";
            if (type == typeof(string))
                return "text";
            return type.Name;
        }

        private static bool TryConvert(string s, Type type, out object value)
        {
            value = null;

            if (type == typeof(string))
            {
                value = s;
                return true;
            }

            if (type.IsEnum)
            {
                try
                {
                    value = Enum.Parse(type, s, ignoreCase: true);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (type == typeof(bool))
            {
                switch (s.ToLowerInvariant())
                {
                    case "true": case "yes": case "on": case "1":
                        value = true; return true;
                    case "false": case "no": case "off": case "0":
                        value = false; return true;
                    default:
                        return false;
                }
            }

            var c = CultureInfo.InvariantCulture;
            if (type == typeof(int) && int.TryParse(s, NumberStyles.Integer, c, out int i)) { value = i; return true; }
            if (type == typeof(long) && long.TryParse(s, NumberStyles.Integer, c, out long l)) { value = l; return true; }
            if (type == typeof(short) && short.TryParse(s, NumberStyles.Integer, c, out short sh)) { value = sh; return true; }
            if (type == typeof(byte) && byte.TryParse(s, NumberStyles.Integer, c, out byte b)) { value = b; return true; }
            if (type == typeof(sbyte) && sbyte.TryParse(s, NumberStyles.Integer, c, out sbyte sb)) { value = sb; return true; }
            if (type == typeof(uint) && uint.TryParse(s, NumberStyles.Integer, c, out uint ui)) { value = ui; return true; }
            if (type == typeof(ulong) && ulong.TryParse(s, NumberStyles.Integer, c, out ulong ul)) { value = ul; return true; }
            if (type == typeof(ushort) && ushort.TryParse(s, NumberStyles.Integer, c, out ushort us)) { value = us; return true; }
            if (type == typeof(float) && float.TryParse(s, NumberStyles.Float, c, out float f)) { value = f; return true; }
            if (type == typeof(double) && double.TryParse(s, NumberStyles.Float, c, out double d)) { value = d; return true; }
            if (type == typeof(decimal) && decimal.TryParse(s, NumberStyles.Float, c, out decimal m)) { value = m; return true; }

            try
            {
                TypeConverter converter = TypeDescriptor.GetConverter(type);
                if (converter != null && converter.IsValid(s))
                {
                    value = converter.ConvertFromInvariantString(s);
                    return value != null;
                }
            }
            catch
            {
                // fall through to failure
            }

            return false;
        }
    }
}
