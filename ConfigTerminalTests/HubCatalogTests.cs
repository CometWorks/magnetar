using System;
using System.IO;
using System.Linq;
using Magnetar.ConfigTerminal.Model;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

public class HubCatalogTests
{
    private static string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    [Fact]
    public void Parses_real_magnetar_hub_catalog()
    {
        // A real MagnetarHub catalog cache captured from a live instance
        // (Sources/Hubs/CometWorks-magnetar-hub.bin, a protobuf-net PluginData[]).
        var plugins = HubCatalog.ReadFile(FixturePath("magnetar-hub.bin"), "MagnetarHub");

        // The catalog contains many real plugins with sane, non-empty identity.
        Assert.True(plugins.Count >= 5, $"expected several plugins, got {plugins.Count}");
        Assert.All(plugins, p => Assert.False(string.IsNullOrEmpty(p.Id)));
        Assert.All(plugins, p => Assert.False(string.IsNullOrEmpty(p.FriendlyName)));

        // Block Limits is a known GitHub plugin in this hub; verify the fields we
        // browse by are decoded from the protobuf wire correctly.
        HubPluginInfo blockLimits = plugins.FirstOrDefault(p => p.FriendlyName == "Block Limits");
        Assert.NotNull(blockLimits);
        Assert.Equal(HubPluginKind.GitHub, blockLimits.Kind);
        Assert.Equal("CometWorks/block-limits", blockLimits.RepoId);
        Assert.Equal("OwendB", blockLimits.Author);
        Assert.Contains("block limit", blockLimits.Tooltip, System.StringComparison.OrdinalIgnoreCase);
        Assert.Equal("MagnetarHub", blockLimits.SourceLabel);
        // The Id is the stable key stored into Profile.GitHub.
        Assert.False(string.IsNullOrWhiteSpace(blockLimits.Id));
    }

    [Fact]
    public void Missing_file_returns_empty()
    {
        Assert.Empty(HubCatalog.ReadFile(FixturePath("does-not-exist.bin")));
        Assert.Empty(HubCatalog.Parse(null));
        Assert.Empty(HubCatalog.Parse(System.Array.Empty<byte>()));
    }
}
