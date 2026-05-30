using System.Collections.Generic;
using PluginSdk.Config;
using PluginSdk.Tools;

namespace PluginSdk.Tests
{
    /// <summary>
    /// Enum used as a configuration value. The first two members override their
    /// UI caption; <see cref="High"/> falls back to its member name.
    /// Underlying values are non-contiguous to prove storage is by name.
    /// </summary>
    public enum Quality
    {
        [EnumCaption("Low quality")] Low = 0,
        [EnumCaption("Medium quality")] Medium = 5,
        High = 10,
    }

    /// <summary>
    /// Struct value used by <see cref="TestConfig.Point"/> and
    /// <see cref="TestConfig.Points"/>. Exercises every scalar type that may
    /// appear as a struct member, plus an enum member.
    /// </summary>
    public struct TestStruct
    {
        [StructMember] public bool Flag;
        [StructMember] public int Integer;
        [StructMember] public long LongInteger;
        [StructMember] public float FloatNumber;
        [StructMember] public double DoubleNumber;
        [StructMember] public string Text { get; set; }
        [StructMember] public Quality Quality;
    }

    /// <summary>
    /// Element of <see cref="TestConfig.TreeNodes"/>. Carries an <c>Id</c> and
    /// a <c>ParentId</c> so the UI can render the list as a tree.
    /// <see cref="Label"/> is marked <see cref="StructCaptionAttribute"/> so
    /// each row in the UI is labelled by its label string.
    /// </summary>
    public struct TreeNode
    {
        [StructMember] public int Id;
        [StructMember] public int ParentId;
        [StructMember, StructCaption] public string Label { get; set; }
    }

    /// <summary>
    /// Struct with nested collections and a nested struct. Exercises the
    /// "nesting" extensions of <see cref="StructMemberInfo"/> and the deep
    /// struct equality in <see cref="PluginConfig"/>.
    /// </summary>
    public struct NestedStruct
    {
        [StructMember] public string Name;
        [StructMember] public List<int> Numbers;
        [StructMember] public SerializableDictionary<string, double> Map;
        [StructMember] public TestStruct Inner;
    }

    /// <summary>
    /// Concrete <see cref="PluginConfig"/> covering every type combination the
    /// library is required to support. Layout containers are declared at the
    /// class level and individual options reference them via
    /// <see cref="ConfigOptionAttribute.Parent"/>.
    /// </summary>
    [Tab("general", caption: "General")]
    [Tab("advanced", caption: "Advanced")]
    [Section("scalars", parent: "general", caption: "Scalar values")]
    [Section("collections", parent: "advanced", caption: "Collections")]
    [Column("scalars-left", parent: "scalars", caption: "Left")]
    [Column("scalars-right", parent: "scalars", caption: "Right")]
    public class TestConfig : PluginConfig
    {
        // Scalars

        [BoolOption("A boolean flag", Parent = "scalars-left")]
        public bool Flag { get; set => SetField(ref field, value); }

        [IntOption(0, 100, "An int in [0,100]", Parent = "scalars-left")]
        public int Integer { get; set => SetField(ref field, value); }

        [LongOption(description: "A long", Parent = "scalars-left")]
        public long LongInteger { get; set => SetField(ref field, value); }

        [FloatOption(description: "A float", Parent = "scalars-right")]
        public float FloatNumber { get; set => SetField(ref field, value); }

        [DoubleOption(description: "A double", Parent = "scalars-right")]
        public double DoubleNumber { get; set => SetField(ref field, value); }

        [StringOption(maxLength: 64, description: "Some text", Parent = "scalars-right")]
        public string Text { get; set => SetField(ref field, value); } = "";

        // Lists of scalars

        [ListOption(description: "List of bool")]
        public List<bool> BoolList { get; set => SetField(ref field, value); } = new List<bool>();

        [ListOption(description: "List of int")]
        public List<int> IntList { get; set => SetField(ref field, value); } = new List<int>();

        [ListOption(description: "List of long")]
        public List<long> LongList { get; set => SetField(ref field, value); } = new List<long>();

        [ListOption(description: "List of float")]
        public List<float> FloatList { get; set => SetField(ref field, value); } = new List<float>();

        [ListOption(description: "List of double")]
        public List<double> DoubleList { get; set => SetField(ref field, value); } = new List<double>();

        [ListOption(description: "List of string")]
        public List<string> StringList { get; set => SetField(ref field, value); } = new List<string>();

        // Dicts with each allowed key type and scalar value types

        [DictOption(description: "string -> int")]
        public SerializableDictionary<string, int> DictStringInt { get; set => SetField(ref field, value); } = new SerializableDictionary<string, int>();

        [DictOption(description: "string -> string")]
        public SerializableDictionary<string, string> DictStringString { get; set => SetField(ref field, value); } = new SerializableDictionary<string, string>();

        [DictOption(description: "string -> double")]
        public SerializableDictionary<string, double> DictStringDouble { get; set => SetField(ref field, value); } = new SerializableDictionary<string, double>();

        [DictOption(description: "int -> string")]
        public SerializableDictionary<int, string> DictIntString { get; set => SetField(ref field, value); } = new SerializableDictionary<int, string>();

        [DictOption(description: "int -> double")]
        public SerializableDictionary<int, double> DictIntDouble { get; set => SetField(ref field, value); } = new SerializableDictionary<int, double>();

        [DictOption(description: "long -> bool")]
        public SerializableDictionary<long, bool> DictLongBool { get; set => SetField(ref field, value); } = new SerializableDictionary<long, bool>();

        [DictOption(description: "long -> long")]
        public SerializableDictionary<long, long> DictLongLong { get; set => SetField(ref field, value); } = new SerializableDictionary<long, long>();

        // Enum scalar and list of enums

        [EnumOption("An enum value", Parent = "scalars-right")]
        public Quality Quality { get; set => SetField(ref field, value); } = Quality.Medium;

        [EnumOption("List of enums")]
        public List<Quality> QualityList { get; set => SetField(ref field, value); } = new List<Quality>();

        // Struct directly, and list of struct

        [StructOption(description: "A struct value")]
        public TestStruct StructValue { get; set => SetField(ref field, value); }

        [StructOption(description: "List of structs")]
        public List<TestStruct> StructList { get; set => SetField(ref field, value); } = new List<TestStruct>();

        // Tree-shaped list of struct (exercises TreeParentField metadata)

        [ListOption(description: "Tree of nodes", TreeParentField = nameof(TreeNode.ParentId))]
        public List<TreeNode> TreeNodes { get; set => SetField(ref field, value); } = new List<TreeNode>();

        // Nested-collection struct (exercises schema recursion + deep equality)

        [StructOption(description: "Struct with nested collections")]
        public NestedStruct Nested { get; set => SetField(ref field, value); } = new NestedStruct
        {
            Numbers = new List<int>(),
            Map = new SerializableDictionary<string, double>(),
        };
    }
}
