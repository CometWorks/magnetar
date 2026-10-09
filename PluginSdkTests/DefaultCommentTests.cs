using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PluginSdk.Config;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// Config whose defaults and descriptions are awkward inside an XML
    /// comment: dashes, newlines, a multi-line default and a null default.
    /// </summary>
    public class CommentedConfig : PluginConfig
    {
        [IntOption(0, 10, "How many")]
        public int Count
        {
            get;
            set => SetField(ref field, value);
        } = 5;

        [StringOption(description: "Dashes --- in the description")]
        public string Dashes
        {
            get;
            set => SetField(ref field, value);
        } = "a--b---c-";

        [StringOption(description: "Two lines")]
        public string Multiline
        {
            get;
            set => SetField(ref field, value);
        } = "one\r\ntwo";

        [ListOption(description: "Some numbers")]
        public List<int> Numbers
        {
            get;
            set => SetField(ref field, value);
        } = new List<int> { 1, 2 };

        [StringOption]
        public string Missing
        {
            get;
            set => SetField(ref field, value);
        }
    }

    /// <summary><see cref="CommentedConfig"/>'s options with other defaults.</summary>
    public class BlankConfig : PluginConfig
    {
        [IntOption]
        public int Count
        {
            get;
            set => SetField(ref field, value);
        }

        [StringOption]
        public string Dashes
        {
            get;
            set => SetField(ref field, value);
        } = "";

        [StringOption]
        public string Multiline
        {
            get;
            set => SetField(ref field, value);
        } = "";

        [ListOption]
        public List<int> Numbers
        {
            get;
            set => SetField(ref field, value);
        } = new List<int>();

        [StringOption]
        public string Missing
        {
            get;
            set => SetField(ref field, value);
        } = "blank";
    }

    /// <summary>
    /// Each option written to the XML file is preceded by comments with its
    /// description and its default as a commented-out element.
    /// </summary>
    public class DefaultCommentTests
    {
        private static string Save<T>(T config)
            where T : PluginConfig
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                $"default-comments-{System.Guid.NewGuid():N}.xml"
            );
            try
            {
                ConfigStorage.SaveXml(config, path);
                return File.ReadAllText(path);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static T Load<T>(string xml)
            where T : PluginConfig, new()
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                $"default-comments-{System.Guid.NewGuid():N}.xml"
            );
            try
            {
                File.WriteAllText(path, xml);
                return ConfigStorage.LoadXml<T>(path);
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private static string[] Comments(string xml) =>
            XDocument.Parse(xml).Root.Nodes().OfType<XComment>().Select(c => c.Value).ToArray();

        [Fact]
        public void AtDefaults_EveryOptionHasDescriptionAndDefaultComments()
        {
            var comments = Comments(Save(new CommentedConfig()));

            Assert.Equal(
                new[]
                {
                    " How many ",
                    " <Count>5</Count> ",
                    " Dashes - - - in the description ",
                    " <Dashes>a-&#45;b-&#45;-c-</Dashes> ",
                    " Two lines ",
                    " <Multiline>one&#xD;&#xA;two</Multiline> ",
                    " Some numbers ",
                    $"{System.Environment.NewLine}  <Numbers>{System.Environment.NewLine}    <int>1</int>{System.Environment.NewLine}    <int>2</int>{System.Environment.NewLine}  </Numbers>{System.Environment.NewLine}  ",
                    " Missing has no default value ",
                },
                comments
            );
        }

        [Fact]
        public void Overridden_CommentsPrecedeTheElement()
        {
            var root = XDocument.Parse(Save(new CommentedConfig { Count = 7 })).Root;
            var element = root.Element("Count");

            Assert.Equal("7", element.Value);
            var before = element.NodesBeforeSelf().OfType<XComment>().Reverse().Take(2).Reverse();
            Assert.Equal(new[] { " How many ", " <Count>5</Count> " }, before.Select(c => c.Value));
        }

        [Fact]
        public void Options_AreSeparatedByBlankLines()
        {
            var xml = Save(
                new CommentedConfig
                {
                    Count = 7,
                    Numbers = new List<int> { 9 },
                }
            );
            var nl = System.Environment.NewLine;

            Assert.Contains(
                $"<CommentedConfig>{nl}  <!-- How many -->{nl}  <!-- <Count>5</Count> -->{nl}  <Count>7</Count>{nl}{nl}  <!-- Dashes",
                xml
            );
            Assert.Contains(
                $"  <!--{nl}  <Numbers>{nl}    <int>1</int>{nl}    <int>2</int>{nl}  </Numbers>{nl}  -->{nl}"
                    + $"  <Numbers>{nl}    <int>9</int>{nl}  </Numbers>{nl}{nl}",
                xml
            );
            Assert.EndsWith(
                $"  <!-- Missing has no default value -->{nl}{nl}</CommentedConfig>",
                xml
            );
        }

        [Fact]
        public void TestConfig_CommentsDoNotChangeTheLoadedValues()
        {
            var original = new TestConfig
            {
                Integer = 42,
                Text = "x--y\r\nz",
                IntList = new List<int> { 3 },
            };
            var xml = Save(original);
            var loaded = Load<TestConfig>(xml);

            Assert.Equal(42, loaded.Integer);
            Assert.Equal("x--y\r\nz", loaded.Text);
            Assert.Equal(new[] { 3 }, loaded.IntList);
            Assert.Equal(xml, Save(loaded));
        }

        [Fact]
        public void UncommentedDefaults_LoadAsTheDefaults()
        {
            // Uncomment every default in the raw text, like an admin would, and
            // load it into a type with the same options but other defaults.
            var xml = Save(new CommentedConfig());
            xml = Regex
                .Replace(xml, @"<!--\s*(<.*?>)\s*-->", "$1", RegexOptions.Singleline)
                .Replace("CommentedConfig", "BlankConfig");

            var loaded = Load<BlankConfig>(xml);

            var defaults = new CommentedConfig();
            Assert.Equal(defaults.Count, loaded.Count);
            Assert.Equal(defaults.Dashes, loaded.Dashes);
            Assert.Equal(defaults.Multiline, loaded.Multiline);
            Assert.Equal(defaults.Numbers, loaded.Numbers);
            Assert.Equal("blank", loaded.Missing);
        }
    }
}
