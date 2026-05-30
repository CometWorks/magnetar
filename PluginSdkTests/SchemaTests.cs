using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PluginSdk.Config;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// Tests for the JSON envelope shape, the layout tree extraction and the
    /// per-option schema metadata used by the external manager app.
    /// </summary>
    public class SchemaTests
    {
        [Fact]
        public void Build_ExtractsLayoutContainersFromClassAttributes()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));

            // 2 tabs + 2 sections + 2 columns declared on TestConfig
            Assert.Equal(6, schema.Layout.Count);

            var general = schema.Layout.Single(c => c.Id == "general");
            Assert.Equal("tab", general.Kind);
            Assert.Null(general.Parent);
            Assert.Equal("General", general.Caption);

            var scalars = schema.Layout.Single(c => c.Id == "scalars");
            Assert.Equal("section", scalars.Kind);
            Assert.Equal("general", scalars.Parent);

            var left = schema.Layout.Single(c => c.Id == "scalars-left");
            Assert.Equal("column", left.Kind);
            Assert.Equal("scalars", left.Parent);
        }

        [Fact]
        public void Build_OptionsCarryParentReferences()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));

            var flag = schema.Properties.Single(p => p.Name == "Flag");
            Assert.Equal("bool", flag.Type);
            Assert.Equal("scalars-left", flag.Parent);

            var integer = schema.Properties.Single(p => p.Name == "Integer");
            Assert.Equal("int", integer.Type);
            Assert.Equal(0, integer.Min);
            Assert.Equal(100, integer.Max);
            Assert.Equal("scalars-left", integer.Parent);

            var longInteger = schema.Properties.Single(p => p.Name == "LongInteger");
            Assert.Equal("long", longInteger.Type);
            // No range set -> Min/Max should be null, not the sentinel
            Assert.Null(longInteger.Min);
            Assert.Null(longInteger.Max);
        }

        [Fact]
        public void Build_DescribesEachCollectionShape()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));

            var stringList = schema.Properties.Single(p => p.Name == "StringList");
            Assert.Equal("list", stringList.Type);
            Assert.Equal("string", stringList.ElementType);
            Assert.Null(stringList.ElementStruct);
            Assert.Null(stringList.TreeParentField);

            var dictLongBool = schema.Properties.Single(p => p.Name == "DictLongBool");
            Assert.Equal("dict", dictLongBool.Type);
            Assert.Equal("long", dictLongBool.KeyType);
            Assert.Equal("bool", dictLongBool.ValueType);

            var dictIntString = schema.Properties.Single(p => p.Name == "DictIntString");
            Assert.Equal("int", dictIntString.KeyType);
            Assert.Equal("string", dictIntString.ValueType);
        }

        [Fact]
        public void Build_StructPropertyReferencesStructDefinition()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));

            var structValue = schema.Properties.Single(p => p.Name == "StructValue");
            Assert.Equal("struct", structValue.Type);
            Assert.Equal(nameof(TestStruct), structValue.StructName);

            Assert.True(schema.Structs.ContainsKey(nameof(TestStruct)));
            var members = schema.Structs[nameof(TestStruct)].Members;
            // 6 fields + 1 property
            Assert.Equal(7, members.Count);
            Assert.Contains(members, m => m.Name == "Flag" && m.Type == "bool");
            Assert.Contains(members, m => m.Name == "Text" && m.Type == "string");
        }

        [Fact]
        public void Build_ListOfStruct_CapturesElementStructName()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));

            var structList = schema.Properties.Single(p => p.Name == "StructList");
            Assert.Equal("list", structList.Type);
            Assert.Equal("struct", structList.ElementType);
            Assert.Equal(nameof(TestStruct), structList.ElementStruct);
        }

        [Fact]
        public void Build_TreeListExposesTreeParentField()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));

            var tree = schema.Properties.Single(p => p.Name == "TreeNodes");
            Assert.Equal("list", tree.Type);
            Assert.Equal("struct", tree.ElementType);
            Assert.Equal(nameof(TreeNode), tree.ElementStruct);
            Assert.Equal("ParentId", tree.TreeParentField);
            Assert.True(schema.Structs.ContainsKey(nameof(TreeNode)));
        }

        [Fact]
        public void Build_NestedStruct_DescribesCollectionAndStructMembers()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));

            Assert.True(schema.Structs.ContainsKey(nameof(NestedStruct)));
            var members = schema.Structs[nameof(NestedStruct)].Members;

            var numbers = members.Single(m => m.Name == "Numbers");
            Assert.Equal("list", numbers.Type);
            Assert.Equal("int", numbers.ElementType);

            var map = members.Single(m => m.Name == "Map");
            Assert.Equal("dict", map.Type);
            Assert.Equal("string", map.KeyType);
            Assert.Equal("double", map.ValueType);

            var inner = members.Single(m => m.Name == "Inner");
            Assert.Equal("struct", inner.Type);
            Assert.Equal(nameof(TestStruct), inner.StructName);
        }

        [Fact]
        public void Build_NestedStruct_TransitivelyRegistersInnerStruct()
        {
            // NestedStruct references TestStruct as a member; even though
            // TestStruct is also referenced from top-level properties here,
            // the recursive struct walk must register it from inside the
            // nested type independently of top-level discovery.
            var schema = ConfigSchema.Build(typeof(TestConfig));
            Assert.True(schema.Structs.ContainsKey(nameof(NestedStruct)));
            Assert.True(schema.Structs.ContainsKey(nameof(TestStruct)));
        }

        [Fact]
        public void Build_EnumProperty_RegistersEnumAndReferencesIt()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));

            var quality = schema.Properties.Single(p => p.Name == "Quality");
            Assert.Equal("enum", quality.Type);
            Assert.Equal(nameof(Quality), quality.EnumName);
            Assert.Equal("scalars-right", quality.Parent);

            Assert.True(schema.Enums.ContainsKey(nameof(Quality)));
        }

        [Fact]
        public void Build_EnumDefinition_ListsMembersInNaturalOrderWithCaptionOverrides()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));
            var values = schema.Enums[nameof(Quality)];

            Assert.Equal(3, values.Count);
            // Natural (underlying-value) order: Low=0, Medium=5, High=10.
            Assert.Equal("Low", values[0].Name);
            Assert.Equal("Low quality", values[0].Caption);
            Assert.Equal("Medium", values[1].Name);
            Assert.Equal("Medium quality", values[1].Caption);
            // High has no [EnumCaption] -> caption falls back to member name.
            Assert.Equal("High", values[2].Name);
            Assert.Equal("High", values[2].Caption);
        }

        [Fact]
        public void Build_ListOfEnum_CapturesElementEnumName()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));

            var qualityList = schema.Properties.Single(p => p.Name == "QualityList");
            Assert.Equal("list", qualityList.Type);
            Assert.Equal("enum", qualityList.ElementType);
            Assert.Equal(nameof(Quality), qualityList.ElementEnum);
            Assert.True(schema.Enums.ContainsKey(nameof(Quality)));
        }

        [Fact]
        public void Build_EnumStructMember_DescribedAsEnumWithName()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));
            var members = schema.Structs[nameof(TestStruct)].Members;

            var quality = members.Single(m => m.Name == "Quality");
            Assert.Equal("enum", quality.Type);
            Assert.Equal(nameof(Quality), quality.EnumName);
            // Reaching TestStruct from the schema walk must also register
            // the enum referenced from inside the struct.
            Assert.True(schema.Enums.ContainsKey(nameof(Quality)));
        }

        [Fact]
        public void Build_StructCaption_EmitsCaptionMemberName()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));
            Assert.Equal("Label", schema.Structs[nameof(TreeNode)].CaptionMember);
        }

        [Fact]
        public void Build_StructWithoutCaption_LeavesCaptionMemberNull()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));
            // TestStruct and NestedStruct do not mark a [StructCaption] member.
            Assert.Null(schema.Structs[nameof(TestStruct)].CaptionMember);
            Assert.Null(schema.Structs[nameof(NestedStruct)].CaptionMember);
        }

        // ---- Negative validation paths for [StructCaption] -----------------
        // Fields are never assigned: schema build throws before any value is read.
#pragma warning disable CS0649
        private struct NonStringCaption
        {
            [StructMember, StructCaption] public int Bad;
        }

        private struct CaptionWithoutStructMember
        {
            [StructMember] public string Good;
            [StructCaption] public string Caption { get; set; }
        }

        private struct TwoCaptions
        {
            [StructMember, StructCaption] public string First;
            [StructMember, StructCaption] public string Second;
        }
#pragma warning restore CS0649

        private class NonStringCaptionConfig : PluginConfig
        {
            private NonStringCaption value;
            [StructOption] public NonStringCaption Value
            {
                get => value;
                set => SetField(ref this.value, value);
            }
        }

        private class CaptionWithoutStructMemberConfig : PluginConfig
        {
            private CaptionWithoutStructMember value;
            [StructOption] public CaptionWithoutStructMember Value
            {
                get => value;
                set => SetField(ref this.value, value);
            }
        }

        private class TwoCaptionsConfig : PluginConfig
        {
            private TwoCaptions value;
            [StructOption] public TwoCaptions Value
            {
                get => value;
                set => SetField(ref this.value, value);
            }
        }

        [Fact]
        public void Build_StructCaption_OnNonStringMember_Throws()
        {
            var ex = Assert.Throws<System.InvalidOperationException>(
                () => ConfigSchema.Build(typeof(NonStringCaptionConfig)));
            Assert.Contains("must be of type string", ex.Message);
        }

        [Fact]
        public void Build_StructCaption_WithoutStructMember_Throws()
        {
            var ex = Assert.Throws<System.InvalidOperationException>(
                () => ConfigSchema.Build(typeof(CaptionWithoutStructMemberConfig)));
            Assert.Contains("lacks [StructMember]", ex.Message);
        }

        [Fact]
        public void Build_StructCaption_DuplicateOnSameStruct_Throws()
        {
            var ex = Assert.Throws<System.InvalidOperationException>(
                () => ConfigSchema.Build(typeof(TwoCaptionsConfig)));
            Assert.Contains("more than one [StructCaption]", ex.Message);
        }
    }

    /// <summary>
    /// Tests that verify the JSON envelope produced by
    /// <see cref="ConfigStorage.SaveJson"/>: shape, presence of all options
    /// (defaults too), a single round-trip via the wire format, and
    /// enum-by-member-name encoding.
    /// </summary>
    public class JsonEnvelopeTests
    {
        [Fact]
        public void SaveJson_ProducesEnvelopeWithSchemaDefaultsAndValues()
        {
            var json = ConfigStorage.SaveJson(new TestConfig { Integer = 5 });
            using var doc = JsonDocument.Parse(json);

            Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
            Assert.True(doc.RootElement.TryGetProperty("schema", out _));
            Assert.True(doc.RootElement.TryGetProperty("defaults", out _));
            Assert.True(doc.RootElement.TryGetProperty("values", out _));
        }

        [Fact]
        public void SaveJson_IncludesAllOptionsEvenAtDefault()
        {
            var json = ConfigStorage.SaveJson(new TestConfig());
            using var doc = JsonDocument.Parse(json);
            var values = doc.RootElement.GetProperty("values");

            // PascalCase property names become camelCase in JSON.
            foreach (var name in new[] {
                "flag", "integer", "longInteger", "floatNumber", "doubleNumber", "text",
                "boolList", "intList", "longList", "floatList", "doubleList", "stringList",
                "dictStringInt", "dictStringString", "dictStringDouble",
                "dictIntString", "dictIntDouble", "dictLongBool", "dictLongLong",
                "quality", "qualityList",
                "structValue", "structList", "treeNodes", "nested",
                "solidColor", "tintColor",
                "uvOffset", "worldOffset", "tileCoord", "gridSize",
                "facing", "spawnPose",
            })
            {
                Assert.True(values.TryGetProperty(name, out _), $"values is missing '{name}'");
            }
        }

        [Fact]
        public void SaveJson_EnumValuesAreSerialisedByMemberName()
        {
            // Enum values must persist as member names, not underlying ints,
            // so renumbering an enum cannot silently change stored values.
            var c = new TestConfig
            {
                Quality = Quality.High,
                QualityList = new List<Quality> { Quality.Low, Quality.Medium },
            };
            var json = ConfigStorage.SaveJson(c);
            using var doc = JsonDocument.Parse(json);
            var values = doc.RootElement.GetProperty("values");

            Assert.Equal("High", values.GetProperty("quality").GetString());

            var listEl = values.GetProperty("qualityList");
            Assert.Equal(2, listEl.GetArrayLength());
            Assert.Equal("Low", listEl[0].GetString());
            Assert.Equal("Medium", listEl[1].GetString());
        }

        [Fact]
        public void SaveJson_DefaultsSectionMirrorsFreshInstance()
        {
            var c = new TestConfig { Integer = 99, Text = "changed" };
            var json = ConfigStorage.SaveJson(c);
            using var doc = JsonDocument.Parse(json);

            var defaults = doc.RootElement.GetProperty("defaults");
            Assert.Equal(0, defaults.GetProperty("integer").GetInt32());
            Assert.Equal("", defaults.GetProperty("text").GetString());

            var values = doc.RootElement.GetProperty("values");
            Assert.Equal(99, values.GetProperty("integer").GetInt32());
            Assert.Equal("changed", values.GetProperty("text").GetString());
        }

        [Fact]
        public void SaveJson_SchemaMatchesConfigSchemaBuild()
        {
            var json = ConfigStorage.SaveJson(new TestConfig());
            using var doc = JsonDocument.Parse(json);

            var schemaEl = doc.RootElement.GetProperty("schema");
            Assert.Equal(6, schemaEl.GetProperty("layout").GetArrayLength());

            var properties = schemaEl.GetProperty("properties");
            Assert.True(properties.GetArrayLength() >= 20);

            // Spot-check: integer carries min/max
            var integerProp = properties.EnumerateArray().Single(p => p.GetProperty("name").GetString() == "Integer");
            Assert.Equal(0, integerProp.GetProperty("min").GetDouble());
            Assert.Equal(100, integerProp.GetProperty("max").GetDouble());

            // structs[TreeNode] is now an object { members, captionMember } —
            // ensure the caption hint travels over the wire as 'captionMember'.
            var treeNode = schemaEl.GetProperty("structs").GetProperty(nameof(TreeNode));
            Assert.Equal("Label", treeNode.GetProperty("captionMember").GetString());
            Assert.True(treeNode.GetProperty("members").GetArrayLength() >= 3);
        }

        [Fact]
        public void LoadJson_AcceptsFlatValuesOnlyDocument()
        {
            // Backward-compat path: a flat values-only document (no envelope).
            var flatJson = "{\"flag\":true,\"integer\":7,\"text\":\"flat\"}";
            var loaded = ConfigStorage.LoadJson<TestConfig>(flatJson);

            Assert.True(loaded.Flag);
            Assert.Equal(7, loaded.Integer);
            Assert.Equal("flat", loaded.Text);
        }
    }

    /// <summary>
    /// Tests that verify the on-disk XML format is sparse: only properties
    /// whose value differs from the default are written, and loading a file
    /// that omits a property leaves it at the constructor's default.
    /// </summary>
    public class SparseXmlTests
    {
        [Fact]
        public void SaveXml_EmptyConfigWritesNoPropertyElements()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sparse-empty-{System.Guid.NewGuid():N}.xml");
            try
            {
                ConfigStorage.SaveXml(new TestConfig(), path);
                var text = System.IO.File.ReadAllText(path);

                // Root element only, no child <Flag>, <Integer>, etc.
                Assert.DoesNotContain("<Flag>", text);
                Assert.DoesNotContain("<Integer>", text);
                Assert.DoesNotContain("<Text>", text);
                Assert.DoesNotContain("<IntList>", text);
                Assert.DoesNotContain("<DictStringInt>", text);
                Assert.DoesNotContain("<StructValue>", text);
                Assert.Contains("<TestConfig", text);
            }
            finally
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }

        [Fact]
        public void SaveXml_WritesOnlyChangedProperties()
        {
            var c = new TestConfig
            {
                Integer = 42,
                Text = "hello",
                IntList = new System.Collections.Generic.List<int> { 1, 2, 3 },
            };
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sparse-some-{System.Guid.NewGuid():N}.xml");
            try
            {
                ConfigStorage.SaveXml(c, path);
                var text = System.IO.File.ReadAllText(path);

                Assert.Contains("<Integer>42</Integer>", text);
                Assert.Contains("<Text>hello</Text>", text);
                Assert.Contains("<IntList>", text);

                // Unchanged properties stay out.
                Assert.DoesNotContain("<Flag>", text);
                Assert.DoesNotContain("<LongInteger>", text);
                Assert.DoesNotContain("<BoolList>", text);
                Assert.DoesNotContain("<DictStringInt>", text);
                Assert.DoesNotContain("<StructValue>", text);
            }
            finally
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }

        [Fact]
        public void LoadXml_MissingPropertyKeepsConstructorDefault()
        {
            // Hand-crafted XML that only mentions Integer; other properties
            // must come from the parameterless ctor.
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sparse-load-{System.Guid.NewGuid():N}.xml");
            System.IO.File.WriteAllText(path,
                "<?xml version=\"1.0\"?>\n<TestConfig><Integer>123</Integer></TestConfig>");
            try
            {
                var loaded = ConfigStorage.LoadXml<TestConfig>(path);
                Assert.Equal(123, loaded.Integer);
                Assert.False(loaded.Flag);
                Assert.Equal("", loaded.Text);
                Assert.Empty(loaded.IntList);
                Assert.Empty(loaded.DictStringInt);
            }
            finally
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }

        [Fact]
        public void SaveXml_StructAtDefaultIsOmitted_BothFieldAndContents()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"sparse-struct-{System.Guid.NewGuid():N}.xml");
            try
            {
                ConfigStorage.SaveXml(new TestConfig { StructValue = new TestStruct { Integer = 7 } }, path);
                var text = System.IO.File.ReadAllText(path);
                Assert.Contains("<StructValue>", text);
                Assert.Contains("<Integer>7</Integer>", text);
            }
            finally
            {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
        }
    }
}
