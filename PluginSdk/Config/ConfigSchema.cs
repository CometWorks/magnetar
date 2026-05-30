using System;
using System.Collections.Generic;
using System.Reflection;

namespace PluginSdk.Config
{
    /// <summary>
    /// Reflection-based schema extractor. Builds a
    /// <see cref="ConfigSchemaData"/> describing the layout tree, options
    /// and struct member definitions of a <see cref="PluginConfig"/>-derived
    /// type, for an external manager app to render a Web UI from.
    /// </summary>
    public static class ConfigSchema
    {
        /// <summary>Builds the schema for the given <paramref name="configType"/>.</summary>
        public static ConfigSchemaData Build(Type configType)
        {
            if (configType == null) throw new ArgumentNullException(nameof(configType));
            if (!typeof(PluginConfig).IsAssignableFrom(configType))
                throw new ArgumentException($"{configType} does not derive from {nameof(PluginConfig)}", nameof(configType));

            var schema = new ConfigSchemaData();

            foreach (var layout in configType.GetCustomAttributes<LayoutContainerAttribute>(inherit: true))
            {
                schema.Layout.Add(new LayoutContainerInfo
                {
                    Kind = layout.Kind,
                    Id = layout.Id,
                    Parent = layout.Parent,
                    Caption = layout.Caption,
                });
            }

            // Struct discovery is a worklist: top-level options enqueue any
            // referenced structs; processing a struct may itself enqueue more
            // structs (nested struct member, list/dict of struct, ...).
            var pendingStructs = new Queue<Type>();

            foreach (var prop in PluginConfig.GetConfigProperties(configType))
            {
                var info = BuildPropertyInfo(prop, pendingStructs, schema);
                if (info != null) schema.Properties.Add(info);
            }

            while (pendingStructs.Count > 0)
            {
                var structType = pendingStructs.Dequeue();
                if (schema.Structs.ContainsKey(structType.Name)) continue;
                schema.Structs[structType.Name] = BuildStructInfo(structType, pendingStructs, schema);
            }

            return schema;
        }

        private static ConfigPropertyInfo BuildPropertyInfo(PropertyInfo prop, Queue<Type> pendingStructs, ConfigSchemaData schema)
        {
            var option = prop.GetCustomAttribute<ConfigOptionAttribute>();
            if (option == null) return null;

            var info = new ConfigPropertyInfo
            {
                Name = prop.Name,
                Description = option.Description,
                Parent = option.Parent,
            };

            var t = prop.PropertyType;

            switch (option)
            {
                case BoolOptionAttribute _:
                    info.Type = "bool";
                    break;

                case IntOptionAttribute io:
                    info.Type = "int";
                    if (io.Min != int.MinValue) info.Min = io.Min;
                    if (io.Max != int.MaxValue) info.Max = io.Max;
                    break;

                case LongOptionAttribute lo:
                    info.Type = "long";
                    if (lo.Min != long.MinValue) info.Min = lo.Min;
                    if (lo.Max != long.MaxValue) info.Max = lo.Max;
                    break;

                case FloatOptionAttribute fo:
                    info.Type = "float";
                    if (!float.IsNegativeInfinity(fo.Min)) info.Min = fo.Min;
                    if (!float.IsPositiveInfinity(fo.Max)) info.Max = fo.Max;
                    break;

                case DoubleOptionAttribute dbo:
                    info.Type = "double";
                    if (!double.IsNegativeInfinity(dbo.Min)) info.Min = dbo.Min;
                    if (!double.IsPositiveInfinity(dbo.Max)) info.Max = dbo.Max;
                    break;

                case StringOptionAttribute so:
                    info.Type = "string";
                    if (so.MaxLength > 0) info.MaxLength = so.MaxLength;
                    info.Pattern = so.Pattern;
                    break;

                case ListOptionAttribute listOpt:
                    info.Type = "list";
                    if (listOpt.MaxCount > 0) info.MaxCount = listOpt.MaxCount;
                    info.TreeParentField = listOpt.TreeParentField;
                    var elementType = GetGenericArgument(t, typeof(List<>), 0);
                    info.ElementType = TypeName(elementType);
                    if (info.ElementType == "struct")
                    {
                        info.ElementStruct = elementType.Name;
                        pendingStructs.Enqueue(elementType);
                    }
                    else if (info.ElementType == "enum")
                    {
                        info.ElementEnum = elementType.Name;
                        RegisterEnum(schema, elementType);
                    }
                    break;

                case DictOptionAttribute dictOpt:
                    info.Type = "dict";
                    if (dictOpt.MaxCount > 0) info.MaxCount = dictOpt.MaxCount;
                    info.TreeParentField = dictOpt.TreeParentField;
                    var dictArgs = GetDictionaryArguments(t);
                    info.KeyType = TypeName(dictArgs.Key);
                    info.ValueType = TypeName(dictArgs.Value);
                    if (info.ValueType == "struct")
                    {
                        info.ValueStruct = dictArgs.Value.Name;
                        pendingStructs.Enqueue(dictArgs.Value);
                    }
                    else if (info.ValueType == "enum")
                    {
                        info.ValueEnum = dictArgs.Value.Name;
                        RegisterEnum(schema, dictArgs.Value);
                    }
                    break;

                case StructOptionAttribute _:
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        // [StructOption] on a List<Struct>
                        var elt = t.GetGenericArguments()[0];
                        info.Type = "list";
                        info.ElementType = "struct";
                        info.ElementStruct = elt.Name;
                        pendingStructs.Enqueue(elt);
                    }
                    else
                    {
                        info.Type = "struct";
                        info.StructName = t.Name;
                        pendingStructs.Enqueue(t);
                    }
                    break;

                case EnumOptionAttribute _:
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        // [EnumOption] on a List<TEnum>
                        var elt = t.GetGenericArguments()[0];
                        info.Type = "list";
                        info.ElementType = "enum";
                        info.ElementEnum = elt.Name;
                        RegisterEnum(schema, elt);
                    }
                    else
                    {
                        info.Type = "enum";
                        info.EnumName = t.Name;
                        RegisterEnum(schema, t);
                    }
                    break;

                default:
                    info.Type = "unknown";
                    break;
            }

            return info;
        }

        private static StructInfo BuildStructInfo(Type structType, Queue<Type> pendingStructs, ConfigSchemaData schema)
        {
            var members = new List<StructMemberInfo>();
            string captionMember = null;

            foreach (var field in structType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                members.Add(DescribeStructMember(
                    field.Name,
                    field.FieldType,
                    field.GetCustomAttribute<StructMemberAttribute>()?.Description,
                    pendingStructs,
                    schema));

                if (field.GetCustomAttribute<StructCaptionAttribute>() != null)
                    captionMember = ValidateCaptionMember(structType, field.Name, field.FieldType,
                        field.GetCustomAttribute<StructMemberAttribute>(), captionMember);
            }

            foreach (var prop in structType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead || !prop.CanWrite) continue;
                members.Add(DescribeStructMember(
                    prop.Name,
                    prop.PropertyType,
                    prop.GetCustomAttribute<StructMemberAttribute>()?.Description,
                    pendingStructs,
                    schema));

                if (prop.GetCustomAttribute<StructCaptionAttribute>() != null)
                    captionMember = ValidateCaptionMember(structType, prop.Name, prop.PropertyType,
                        prop.GetCustomAttribute<StructMemberAttribute>(), captionMember);
            }

            return new StructInfo { Members = members, CaptionMember = captionMember };
        }

        /// <summary>
        /// Validates a <see cref="StructCaptionAttribute"/>-marked member:
        /// it must also carry <see cref="StructMemberAttribute"/>, be of type
        /// <c>string</c>, and be the only such member on its struct. Returns
        /// the validated member name.
        /// </summary>
        private static string ValidateCaptionMember(
            Type structType, string memberName, Type memberType,
            StructMemberAttribute structMember, string existingCaptionMember)
        {
            if (existingCaptionMember != null)
                throw new InvalidOperationException(
                    $"{structType.Name} has more than one [StructCaption] member " +
                    $"('{existingCaptionMember}' and '{memberName}'); only one is allowed.");
            if (structMember == null)
                throw new InvalidOperationException(
                    $"{structType.Name}.{memberName} is marked [StructCaption] but lacks [StructMember]; " +
                    "the caption member must also be a serialized struct member so the UI can read its value.");
            if (memberType != typeof(string))
                throw new InvalidOperationException(
                    $"{structType.Name}.{memberName} is marked [StructCaption] but is {memberType.Name}; " +
                    "the caption member must be of type string.");
            return memberName;
        }

        /// <summary>
        /// Describes one struct member. Recognises scalars, enums,
        /// <c>List&lt;T&gt;</c>, <c>Dictionary&lt;K,V&gt;</c> and nested
        /// structs — the same shapes the top-level option attributes cover.
        /// Anything else collapses to <c>"unknown"</c>.
        /// </summary>
        private static StructMemberInfo DescribeStructMember(string name, Type type, string description, Queue<Type> pendingStructs, ConfigSchemaData schema)
        {
            var info = new StructMemberInfo { Name = name, Description = description };
            var typeName = TypeName(type);

            // Scalars (everything that isn't a collection, struct or enum).
            if (typeName != "struct" && typeName != "unknown" && typeName != "enum")
            {
                info.Type = typeName;
                return info;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elt = type.GetGenericArguments()[0];
                info.Type = "list";
                info.ElementType = TypeName(elt);
                if (info.ElementType == "struct")
                {
                    info.ElementStruct = elt.Name;
                    pendingStructs.Enqueue(elt);
                }
                else if (info.ElementType == "enum")
                {
                    info.ElementEnum = elt.Name;
                    RegisterEnum(schema, elt);
                }
                return info;
            }

            var dictArgs = TryGetDictionaryArguments(type);
            if (dictArgs.HasValue)
            {
                info.Type = "dict";
                info.KeyType = TypeName(dictArgs.Value.Key);
                info.ValueType = TypeName(dictArgs.Value.Value);
                if (info.ValueType == "struct")
                {
                    info.ValueStruct = dictArgs.Value.Value.Name;
                    pendingStructs.Enqueue(dictArgs.Value.Value);
                }
                else if (info.ValueType == "enum")
                {
                    info.ValueEnum = dictArgs.Value.Value.Name;
                    RegisterEnum(schema, dictArgs.Value.Value);
                }
                return info;
            }

            if (typeName == "enum")
            {
                info.Type = "enum";
                info.EnumName = type.Name;
                RegisterEnum(schema, type);
                return info;
            }

            if (typeName == "struct")
            {
                info.Type = "struct";
                info.StructName = type.Name;
                pendingStructs.Enqueue(type);
                return info;
            }

            info.Type = "unknown";
            return info;
        }

        private static string TypeName(Type t)
        {
            if (t == typeof(bool)) return "bool";
            if (t == typeof(int)) return "int";
            if (t == typeof(long)) return "long";
            if (t == typeof(float)) return "float";
            if (t == typeof(double)) return "double";
            if (t == typeof(string)) return "string";
            if (t.IsEnum) return "enum";
            if (t.IsValueType && !t.IsPrimitive && !t.IsEnum) return "struct";
            return "unknown";
        }

        /// <summary>
        /// Records <paramref name="enumType"/> in <paramref name="schema"/>'s
        /// enum table (idempotent). Members are listed in the enum's natural
        /// (underlying-value) order; each carries its member name — the value
        /// used in storage and on the wire — and a UI caption that defaults
        /// to the member name unless overridden with
        /// <see cref="EnumCaptionAttribute"/>.
        /// </summary>
        private static void RegisterEnum(ConfigSchemaData schema, Type enumType)
        {
            if (schema.Enums.ContainsKey(enumType.Name)) return;

            var values = new List<EnumValueInfo>();
            foreach (var name in Enum.GetNames(enumType))
            {
                var caption = enumType.GetField(name)
                    ?.GetCustomAttribute<EnumCaptionAttribute>()?.Caption ?? name;
                values.Add(new EnumValueInfo { Name = name, Caption = caption });
            }

            schema.Enums[enumType.Name] = values;
        }

        private static Type GetGenericArgument(Type t, Type expectedGenericDefinition, int index)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == expectedGenericDefinition)
                return t.GetGenericArguments()[index];
            throw new InvalidOperationException($"Type {t} is not a {expectedGenericDefinition}");
        }

        private static (Type Key, Type Value) GetDictionaryArguments(Type t)
        {
            var args = TryGetDictionaryArguments(t);
            if (args.HasValue) return args.Value;
            throw new InvalidOperationException($"Type {t} is not a Dictionary<,>");
        }

        private static (Type Key, Type Value)? TryGetDictionaryArguments(Type t)
        {
            // SerializableDictionary<K,V> inherits Dictionary<K,V>; walk up to
            // find Dictionary<,>'s generic arguments.
            for (var cursor = t; cursor != null; cursor = cursor.BaseType)
            {
                if (cursor.IsGenericType && cursor.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var args = cursor.GetGenericArguments();
                    return (args[0], args[1]);
                }
            }
            return null;
        }
    }

    /// <summary>Root of the schema document embedded in JSON output.</summary>
    public sealed class ConfigSchemaData
    {
        public List<LayoutContainerInfo> Layout { get; set; } = new List<LayoutContainerInfo>();
        public List<ConfigPropertyInfo> Properties { get; set; } = new List<ConfigPropertyInfo>();
        public Dictionary<string, StructInfo> Structs { get; set; } = new Dictionary<string, StructInfo>();

        /// <summary>
        /// Enum definitions referenced by options or struct members, keyed by
        /// enum type name. Each entry lists the members in the enum's natural
        /// (underlying-value) order.
        /// </summary>
        public Dictionary<string, List<EnumValueInfo>> Enums { get; set; } = new Dictionary<string, List<EnumValueInfo>>();
    }

    /// <summary>
    /// Schema description of one user-defined struct used as a configuration
    /// value. <see cref="Members"/> lists its serialized fields and
    /// properties; <see cref="CaptionMember"/> — when set via
    /// <see cref="StructCaptionAttribute"/> on one of the members — is the
    /// name of the string member the UI should display as the row caption in
    /// a <c>List&lt;Struct&gt;</c> editor. <c>null</c> means the UI falls
    /// back to a positional placeholder.
    /// </summary>
    public sealed class StructInfo
    {
        public List<StructMemberInfo> Members { get; set; } = new List<StructMemberInfo>();
        public string CaptionMember { get; set; }
    }

    /// <summary>
    /// One member of an enum used as a configuration value. <see cref="Name"/>
    /// is the member identifier — the value used in XML/JSON storage — and
    /// <see cref="Caption"/> is the UI caption (defaults to <see cref="Name"/>,
    /// overridden per member with <see cref="EnumCaptionAttribute"/>).
    /// </summary>
    public sealed class EnumValueInfo
    {
        public string Name { get; set; }
        public string Caption { get; set; }
    }

    /// <summary>One node of the layout tree.</summary>
    public sealed class LayoutContainerInfo
    {
        public string Kind { get; set; }
        public string Id { get; set; }
        public string Parent { get; set; }
        public string Caption { get; set; }
    }

    /// <summary>
    /// Metadata for a single config option. Fields that don't apply to the
    /// current property type are left null (and are omitted from JSON output
    /// via <c>JsonIgnoreCondition.WhenWritingNull</c>).
    /// </summary>
    public sealed class ConfigPropertyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Parent { get; set; }

        // Numeric constraints (double covers int / long / float / double).
        public double? Min { get; set; }
        public double? Max { get; set; }

        // String constraints
        public int? MaxLength { get; set; }
        public string Pattern { get; set; }

        // List / Dict
        public int? MaxCount { get; set; }
        public string ElementType { get; set; }
        public string ElementStruct { get; set; }
        public string ElementEnum { get; set; }
        public string KeyType { get; set; }
        public string ValueType { get; set; }
        public string ValueStruct { get; set; }
        public string ValueEnum { get; set; }
        public string TreeParentField { get; set; }

        // Struct
        public string StructName { get; set; }

        // Enum (references a definition in ConfigSchemaData.Enums)
        public string EnumName { get; set; }
    }

    /// <summary>
    /// One member of a Struct used as a configuration value. For nested
    /// collections / structs the same metadata fields as
    /// <see cref="ConfigPropertyInfo"/> are filled (and null fields are
    /// omitted from JSON output).
    /// </summary>
    public sealed class StructMemberInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }

        // For list / dict / struct / enum members
        public string ElementType { get; set; }
        public string ElementStruct { get; set; }
        public string ElementEnum { get; set; }
        public string KeyType { get; set; }
        public string ValueType { get; set; }
        public string ValueStruct { get; set; }
        public string ValueEnum { get; set; }
        public string StructName { get; set; }
        public string EnumName { get; set; }
    }
}
