using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginSdk.Config;
using PluginSdk.Tools;
using VRage;
using VRageMath;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// XML and JSON round-trip tests covering every type combination
    /// supported by <see cref="PluginConfig"/>: scalars, enums, lists of
    /// scalars and enums, dictionaries with each allowed key type, structs
    /// (including with enum members), and lists of structs.
    /// </summary>
    public class SerializationTests
    {
        private static TestConfig MakePopulatedConfig()
        {
            return new TestConfig
            {
                Flag = true,
                Integer = 42,
                LongInteger = 1L << 40,
                FloatNumber = 1.5f,
                DoubleNumber = 2.71828,
                Text = "hello",

                BoolList = new List<bool> { true, false, true },
                IntList = new List<int> { -1, 0, 1, 2, 3 },
                LongList = new List<long> { -9_000_000_000L, 9_000_000_000L },
                FloatList = new List<float> { 0.1f, 0.2f },
                DoubleList = new List<double> { 1.1, 2.2, 3.3 },
                StringList = new List<string> { "a", "b", "c" },

                DictStringInt = new SerializableDictionary<string, int>
                {
                    ["one"] = 1,
                    ["two"] = 2,
                },
                DictStringString = new SerializableDictionary<string, string>
                {
                    ["greeting"] = "hello",
                },
                DictStringDouble = new SerializableDictionary<string, double>
                {
                    ["pi"] = 3.14,
                    ["e"] = 2.72,
                },
                DictIntString = new SerializableDictionary<int, string>
                {
                    [1] = "one",
                    [2] = "two",
                },
                DictIntDouble = new SerializableDictionary<int, double>
                {
                    [10] = 10.5,
                },
                DictLongBool = new SerializableDictionary<long, bool>
                {
                    [1L << 33] = true,
                    [1L << 34] = false,
                },
                DictLongLong = new SerializableDictionary<long, long>
                {
                    [100L] = 200L,
                },

                Quality = Quality.High,
                QualityList = new List<Quality> { Quality.Low, Quality.High, Quality.Medium },

                StructValue = new TestStruct
                {
                    Flag = true,
                    Integer = 7,
                    LongInteger = 1L << 35,
                    FloatNumber = 0.5f,
                    DoubleNumber = 1.5,
                    Text = "nested",
                    Quality = Quality.Medium,
                },

                StructList = new List<TestStruct>
                {
                    new TestStruct { Flag = true,  Integer = 1, Text = "first"  },
                    new TestStruct { Flag = false, Integer = 2, Text = "second" },
                },

                TreeNodes = new List<TreeNode>
                {
                    new TreeNode { Id = 1, ParentId = 0, Label = "root"   },
                    new TreeNode { Id = 2, ParentId = 1, Label = "child"  },
                    new TreeNode { Id = 3, ParentId = 1, Label = "child2" },
                },

                Nested = new NestedStruct
                {
                    Name = "outer",
                    Numbers = new List<int> { 10, 20, 30 },
                    Map = new SerializableDictionary<string, double> { ["k"] = 1.5 },
                    Inner = new TestStruct { Integer = 9, Text = "inner" },
                },

                SolidColor = new Color((byte)200, (byte)100, (byte)50, (byte)255),
                TintColor = new Color((byte)16, (byte)32, (byte)64, (byte)200),
                UvOffset = new Vector2D(0.125, -0.5),
                WorldOffset = new Vector3D(-100.25, 200.5, 300.75),
                TileCoord = new Vector2I(-7, 13),
                GridSize = new Vector3I(5, 5, 5),
                Facing = Base6Directions.Direction.Backward,
                SpawnPose = new MyPositionAndOrientation(
                    new Vector3D(1000.5, 2000.25, 3000.125),
                    Vector3.Right,
                    Vector3.Up),
            };
        }

        private static void AssertEqual(TestConfig expected, TestConfig actual)
        {
            Assert.Equal(expected.Flag, actual.Flag);
            Assert.Equal(expected.Integer, actual.Integer);
            Assert.Equal(expected.LongInteger, actual.LongInteger);
            Assert.Equal(expected.FloatNumber, actual.FloatNumber);
            Assert.Equal(expected.DoubleNumber, actual.DoubleNumber);
            Assert.Equal(expected.Text, actual.Text);

            Assert.Equal(expected.BoolList, actual.BoolList);
            Assert.Equal(expected.IntList, actual.IntList);
            Assert.Equal(expected.LongList, actual.LongList);
            Assert.Equal(expected.FloatList, actual.FloatList);
            Assert.Equal(expected.DoubleList, actual.DoubleList);
            Assert.Equal(expected.StringList, actual.StringList);

            Assert.Equal(expected.DictStringInt, actual.DictStringInt);
            Assert.Equal(expected.DictStringString, actual.DictStringString);
            Assert.Equal(expected.DictStringDouble, actual.DictStringDouble);
            Assert.Equal(expected.DictIntString, actual.DictIntString);
            Assert.Equal(expected.DictIntDouble, actual.DictIntDouble);
            Assert.Equal(expected.DictLongBool, actual.DictLongBool);
            Assert.Equal(expected.DictLongLong, actual.DictLongLong);

            Assert.Equal(expected.Quality, actual.Quality);
            Assert.Equal(expected.QualityList, actual.QualityList);

            Assert.Equal(expected.StructValue.Flag, actual.StructValue.Flag);
            Assert.Equal(expected.StructValue.Integer, actual.StructValue.Integer);
            Assert.Equal(expected.StructValue.LongInteger, actual.StructValue.LongInteger);
            Assert.Equal(expected.StructValue.FloatNumber, actual.StructValue.FloatNumber);
            Assert.Equal(expected.StructValue.DoubleNumber, actual.StructValue.DoubleNumber);
            Assert.Equal(expected.StructValue.Text, actual.StructValue.Text);
            Assert.Equal(expected.StructValue.Quality, actual.StructValue.Quality);

            Assert.Equal(expected.StructList.Count, actual.StructList.Count);
            for (int i = 0; i < expected.StructList.Count; i++)
            {
                Assert.Equal(expected.StructList[i].Flag, actual.StructList[i].Flag);
                Assert.Equal(expected.StructList[i].Integer, actual.StructList[i].Integer);
                Assert.Equal(expected.StructList[i].LongInteger, actual.StructList[i].LongInteger);
                Assert.Equal(expected.StructList[i].FloatNumber, actual.StructList[i].FloatNumber);
                Assert.Equal(expected.StructList[i].DoubleNumber, actual.StructList[i].DoubleNumber);
                Assert.Equal(expected.StructList[i].Text, actual.StructList[i].Text);
            }

            Assert.Equal(expected.TreeNodes.Count, actual.TreeNodes.Count);
            for (int i = 0; i < expected.TreeNodes.Count; i++)
            {
                Assert.Equal(expected.TreeNodes[i].Id, actual.TreeNodes[i].Id);
                Assert.Equal(expected.TreeNodes[i].ParentId, actual.TreeNodes[i].ParentId);
                Assert.Equal(expected.TreeNodes[i].Label, actual.TreeNodes[i].Label);
            }

            Assert.Equal(expected.Nested.Name, actual.Nested.Name);
            Assert.Equal(expected.Nested.Numbers, actual.Nested.Numbers);
            Assert.Equal(expected.Nested.Map, actual.Nested.Map);
            Assert.Equal(expected.Nested.Inner.Integer, actual.Nested.Inner.Integer);
            Assert.Equal(expected.Nested.Inner.Text, actual.Nested.Inner.Text);

            Assert.Equal(expected.SolidColor, actual.SolidColor);
            Assert.Equal(expected.TintColor, actual.TintColor);
            Assert.Equal(expected.UvOffset, actual.UvOffset);
            Assert.Equal(expected.WorldOffset, actual.WorldOffset);
            Assert.Equal(expected.TileCoord, actual.TileCoord);
            Assert.Equal(expected.GridSize, actual.GridSize);
            Assert.Equal(expected.Facing, actual.Facing);
            Assert.Equal(expected.SpawnPose.Position, actual.SpawnPose.Position);
            Assert.Equal(expected.SpawnPose.Forward, actual.SpawnPose.Forward);
            Assert.Equal(expected.SpawnPose.Up, actual.SpawnPose.Up);
        }

        [Fact]
        public void Xml_RoundTrip_PreservesAllValues()
        {
            var original = MakePopulatedConfig();
            var path = Path.Combine(Path.GetTempPath(), $"magnetar-xml-{System.Guid.NewGuid():N}.xml");
            try
            {
                ConfigStorage.SaveXml(original, path);
                Assert.True(File.Exists(path));
                var loaded = ConfigStorage.LoadXml<TestConfig>(path);
                AssertEqual(original, loaded);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Xml_LoadingMissingFile_ReturnsDefaultInstance()
        {
            var path = Path.Combine(Path.GetTempPath(), $"magnetar-missing-{System.Guid.NewGuid():N}.xml");
            var loaded = ConfigStorage.LoadXml<TestConfig>(path);
            Assert.NotNull(loaded);
            Assert.False(loaded.Flag);
            Assert.Empty(loaded.IntList);
        }

        [Fact]
        public void Xml_SaveWritesAtomicallyViaTempFile()
        {
            var path = Path.Combine(Path.GetTempPath(), $"magnetar-atomic-{System.Guid.NewGuid():N}.xml");
            try
            {
                ConfigStorage.SaveXml(new TestConfig(), path);
                Assert.True(File.Exists(path));
                Assert.False(File.Exists(path + ".tmp"));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Json_RoundTrip_PreservesAllValues()
        {
            var original = MakePopulatedConfig();
            var json = ConfigStorage.SaveJson(original);
            Assert.False(string.IsNullOrWhiteSpace(json));
            var loaded = ConfigStorage.LoadJson<TestConfig>(json);
            AssertEqual(original, loaded);
        }

        [Fact]
        public void Json_RoundTrip_EmptyConfig_PreservesDefaults()
        {
            var original = new TestConfig();
            var json = ConfigStorage.SaveJson(original);
            var loaded = ConfigStorage.LoadJson<TestConfig>(json);
            AssertEqual(original, loaded);
        }

        [Fact]
        public void Xml_EnumValueIsStoredByMemberName()
        {
            // Quality.High has underlying value 10; storing it by name protects
            // against silent breakage if the enum is later renumbered.
            var c = new TestConfig { Quality = Quality.High };
            var path = Path.Combine(Path.GetTempPath(), $"magnetar-enum-{System.Guid.NewGuid():N}.xml");
            try
            {
                ConfigStorage.SaveXml(c, path);
                var text = File.ReadAllText(path);
                Assert.Contains("<Quality>High</Quality>", text);
                Assert.DoesNotContain("<Quality>10</Quality>", text);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Tests that pin down the on-disk and on-wire shape of the VRage value
    /// types — these bypass the generic XmlSerializer and System.Text.Json
    /// paths, so their format is part of the library's contract.
    /// </summary>
    public class TypeSerializationTests
    {
        private static string WriteXml(TestConfig c)
        {
            var path = Path.Combine(Path.GetTempPath(), $"magnetar-se-{System.Guid.NewGuid():N}.xml");
            try
            {
                ConfigStorage.SaveXml(c, path);
                return File.ReadAllText(path);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Xml_Color_UsesHexRgbaFormat()
        {
            var c = new TestConfig { SolidColor = new Color((byte)0xAB, (byte)0xCD, (byte)0xEF, (byte)0xFF) };
            var text = WriteXml(c);
            Assert.Contains("<SolidColor>#ABCDEFFF</SolidColor>", text);
        }

        [Fact]
        public void Xml_Vectors_UseSpaceSeparatedComponents()
        {
            var c = new TestConfig
            {
                UvOffset = new Vector2D(1.5, -2.5),
                WorldOffset = new Vector3D(10, 20, 30),
                TileCoord = new Vector2I(-1, 2),
                GridSize = new Vector3I(3, 4, 5),
            };
            var text = WriteXml(c);
            // The exact double formatting is G17, but the components must be
            // space-separated and in declaration order.
            Assert.Matches(@"<UvOffset>1\.5\S* -2\.5\S*</UvOffset>", text);
            Assert.Matches(@"<WorldOffset>10\S* 20\S* 30\S*</WorldOffset>", text);
            Assert.Contains("<TileCoord>-1 2</TileCoord>", text);
            Assert.Contains("<GridSize>3 4 5</GridSize>", text);
        }

        [Fact]
        public void Xml_Direction_StoredByMemberName()
        {
            var c = new TestConfig { Facing = Base6Directions.Direction.Backward };
            var text = WriteXml(c);
            Assert.Contains("<Facing>Backward</Facing>", text);
        }

        [Fact]
        public void Xml_PositionAndOrientation_HasNestedPositionForwardUpOnly()
        {
            var c = new TestConfig
            {
                SpawnPose = new MyPositionAndOrientation(new Vector3D(1, 2, 3), Vector3.Forward, Vector3.Up),
            };
            var text = WriteXml(c);
            Assert.Contains("<SpawnPose>", text);
            Assert.Contains("<Position>", text);
            Assert.Contains("<Forward>", text);
            Assert.Contains("<Up>", text);
            // The derived Orientation quaternion must never appear.
            Assert.DoesNotContain("<Orientation>", text);
        }

        [Fact]
        public void Xml_DefaultValueIsOmitted()
        {
            // SpawnPose default has a non-zero Forward/Up; setting it back to
            // the default must round-trip as "no element written".
            var defaults = new TestConfig();
            var c = new TestConfig { SpawnPose = defaults.SpawnPose };
            var text = WriteXml(c);
            Assert.DoesNotContain("<SpawnPose>", text);
        }

        [Fact]
        public void Xml_Color_AcceptsBothRgbAndRgbaInputs()
        {
            // Hand-crafted XML: a 6-hex form (no alpha) must default alpha to 255;
            // an 8-hex form must take the alpha verbatim.
            var path = Path.Combine(Path.GetTempPath(), $"magnetar-color-mix-{System.Guid.NewGuid():N}.xml");
            File.WriteAllText(path,
                "<?xml version=\"1.0\"?>\n" +
                "<TestConfig>" +
                "<SolidColor>#102030</SolidColor>" +
                "<TintColor>#10203040</TintColor>" +
                "</TestConfig>");
            try
            {
                var loaded = ConfigStorage.LoadXml<TestConfig>(path);
                Assert.Equal((byte)0x10, loaded.SolidColor.R);
                Assert.Equal((byte)0x20, loaded.SolidColor.G);
                Assert.Equal((byte)0x30, loaded.SolidColor.B);
                Assert.Equal((byte)0xFF, loaded.SolidColor.A);
                Assert.Equal((byte)0x40, loaded.TintColor.A);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Fact]
        public void Json_Color_UsesHexString()
        {
            var c = new TestConfig { SolidColor = new Color((byte)1, (byte)2, (byte)3, (byte)255) };
            var json = ConfigStorage.SaveJson(c);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var solid = doc.RootElement.GetProperty("values").GetProperty("solidColor").GetString();
            Assert.Equal("#010203FF", solid);
        }

        [Fact]
        public void Json_Vector3D_UsesObjectShape()
        {
            var c = new TestConfig { WorldOffset = new Vector3D(1.5, 2.5, 3.5) };
            var json = ConfigStorage.SaveJson(c);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var v = doc.RootElement.GetProperty("values").GetProperty("worldOffset");
            Assert.Equal(1.5, v.GetProperty("x").GetDouble());
            Assert.Equal(2.5, v.GetProperty("y").GetDouble());
            Assert.Equal(3.5, v.GetProperty("z").GetDouble());
        }

        [Fact]
        public void Json_Direction_IsStringMemberName()
        {
            var c = new TestConfig { Facing = Base6Directions.Direction.Left };
            var json = ConfigStorage.SaveJson(c);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal("Left", doc.RootElement.GetProperty("values").GetProperty("facing").GetString());
        }

        [Fact]
        public void Json_Pose_HasOnlyPositionForwardUp()
        {
            var c = new TestConfig
            {
                SpawnPose = new MyPositionAndOrientation(new Vector3D(1, 2, 3), Vector3.Forward, Vector3.Up),
            };
            var json = ConfigStorage.SaveJson(c);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var pose = doc.RootElement.GetProperty("values").GetProperty("spawnPose");
            Assert.True(pose.TryGetProperty("position", out _));
            Assert.True(pose.TryGetProperty("forward", out _));
            Assert.True(pose.TryGetProperty("up", out _));
            Assert.False(pose.TryGetProperty("orientation", out _));
        }

        [Fact]
        public void Schema_ColorOption_ExposesHasAlphaFlag()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));
            var solid = schema.Properties.Single(p => p.Name == nameof(TestConfig.SolidColor));
            var tint  = schema.Properties.Single(p => p.Name == nameof(TestConfig.TintColor));

            Assert.Equal(nameof(Color), solid.Type);
            Assert.False(solid.HasAlpha);
            Assert.Equal(nameof(Color), tint.Type);
            Assert.True(tint.HasAlpha);
        }

        [Fact]
        public void Schema_VectorAndPoseAndDirection_HaveDistinctTypeNames()
        {
            var schema = ConfigSchema.Build(typeof(TestConfig));
            Assert.Equal(nameof(Vector2D),                 schema.Properties.Single(p => p.Name == "UvOffset").Type);
            Assert.Equal(nameof(Vector3D),                 schema.Properties.Single(p => p.Name == "WorldOffset").Type);
            Assert.Equal(nameof(Vector2I),                 schema.Properties.Single(p => p.Name == "TileCoord").Type);
            Assert.Equal(nameof(Vector3I),                 schema.Properties.Single(p => p.Name == "GridSize").Type);
            Assert.Equal(nameof(MyPositionAndOrientation), schema.Properties.Single(p => p.Name == "SpawnPose").Type);

            var dir = schema.Properties.Single(p => p.Name == "Facing");
            Assert.Equal(nameof(Base6Directions.Direction), dir.Type);
            // Direction values travel as enum names — the schema must surface the
            // member list so the UI does not hard-code it.
            Assert.Equal(nameof(Base6Directions.Direction), dir.EnumName);
            Assert.True(schema.Enums.ContainsKey(nameof(Base6Directions.Direction)));
        }
    }
}
