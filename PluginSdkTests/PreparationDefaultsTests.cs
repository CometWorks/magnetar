using System;
using System.IO;
using System.Text.Json;
using Magnetar.Legacy.Preparation;
using PluginSdk.Config;
using Xunit;

namespace PluginSdkTests;

public class PreparationDefaultsTests
{
    public sealed class Config : PluginConfig
    {
        [IntOption(1, 100, "Count")]
        public int Count { get; set; } = 12;
    }
    public sealed class OtherConfig : PluginConfig
    {
        [BoolOption("Enabled")]
        public bool Enabled { get; set; } = true;
    }
    public sealed class Plugin
    {
        public Plugin() => throw new Exception("Plugin constructor must never execute.");
        public Config Config => throw new Exception("Plugin getter must never execute.");
        public OtherConfig Other => throw new Exception("Plugin getter must never execute.");
    }
    public sealed class AmbiguousPlugin { public PluginConfig Config => null; }
    public sealed class PrivatePlugin { private Config Config; }
    public sealed class BrokenConfig : PluginConfig
    {
        public BrokenConfig() => throw new InvalidOperationException("Game unavailable");
    }
    public sealed class BrokenPlugin { public BrokenConfig Config => null; }

    [Fact]
    public void ExportsMultipleCompleteEnvelopesWithoutConstructingOrReadingPlugin()
    {
        var result = ConfigurationDefaults.Export([typeof(Plugin), typeof(Config), typeof(OtherConfig)], [typeof(Plugin)]);
        Assert.Equal(2, result.Count);
        using var document = JsonDocument.Parse(result[typeof(Config).FullName]);
        Assert.True(document.RootElement.TryGetProperty("schema", out _));
        Assert.Equal(12, document.RootElement.GetProperty("defaults").GetProperty("count").GetInt32());
        Assert.Equal(12, document.RootElement.GetProperty("values").GetProperty("count").GetInt32());
    }

    [Fact]
    public void RefusesPrivateUnusedAndBaseTypedConfiguration()
    {
        Assert.Throws<InvalidDataException>(() => ConfigurationDefaults.Export([typeof(Config), typeof(PrivatePlugin)], [typeof(PrivatePlugin)]));
        Assert.Throws<InvalidDataException>(() => ConfigurationDefaults.Export([typeof(AmbiguousPlugin)], [typeof(AmbiguousPlugin)]));
        Assert.Throws<InvalidDataException>(() => ConfigurationDefaults.Export([typeof(Config)], []));
    }

    [Fact]
    public void RefusesDefaultsRequiringGameInitialization()
    {
        var error = Assert.Throws<InvalidDataException>(() => ConfigurationDefaults.Export([typeof(BrokenConfig), typeof(BrokenPlugin)], [typeof(BrokenPlugin)]));
        Assert.Contains("Game unavailable", error.InnerException.Message);
    }
}
