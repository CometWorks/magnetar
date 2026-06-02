using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using PluginSdk.Tools;

namespace PluginSdk.Config
{
    /// <summary>
    /// Base class for managed plugin configuration. Plugins derive from this
    /// class and declare one public read/write property per option, whose
    /// setter calls <see cref="SetField{T}"/> so <see cref="PropertyChanged"/>
    /// is raised when the value changes. The C# 14 <c>field</c> contextual
    /// keyword refers to the compiler-generated backing field, so no explicit
    /// private field is needed; defaults are set with a property initializer:
    ///
    /// <code>
    /// [IntOption(1, 240, "Ticks per second")]
    /// public int TickRate { get; set =&gt; SetField(ref field, value); } = 60;
    /// </code>
    ///
    /// Each public property must be annotated with the matching attribute
    /// from <see cref="ConfigAttributes"/> so the server can discover,
    /// validate, remotely manage and lay out the option in the Web UI.
    ///
    /// <para><b>Supported property types</b></para>
    /// <list type="bullet">
    ///   <item><description><c>bool</c>, <c>int</c>, <c>long</c>, <c>float</c>,
    ///         <c>double</c>, <c>string</c></description></item>
    ///   <item><description>A user-defined <c>enum</c>. Stored by member name
    ///         (never its integer value) in both XML and JSON. Annotate the
    ///         property with <see cref="EnumOptionAttribute"/>; override member
    ///         captions with <see cref="EnumCaptionAttribute"/>.</description></item>
    ///   <item><description><c>List&lt;T&gt;</c> where <c>T</c> is one of the
    ///         scalar types above, an enum, or a user-defined <i>Struct</i>.</description></item>
    ///   <item><description><see cref="SerializableDictionary{TKey,TValue}"/>
    ///         where <c>TKey</c> is <c>string</c>, <c>int</c> or <c>long</c>,
    ///         and <c>TValue</c> is one of the scalar types above.</description></item>
    ///   <item><description>A user-defined Struct whose public fields and
    ///         properties are the scalar types above, a <c>List&lt;T&gt;</c>
    ///         or <see cref="SerializableDictionary{TKey,TValue}"/> of
    ///         scalars or structs, or another Struct — nesting is allowed.
    ///         Annotate struct members with <see cref="StructMemberAttribute"/>.
    ///         A Struct may be used directly as a value or as the element
    ///         type of a <c>List&lt;T&gt;</c>, but <b>not</b> as a
    ///         Dictionary key.</description></item>
    ///   <item><description>Built-in VRage value types: <c>Color</c> (always
    ///         stored RGBA; the UI variant is selected by
    ///         <see cref="ColorOptionAttribute"/>), <c>Vector2D</c>,
    ///         <c>Vector3D</c>, <c>Vector2I</c>, <c>Vector3I</c>,
    ///         <c>Base6Directions.Direction</c> (stored by member name) and
    ///         <c>MyPositionAndOrientation</c> (only <c>Position</c>,
    ///         <c>Forward</c> and <c>Up</c> are surfaced; the derived
    ///         <c>Orientation</c> quaternion is not).</description></item>
    /// </list>
    ///
    /// <para><b>Layout</b></para>
    /// <para>
    /// Declare <see cref="SectionAttribute"/>, <see cref="TabAttribute"/> or
    /// <see cref="ColumnAttribute"/> on the derived config class to form a
    /// layout tree, and set <see cref="ConfigOptionAttribute.Parent"/> on
    /// each option to attach it to a container. With no layout containers
    /// declared the UI falls back to a vbox of options in declaration order.
    /// </para>
    ///
    /// <para><b>Storage</b></para>
    /// <para>
    /// <see cref="ConfigStorage"/> serialises a derived config:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>XML</b> — local on-disk format. Only
    ///         properties whose current value differs from the default value
    ///         (as produced by the parameterless constructor) are written.
    ///         Defaults are reapplied implicitly when a missing element is
    ///         loaded back into a fresh instance.</description></item>
    ///   <item><description><b>JSON</b> — remote management wire format.
    ///         Includes the full layout schema, the default values and the
    ///         current values; every option is present even when its value
    ///         equals the default. Loading reads only the values section.</description></item>
    /// </list>
    ///
    /// <para><b>Change notification — important</b></para>
    /// <para>
    /// Change notifications are raised by the top-level property setter.
    /// Scalar assignments notify automatically through
    /// <see cref="SetField{T}"/>. Lists, dictionaries and structs cannot
    /// detect in-place mutations of their contents, so after editing the
    /// contents of such an option you MUST call
    /// <see cref="NotifyChanged"/> with that property's name to make the
    /// change visible to XML save, the JSON envelope and listeners.
    /// </para>
    ///
    /// <para><b>Mutation pattern</b></para>
    /// <code>
    /// config.Names.Add("added");
    /// config.NotifyChanged(nameof(config.Names));
    ///
    /// config.Counters["foo"] = 1;
    /// config.NotifyChanged(nameof(config.Counters));
    /// </code>
    ///
    /// <para><b>Nested mutation</b></para>
    /// <para>
    /// When a struct contains a list, dictionary or another struct, mutate
    /// the contents in place at any depth and then raise a single
    /// notification for the top-level property whose value changed.
    /// </para>
    /// <code>
    /// config.Bounds.Tags.Add(42);
    /// config.NotifyChanged(nameof(config.Bounds));
    /// </code>
    /// </summary>
    public abstract class PluginConfig : INotifyPropertyChanged, IXmlSerializable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for <paramref name="propertyName"/>.
        /// Call this after mutating a list, dictionary or struct option in
        /// place, since such in-place changes bypass the property setter and
        /// would otherwise go unobserved by XML save, the JSON envelope and
        /// listeners. Top-level scalar assignments do not need this — their
        /// setter already notifies through <see cref="SetField{T}"/>.
        /// </summary>
        public void NotifyChanged(string propertyName) => OnPropertyChanged(propertyName);

        /// <summary>
        /// Assigns <paramref name="value"/> to <paramref name="field"/> and
        /// raises <see cref="PropertyChanged"/> when the value differs from
        /// the previous one (per <see cref="EqualityComparer{T}.Default"/>).
        /// Returns <c>true</c> if the value was changed.
        /// </summary>
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // ----- IXmlSerializable: sparse "only non-default values" format ----

        XmlSchema IXmlSerializable.GetSchema() => null;

        private static readonly ConcurrentDictionary<(Type Type, string RootName), XmlSerializer> XmlSerializerCache
            = new ConcurrentDictionary<(Type, string), XmlSerializer>();

        private static XmlSerializer GetXmlSerializer(Type type, string rootName)
            => XmlSerializerCache.GetOrAdd((type, rootName), key => new XmlSerializer(key.Type, new XmlRootAttribute(key.RootName)));

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> ConfigPropertyCache
            = new ConcurrentDictionary<Type, PropertyInfo[]>();

        internal static PropertyInfo[] GetConfigProperties(Type type)
            => ConfigPropertyCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                 .Where(p => p.CanRead && p.CanWrite && p.GetCustomAttribute<ConfigOptionAttribute>() != null)
                 .ToArray());

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            var type = GetType();
            var defaults = Activator.CreateInstance(type);

            // Suppress xmlns:xsi / xmlns:xsd on every child element.
            var ns = new XmlSerializerNamespaces();
            ns.Add(string.Empty, string.Empty);

            foreach (var prop in GetConfigProperties(type))
            {
                var value = prop.GetValue(this);
                var defaultValue = prop.GetValue(defaults);
                if (ValuesEqual(value, defaultValue)) continue;

                if (TypeSerialization.IsHandled(prop.PropertyType))
                {
                    TypeSerialization.WriteXml(writer, prop.Name, value);
                    continue;
                }

                var serializer = GetXmlSerializer(prop.PropertyType, prop.Name);
                serializer.Serialize(writer, value, ns);
            }
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            var type = GetType();
            var props = GetConfigProperties(type).ToDictionary(p => p.Name);

            bool isEmpty = reader.IsEmptyElement;
            reader.ReadStartElement();
            if (isEmpty) return;

            reader.MoveToContent();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType == XmlNodeType.Element && props.TryGetValue(reader.Name, out var prop))
                {
                    object value;
                    if (TypeSerialization.IsHandled(prop.PropertyType))
                        value = TypeSerialization.ReadXml(reader, prop.PropertyType);
                    else
                        value = GetXmlSerializer(prop.PropertyType, prop.Name).Deserialize(reader);
                    prop.SetValue(this, value);
                }
                else
                {
                    reader.Skip();
                }
                reader.MoveToContent();
            }
            reader.ReadEndElement();
        }

        /// <summary>
        /// Deep value equality for the property types supported by
        /// <see cref="PluginConfig"/>. Drives the "skip default values" rule
        /// in XML serialization, so that a populated list with the same
        /// contents as the default empty list compares equal correctly, and
        /// a struct whose members include a list or dict can still match
        /// its default.
        /// </summary>
        internal static bool ValuesEqual(object a, object b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;

            // Order matters: SerializableDictionary implements IDictionary
            // (via Dictionary<,>) but not IList. Check dictionary first.
            if (a is IDictionary dictA && b is IDictionary dictB)
            {
                if (dictA.Count != dictB.Count) return false;
                foreach (var key in dictA.Keys)
                {
                    if (!dictB.Contains(key)) return false;
                    if (!ValuesEqual(dictA[key], dictB[key])) return false;
                }
                return true;
            }

            if (a is IList listA && b is IList listB)
            {
                if (listA.Count != listB.Count) return false;
                for (int i = 0; i < listA.Count; i++)
                    if (!ValuesEqual(listA[i], listB[i])) return false;
                return true;
            }

            // User-defined struct: deep-compare field by field so a struct
            // whose members include a list/dict can match its default even
            // when the collection references differ. Scalars, primitives,
            // strings, and known BCL value types (decimal, DateTime, Guid,
            // ...) fall through to their own Equals.
            var ta = a.GetType();
            if (ta == b.GetType() && IsUserStructType(ta))
            {
                foreach (var field in ta.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    if (!ValuesEqual(field.GetValue(a), field.GetValue(b))) return false;
                foreach (var prop in ta.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
                    if (!ValuesEqual(prop.GetValue(a), prop.GetValue(b))) return false;
                }
                return true;
            }

            return a.Equals(b);
        }

        private static bool IsUserStructType(Type t)
        {
            if (!t.IsValueType || t.IsPrimitive || t.IsEnum) return false;
            if (t.IsGenericType) return false;                 // KeyValuePair<,>, Nullable<>, ...
            if (t == typeof(decimal) || t == typeof(DateTime)
                || t == typeof(DateTimeOffset) || t == typeof(TimeSpan)
                || t == typeof(Guid))
                return false;
            // VRage value types (Color, Vector2D, ...) implement IEquatable<T>
            // already — skip the field-by-field walk, which would also confuse
            // itself on Color's aliased X/R, Y/G, Z/B properties.
            if (TypeSerialization.IsHandled(t)) return false;
            return true;
        }
    }
}
