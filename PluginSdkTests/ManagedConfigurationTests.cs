using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using PluginSdk.Config;
using Xunit;

namespace PluginSdk.Tests
{
    [CollectionDefinition("Managed configuration", DisableParallelization = true)]
    public class ManagedConfigurationCollection { }

    [Collection("Managed configuration")]
    public class ManagedConfigurationTests
    {
        private sealed class OrdinaryPlugin
        {
            public TestConfig PluginConfig { get; } = ConfigStorage.LoadXml<TestConfig>("missing-local-config.xml");
            public int ConstructorValue { get; }
            public OrdinaryPlugin() { ConstructorValue = PluginConfig.Integer; }
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
