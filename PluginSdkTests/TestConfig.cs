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
        private bool flag;
        private int integer;
        private long longInteger;
        private float floatNumber;
        private double doubleNumber;
        private string text = "";

        // Lists of scalars
        private List<bool> boolList = new List<bool>();
        private List<int> intList = new List<int>();
        private List<long> longList = new List<long>();
        private List<float> floatList = new List<float>();
        private List<double> doubleList = new List<double>();
        private List<string> stringList = new List<string>();

        // Dicts with each allowed key type and scalar value types
        private SerializableDictionary<string, int> dictStringInt = new SerializableDictionary<string, int>();
        private SerializableDictionary<string, string> dictStringString = new SerializableDictionary<string, string>();
        private SerializableDictionary<string, double> dictStringDouble = new SerializableDictionary<string, double>();
        private SerializableDictionary<int, string> dictIntString = new SerializableDictionary<int, string>();
        private SerializableDictionary<int, double> dictIntDouble = new SerializableDictionary<int, double>();
        private SerializableDictionary<long, bool> dictLongBool = new SerializableDictionary<long, bool>();
        private SerializableDictionary<long, long> dictLongLong = new SerializableDictionary<long, long>();

        // Enum scalar and list of enums
        private Quality quality = Quality.Medium;
        private List<Quality> qualityList = new List<Quality>();

        // Struct directly, and list of struct
        private TestStruct structValue;
        private List<TestStruct> structList = new List<TestStruct>();

        // Tree-shaped list of struct (exercises TreeParentField metadata)
        private List<TreeNode> treeNodes = new List<TreeNode>();

        // Nested-collection struct (exercises schema recursion + deep equality)
        private NestedStruct nested = new NestedStruct
        {
            Numbers = new List<int>(),
            Map = new SerializableDictionary<string, double>(),
        };

        [BoolOption("A boolean flag", Parent = "scalars-left")]
        public bool Flag { get => flag; set => SetField(ref flag, value); }

        [IntOption(0, 100, "An int in [0,100]", Parent = "scalars-left")]
        public int Integer { get => integer; set => SetField(ref integer, value); }

        [LongOption(description: "A long", Parent = "scalars-left")]
        public long LongInteger { get => longInteger; set => SetField(ref longInteger, value); }

        [FloatOption(description: "A float", Parent = "scalars-right")]
        public float FloatNumber { get => floatNumber; set => SetField(ref floatNumber, value); }

        [DoubleOption(description: "A double", Parent = "scalars-right")]
        public double DoubleNumber { get => doubleNumber; set => SetField(ref doubleNumber, value); }

        [StringOption(maxLength: 64, description: "Some text", Parent = "scalars-right")]
        public string Text { get => text; set => SetField(ref text, value); }

        [ListOption(description: "List of bool")]
        public List<bool> BoolList { get => boolList; set => SetField(ref boolList, value); }

        [ListOption(description: "List of int")]
        public List<int> IntList { get => intList; set => SetField(ref intList, value); }

        [ListOption(description: "List of long")]
        public List<long> LongList { get => longList; set => SetField(ref longList, value); }

        [ListOption(description: "List of float")]
        public List<float> FloatList { get => floatList; set => SetField(ref floatList, value); }

        [ListOption(description: "List of double")]
        public List<double> DoubleList { get => doubleList; set => SetField(ref doubleList, value); }

        [ListOption(description: "List of string")]
        public List<string> StringList { get => stringList; set => SetField(ref stringList, value); }

        [DictOption(description: "string -> int")]
        public SerializableDictionary<string, int> DictStringInt { get => dictStringInt; set => SetField(ref dictStringInt, value); }

        [DictOption(description: "string -> string")]
        public SerializableDictionary<string, string> DictStringString { get => dictStringString; set => SetField(ref dictStringString, value); }

        [DictOption(description: "string -> double")]
        public SerializableDictionary<string, double> DictStringDouble { get => dictStringDouble; set => SetField(ref dictStringDouble, value); }

        [DictOption(description: "int -> string")]
        public SerializableDictionary<int, string> DictIntString { get => dictIntString; set => SetField(ref dictIntString, value); }

        [DictOption(description: "int -> double")]
        public SerializableDictionary<int, double> DictIntDouble { get => dictIntDouble; set => SetField(ref dictIntDouble, value); }

        [DictOption(description: "long -> bool")]
        public SerializableDictionary<long, bool> DictLongBool { get => dictLongBool; set => SetField(ref dictLongBool, value); }

        [DictOption(description: "long -> long")]
        public SerializableDictionary<long, long> DictLongLong { get => dictLongLong; set => SetField(ref dictLongLong, value); }

        [EnumOption("An enum value", Parent = "scalars-right")]
        public Quality Quality { get => quality; set => SetField(ref quality, value); }

        [EnumOption("List of enums")]
        public List<Quality> QualityList { get => qualityList; set => SetField(ref qualityList, value); }

        [StructOption(description: "A struct value")]
        public TestStruct StructValue { get => structValue; set => SetField(ref structValue, value); }

        [StructOption(description: "List of structs")]
        public List<TestStruct> StructList { get => structList; set => SetField(ref structList, value); }

        [ListOption(description: "Tree of nodes", TreeParentField = nameof(TreeNode.ParentId))]
        public List<TreeNode> TreeNodes { get => treeNodes; set => SetField(ref treeNodes, value); }

        [StructOption(description: "Struct with nested collections")]
        public NestedStruct Nested { get => nested; set => SetField(ref nested, value); }
    }
}
