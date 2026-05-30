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
    /// Lists, dictionaries and structs cannot detect in-place mutations of
    /// their contents — to make a change visible to the server you MUST
    /// reassign the whole collection or struct back to the property, even
    /// when only an element, entry or field changed. The default equality
    /// check used by <see cref="SetField{T}"/> is
    /// <see cref="EqualityComparer{T}.Default"/>, which is reference
    /// equality for collections; reassigning a new instance always passes
    /// that check and raises <see cref="PropertyChanged"/>.
    /// </para>
    ///
    /// <para><b>Correct mutation patterns</b></para>
    /// <code>
    /// config.Names = new List&lt;string&gt;(config.Names) { "added" };
    ///
    /// var counters = new SerializableDictionary&lt;string, int&gt;(config.Counters);
    /// counters["foo"] = 1;
    /// config.Counters = counters;
    ///
    /// var bounds = config.Bounds;
    /// bounds.Max = 10;
    /// config.Bounds = bounds;
    /// </code>
    ///
    /// <para><b>Incorrect mutation patterns — silently ignored</b></para>
    /// <code>
    /// config.Names.Add("added");      // mutates in place: no notification
    /// config.Counters["foo"] = 1;     // mutates in place: no notification
    /// </code>
    ///
    /// <para><b>Nested mutation</b></para>
    /// <para>
    /// When a struct contains a list, dictionary or another struct, every
    /// level must be rebuilt and reassigned. Mutating the inner collection
    /// or modifying a nested struct field in place is invisible at every
    /// level: only the top-level property setter raises
    /// <see cref="PropertyChanged"/>.
    /// </para>
    /// <code>
    /// var outer = config.Bounds;                          // copy struct
    /// var inner = new List&lt;int&gt;(outer.Tags) { 42 };  // rebuild list
    /// outer.Tags = inner;                                 // assign back into struct copy
    /// config.Bounds = outer;                              // assign back into property
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
                    var serializer = GetXmlSerializer(prop.PropertyType, prop.Name);
                    var value = serializer.Deserialize(reader);
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
            return true;
        }
    }
}
