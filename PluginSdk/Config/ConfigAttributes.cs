using System;
using PluginSdk.Tools;

namespace PluginSdk.Config
{
    /// <summary>
    /// Marker namespace anchor. The individual attribute classes below
    /// annotate a <see cref="PluginConfig"/>-derived class and its public
    /// properties so the server can discover, validate, remotely manage and
    /// lay out each option in an external Web UI.
    ///
    /// <para><b>Layout model</b></para>
    /// <para>
    /// Layout containers (<see cref="SectionAttribute"/>,
    /// <see cref="TabAttribute"/>, <see cref="ColumnAttribute"/>) are
    /// declared on the config class itself and form a tree via
    /// <c>Id</c> / <c>Parent</c> ids. Each option property may set
    /// <see cref="ConfigOptionAttribute.Parent"/> to attach itself to a
    /// container. If no layout containers are declared and no option sets
    /// <c>Parent</c>, the UI falls back to a flat vbox of options in
    /// declaration order — layout is entirely optional.
    /// </para>
    /// </summary>
    public static class ConfigAttributes
    {
        // intentionally empty - aggregator for documentation cross-refs
    }

    /// <summary>
    /// Base class for layout container attributes. Applied to a config class
    /// to declare one node of the layout tree.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public abstract class LayoutContainerAttribute : Attribute
    {
        /// <summary>Unique id of this container within the config class.</summary>
        public string Id { get; }

        /// <summary>Id of the parent container, or <c>null</c> for a root node.</summary>
        public string Parent { get; }

        /// <summary>Human-readable caption shown in the UI.</summary>
        public string Caption { get; }

        protected LayoutContainerAttribute(string id, string parent, string caption)
        {
            Id = id;
            Parent = parent;
            Caption = caption;
        }

        /// <summary>Layout kind: <c>section</c>, <c>tab</c> or <c>column</c>.</summary>
        public abstract string Kind { get; }
    }

    /// <summary>
    /// Groups options inside a captioned section (group box). Sections with
    /// the same parent stack vertically.
    /// </summary>
    public sealed class SectionAttribute : LayoutContainerAttribute
    {
        public SectionAttribute(string id, string parent = null, string caption = null)
            : base(id, parent, caption) { }
        public override string Kind => "section";
    }

    /// <summary>
    /// A tab in a tab container. Tabs sharing the same parent are siblings of
    /// a tab strip — only one is selected at a time.
    /// </summary>
    public sealed class TabAttribute : LayoutContainerAttribute
    {
        public TabAttribute(string id, string parent = null, string caption = null)
            : base(id, parent, caption) { }
        public override string Kind => "tab";
    }

    /// <summary>
    /// A vertical column. Columns sharing the same parent lay out
    /// horizontally side-by-side.
    /// </summary>
    public sealed class ColumnAttribute : LayoutContainerAttribute
    {
        public ColumnAttribute(string id, string parent = null, string caption = null)
            : base(id, parent, caption) { }
        public override string Kind => "column";
    }

    /// <summary>Base class for every config option attribute.</summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public abstract class ConfigOptionAttribute : Attribute
    {
        public string Description { get; }

        /// <summary>
        /// Optional layout container id this option should be placed in.
        /// If <c>null</c> the option appears at the root of the UI tree.
        /// </summary>
        public string Parent { get; set; }

        protected ConfigOptionAttribute(string description)
        {
            Description = description;
        }
    }

    /// <summary>Marks a <c>bool</c> configuration option.</summary>
    public sealed class BoolOptionAttribute : ConfigOptionAttribute
    {
        public BoolOptionAttribute(string description = null) : base(description) { }
    }

    /// <summary>Marks a 32-bit <c>int</c> configuration option with an optional inclusive range.</summary>
    public sealed class IntOptionAttribute : ConfigOptionAttribute
    {
        public int Min { get; }
        public int Max { get; }

        public IntOptionAttribute(int min = int.MinValue, int max = int.MaxValue, string description = null)
            : base(description)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>Marks a 64-bit <c>long</c> configuration option with an optional inclusive range.</summary>
    public sealed class LongOptionAttribute : ConfigOptionAttribute
    {
        public long Min { get; }
        public long Max { get; }

        public LongOptionAttribute(long min = long.MinValue, long max = long.MaxValue, string description = null)
            : base(description)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>Marks a <c>float</c> configuration option with an optional inclusive range.</summary>
    public sealed class FloatOptionAttribute : ConfigOptionAttribute
    {
        public float Min { get; }
        public float Max { get; }

        public FloatOptionAttribute(float min = float.NegativeInfinity, float max = float.PositiveInfinity, string description = null)
            : base(description)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>Marks a <c>double</c> configuration option with an optional inclusive range.</summary>
    public sealed class DoubleOptionAttribute : ConfigOptionAttribute
    {
        public double Min { get; }
        public double Max { get; }

        public DoubleOptionAttribute(double min = double.NegativeInfinity, double max = double.PositiveInfinity, string description = null)
            : base(description)
        {
            Min = min;
            Max = max;
        }
    }

    /// <summary>
    /// Marks a <c>string</c> configuration option. <paramref name="maxLength"/>
    /// of <c>0</c> means unlimited; <paramref name="pattern"/> is an optional
    /// regular expression that the value must fully match.
    /// </summary>
    public sealed class StringOptionAttribute : ConfigOptionAttribute
    {
        public int MaxLength { get; }
        public string Pattern { get; }

        public StringOptionAttribute(int maxLength = 0, string pattern = null, string description = null)
            : base(description)
        {
            MaxLength = maxLength;
            Pattern = pattern;
        }
    }

    /// <summary>
    /// Marks a <c>List&lt;T&gt;</c> configuration option. The element type
    /// <c>T</c> must be one of the supported scalar types, an enum, or a
    /// Struct. <paramref name="maxCount"/> of <c>0</c> means unlimited.
    ///
    /// <para>
    /// When the element type is a Struct, set
    /// <see cref="TreeParentField"/> to the name of a struct member that
    /// points to the id of its parent element — the UI will then render the
    /// list as a tree instead of a flat list.
    /// </para>
    /// <para>
    /// A <c>List&lt;TEnum&gt;</c> may also be declared with
    /// <see cref="EnumOptionAttribute"/>; both produce the same schema, with
    /// <c>EnumOption</c> preferred for clarity.
    /// </para>
    /// </summary>
    public sealed class ListOptionAttribute : ConfigOptionAttribute
    {
        public int MaxCount { get; }

        /// <summary>
        /// Name of a struct member on the element type that references the
        /// parent element's id. Triggers a tree view in the UI when set.
        /// </summary>
        public string TreeParentField { get; set; }

        public ListOptionAttribute(int maxCount = 0, string description = null)
            : base(description)
        {
            MaxCount = maxCount;
        }
    }

    /// <summary>
    /// Marks a <see cref="SerializableDictionary{TKey,TValue}"/> configuration
    /// option. The key type must be <c>string</c>, <c>int</c> or <c>long</c>;
    /// the value type must be one of the supported scalar types.
    /// <paramref name="maxCount"/> of <c>0</c> means unlimited.
    ///
    /// <para>
    /// When the value type is a Struct, set
    /// <see cref="TreeParentField"/> to the name of a struct member that
    /// references another entry's key — the UI will then render the
    /// dictionary as a tree instead of a flat key/value list.
    /// </para>
    /// </summary>
    public sealed class DictOptionAttribute : ConfigOptionAttribute
    {
        public int MaxCount { get; }

        /// <summary>
        /// Name of a struct member on the value type that references the
        /// parent entry's key. Triggers a tree view in the UI when set.
        /// </summary>
        public string TreeParentField { get; set; }

        public DictOptionAttribute(int maxCount = 0, string description = null)
            : base(description)
        {
            MaxCount = maxCount;
        }
    }

    /// <summary>
    /// Marks a configuration option whose type is a user-defined Struct, or a
    /// <c>List&lt;T&gt;</c> of such a Struct. Annotate the struct's public
    /// members with <see cref="StructMemberAttribute"/>.
    /// </summary>
    public sealed class StructOptionAttribute : ConfigOptionAttribute
    {
        public StructOptionAttribute(string description = null) : base(description) { }
    }

    /// <summary>
    /// Marks a configuration option whose type is a user-defined <c>enum</c>,
    /// or a <c>List&lt;TEnum&gt;</c> of such an enum.
    ///
    /// <para>
    /// The value is always stored — in both XML and JSON — as the enum
    /// member's name, never its underlying integer value, so renumbering the
    /// enum does not break existing configs. The schema sent to the UI lists
    /// every member name in the enum's natural (underlying-value) order, along
    /// with a parallel caption for each member. Captions default to the
    /// member name and can be overridden per member with
    /// <see cref="EnumCaptionAttribute"/>.
    /// </para>
    /// </summary>
    public sealed class EnumOptionAttribute : ConfigOptionAttribute
    {
        public EnumOptionAttribute(string description = null) : base(description) { }
    }

    /// <summary>
    /// Color storage form used by <see cref="ColorOptionAttribute"/>. The value
    /// is always stored as RGBA on disk and in memory; the format only selects
    /// whether the UI exposes the alpha slider.
    /// </summary>
    public enum ColorFormat
    {
        /// <summary>Hide the alpha channel in the UI; alpha is forced to 255.</summary>
        Rgb,
        /// <summary>Expose the alpha channel in the UI.</summary>
        Rgba,
    }

    /// <summary>
    /// Marks a <see cref="VRageMath.Color"/> configuration option. The value is
    /// always stored as RGBA (four bytes); <see cref="Format"/> only chooses
    /// whether the UI shows the alpha control.
    /// </summary>
    public sealed class ColorOptionAttribute : ConfigOptionAttribute
    {
        public ColorFormat Format { get; }

        public ColorOptionAttribute(ColorFormat format = ColorFormat.Rgba, string description = null)
            : base(description)
        {
            Format = format;
        }
    }

    /// <summary>Marks a <see cref="VRageMath.Vector2D"/> configuration option.</summary>
    public sealed class Vector2DOptionAttribute : ConfigOptionAttribute
    {
        public Vector2DOptionAttribute(string description = null) : base(description) { }
    }

    /// <summary>Marks a <see cref="VRageMath.Vector3D"/> configuration option.</summary>
    public sealed class Vector3DOptionAttribute : ConfigOptionAttribute
    {
        public Vector3DOptionAttribute(string description = null) : base(description) { }
    }

    /// <summary>Marks a <see cref="VRageMath.Vector2I"/> configuration option.</summary>
    public sealed class Vector2IOptionAttribute : ConfigOptionAttribute
    {
        public Vector2IOptionAttribute(string description = null) : base(description) { }
    }

    /// <summary>Marks a <see cref="VRageMath.Vector3I"/> configuration option.</summary>
    public sealed class Vector3IOptionAttribute : ConfigOptionAttribute
    {
        public Vector3IOptionAttribute(string description = null) : base(description) { }
    }

    /// <summary>
    /// Marks a <see cref="VRageMath.Base6Directions.Direction"/> configuration
    /// option. The value is stored by member name (Forward, Backward, Left,
    /// Right, Up, Down) so storage is independent of the enum's underlying
    /// integer ordering.
    /// </summary>
    public sealed class DirectionOptionAttribute : ConfigOptionAttribute
    {
        public DirectionOptionAttribute(string description = null) : base(description) { }
    }

    /// <summary>
    /// Marks a <see cref="VRage.MyPositionAndOrientation"/> configuration
    /// option. The UI exposes three editors — Position (Vector3D), Forward and
    /// Up (Vector3 float each); the derived <c>Orientation</c> quaternion is
    /// not surfaced.
    /// </summary>
    public sealed class PositionAndOrientationOptionAttribute : ConfigOptionAttribute
    {
        public PositionAndOrientationOptionAttribute(string description = null) : base(description) { }
    }

    /// <summary>
    /// Marks a public field or property inside a Struct that is used as a
    /// configuration value. Carries a description for the UI; constraint
    /// validation for struct members is intentionally simple and
    /// metadata-only.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public sealed class StructMemberAttribute : Attribute
    {
        public string Description { get; }

        public StructMemberAttribute(string description = null)
        {
            Description = description;
        }
    }

    /// <summary>
    /// Overrides the UI caption shown for one member of an enum used as a
    /// configuration value. Applied to the enum field itself (e.g.
    /// <c>[EnumCaption("Low quality")] Low</c>). The member's identifier — not
    /// its caption — remains the value persisted to XML and JSON; this
    /// attribute is metadata only and never affects storage. The naming
    /// mirrors the <c>Caption</c> property on layout containers
    /// (<see cref="LayoutContainerAttribute"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class EnumCaptionAttribute : Attribute
    {
        public string Caption { get; }

        public EnumCaptionAttribute(string caption)
        {
            Caption = caption;
        }
    }

    /// <summary>
    /// Marks one <see cref="StructMemberAttribute"/>-annotated member of a
    /// Struct as the source for the row caption when instances of the struct
    /// appear in a <c>List&lt;Struct&gt;</c> (flat or tree) editor in the UI.
    /// The marked member must also carry <see cref="StructMemberAttribute"/>
    /// and must be of type <c>string</c>; at most one member per struct may
    /// be marked. When absent, the UI falls back to a positional placeholder
    /// for each row.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class StructCaptionAttribute : Attribute
    {
    }
}
