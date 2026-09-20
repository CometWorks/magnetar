using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using PluginSdk.Config;
using Xunit;

namespace PluginSdk.Tests
{
    [CollectionDefinition("Managed configuration", DisableParallelization = true)]
    public class ManagedConfigurationCollection { }

    [Collection("Managed configuration")]
    public class ManagedConfigurationTests : IDisposable
    {
        public ManagedConfigurationTests() { Reset(); }
        public void Dispose() { Reset(); }
        private static void Reset()
        {
            var type = typeof(ManagedPluginConfiguration);
            foreach (string name in new[] { "Entries", "Owners", "Instances", "ExpectedInstances" })
            {
                var value = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                value.GetType().GetMethod("Clear").Invoke(value, null);
            }
            var loaded = typeof(ConfigStorage).GetField("LoadedConfigurations", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            loaded.GetType().GetMethod("Clear").Invoke(loaded, null);
            foreach (string name in new[] { "configured", "initialized" })
                type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
            type.GetField("failure", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, null);
        }

        public sealed class SecondConfig : PluginConfig
        {
            [IntOption(0, 100)] public int Number { get; set; }
        }
        private sealed class PrivateMultiConfigPlugin
        {
            private readonly TestConfig first = ConfigStorage.LoadXml<TestConfig>("ignored-first.xml");
            private static SecondConfig second;
            public PrivateMultiConfigPlugin() { second = ConfigStorage.LoadXml<SecondConfig>("ignored-second.xml"); }
            public int Total => first.Integer + second.Number;
            public void ChangeSecond() { second.Number++; }
        }

        private static void WithCanonical(object document, Action test)
        {
            string path = Path.GetTempFileName();
            string previous = Environment.GetEnvironmentVariable("CLUSTER_PLUGIN_CONFIGURATION");
            string previousHash = Environment.GetEnvironmentVariable("CLUSTER_PLUGIN_CONFIGURATION_SHA256");
            try
            {
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(document);
                File.WriteAllBytes(path, bytes);
                Environment.SetEnvironmentVariable("CLUSTER_PLUGIN_CONFIGURATION", path);
                Environment.SetEnvironmentVariable("CLUSTER_PLUGIN_CONFIGURATION_SHA256", Convert.ToHexString(SHA256.HashData(bytes)));
                test();
            }
            finally
            {
                Environment.SetEnvironmentVariable("CLUSTER_PLUGIN_CONFIGURATION", previous);
                Environment.SetEnvironmentVariable("CLUSTER_PLUGIN_CONFIGURATION_SHA256", previousHash);
                File.Delete(path);
            }
        }

        [Fact]
        public void Schema_two_applies_and_observes_private_and_static_configurations()
        {
            string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(TestConfig).Assembly.Location)));
            var first = JsonDocument.Parse(ConfigStorage.SaveJson(new TestConfig { Integer = 42 })).RootElement.Clone();
            var second = JsonDocument.Parse(ConfigStorage.SaveJson(new SecondConfig { Number = 7 })).RootElement.Clone();
            WithCanonical(new { schemaVersion = 2, revision = "two", plugins = new[] { new { id = "ordinary", assemblySha256 = hash,
                configurations = new[] { new { configType = typeof(TestConfig).FullName, configuration = first },
                    new { configType = typeof(SecondConfig).FullName, configuration = second } } } } }, () =>
            {
                ManagedPluginConfiguration.ConfigureFromEnvironment();
                ManagedPluginConfiguration.BindOwner("ordinary", typeof(TestConfig).Assembly);
                ManagedPluginConfiguration.ExpectInstance("ordinary");
                var plugin = new PrivateMultiConfigPlugin();
                Assert.Equal(49, plugin.Total);
                ManagedPluginConfiguration.Initialized("ordinary", plugin);
                ManagedPluginConfiguration.CompleteInitialization();
                Assert.True(ManagedPluginConfiguration.Observe());
                plugin.ChangeSecond();
                Assert.False(ManagedPluginConfiguration.Observe());
            });
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, true)]
        public void Canonical_arrays_require_new_schema_and_unique_types(int schema, bool duplicate)
        {
            var config = new { configType = typeof(TestConfig).FullName,
                configuration = JsonDocument.Parse(ConfigStorage.SaveJson(new TestConfig())).RootElement.Clone() };
            WithCanonical(new { schemaVersion = schema, revision = "bad", plugins = new[] { new { id = "ordinary", assemblySha256 = new string('a', 64),
                configurations = duplicate ? new[] { config, config } : new[] { config } } } }, () =>
                Assert.Throws<InvalidDataException>(() => ManagedPluginConfiguration.ConfigureFromEnvironment()));
        }

        private sealed class OrdinaryPlugin
        {
            public TestConfig PluginConfig { get; } = ConfigStorage.LoadXml<TestConfig>("missing-local-config.xml");
            public int ConstructorValue { get; }
            public OrdinaryPlugin() { ConstructorValue = PluginConfig.Integer; }
        }

        private sealed class FieldPlugin
        {
            public TestConfig Config = ConfigStorage.LoadXml<TestConfig>("ignored.xml");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference LoadDiscarded() => new WeakReference(ConfigStorage.LoadJson<TestConfig>("{}"));

        [Fact]
        public void Standalone_loads_are_discoverable_without_retaining_discarded_configs()
        {
            var local = ConfigStorage.LoadXml<TestConfig>("missing-local-config.xml");
            var json = ConfigStorage.LoadJson<SecondConfig>("{\"Number\":7}");
            var snapshot = ConfigStorage.GetLoadedConfigurations(typeof(TestConfig).Assembly);
            Assert.Contains(local, snapshot);
            Assert.Contains(json, snapshot);
            Assert.Single(snapshot, value => ReferenceEquals(value, local));
            var discarded = LoadDiscarded();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            Assert.False(discarded.IsAlive);
            Assert.Contains(local, ConfigStorage.GetLoadedConfigurations(typeof(TestConfig).Assembly));
        }

        [Theory]
        [InlineData("1", "1.0", true)]
        [InlineData("1e2", "100.00", true)]
        [InlineData("-0", "0.000e99", true)]
        [InlineData("9007199254740992", "9007199254740993", false)]
        [InlineData("1e1000000", "10e999999", true)]
        [InlineData("1e1000000", "2e1000000", false)]
        public void Numeric_comparison_is_exact_and_ignores_format(string left, string right, bool expected)
        {
            var equivalent = typeof(ManagedPluginConfiguration).GetMethod("Equivalent", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.Equal(expected, (bool)equivalent.Invoke(null, new object[] { left, right }));
        }

        [Fact]
        public void Canonical_values_precede_constructor_and_drift_is_sticky()
        {
            string path = Path.GetTempFileName();
            const string variable = "CLUSTER_PLUGIN_CONFIGURATION";
            const string hashVariable = "CLUSTER_PLUGIN_CONFIGURATION_SHA256";
            string previous = Environment.GetEnvironmentVariable(variable);
            string previousHash = Environment.GetEnvironmentVariable(hashVariable);
            try
            {
                var canonical = new TestConfig { Integer = 42 };
                var envelope = JsonDocument.Parse(ConfigStorage.SaveJson(canonical)).RootElement.Clone();
                string assemblyHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(TestConfig).Assembly.Location)));
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { schemaVersion = 1, revision = "revision-1",
                    plugins = new[] { new { id = "ordinary", assemblySha256 = assemblyHash,
                        configType = typeof(TestConfig).FullName, configuration = envelope } } });
                File.WriteAllBytes(path, bytes);
                Environment.SetEnvironmentVariable(variable, path);
                Environment.SetEnvironmentVariable(hashVariable, Convert.ToHexString(SHA256.HashData(bytes)));
                Assert.False(ManagedPluginConfiguration.Observe());
                ManagedPluginConfiguration.ConfigureFromEnvironment();
                Assert.Throws<InvalidOperationException>(() => ConfigStorage.LoadXml<TestConfig>("ignored"));
                ManagedPluginConfiguration.BindOwner("ordinary", typeof(TestConfig).Assembly);
                ManagedPluginConfiguration.ExpectInstance("ordinary");
                var plugin = new OrdinaryPlugin();
                Assert.Equal(42, plugin.ConstructorValue);
                Assert.Equal(42, ConfigStorage.LoadJson<TestConfig>("{}").Integer);
                ManagedPluginConfiguration.Initialized("ordinary", plugin);
                ManagedPluginConfiguration.CompleteInitialization();
                Assert.True(ManagedPluginConfiguration.Observe());
                var fieldPlugin = new FieldPlugin();
                ManagedPluginConfiguration.Initialized("ordinary", fieldPlugin);
                Assert.True(ManagedPluginConfiguration.Observe());
                ConfigStorage.SaveXml(plugin.PluginConfig, path);
                Assert.Equal(bytes, File.ReadAllBytes(path));
                plugin.PluginConfig.Integer = 7;
                Assert.Throws<InvalidDataException>(() => ConfigStorage.SaveXml(plugin.PluginConfig, path));
                Assert.False(ManagedPluginConfiguration.Observe());
                plugin.PluginConfig.Integer = 42;
                Assert.False(ManagedPluginConfiguration.Observe());
                Assert.NotNull(ManagedPluginConfiguration.Failure);
            }
            finally
            {
                Environment.SetEnvironmentVariable(variable, previous);
                Environment.SetEnvironmentVariable(hashVariable, previousHash);
                File.Delete(path);
            }
        }
    }
}
