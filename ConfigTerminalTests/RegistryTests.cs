using System.Linq;
using Magnetar.ConfigTerminal.Model;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

public class RegistryTests
{
    [Fact]
    public void Ids_are_unique()
    {
        var dupes = OptionRegistry.All.GroupBy(o => o.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        Assert.Empty(dupes);
    }

    [Fact]
    public void XmlNames_are_unique_per_scope()
    {
        foreach (OptionScope scope in new[] { OptionScope.DedicatedRoot, OptionScope.Session })
        {
            var list = OptionRegistry.All.Where(o => o.Scope == scope);
            var dupes = list.GroupBy(o => o.XmlName).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Assert.True(dupes.Count == 0, $"Duplicate XML names in {scope}: {string.Join(",", dupes)}");
        }
    }

    [Fact]
    public void Enum_options_have_choices()
    {
        foreach (OptionDefinition o in OptionRegistry.All.Where(o => o.Kind == OptionKind.Enum))
            Assert.True(o.Choices != null && o.Choices.Length > 0, $"{o.Id} has no choices");
    }

    [Fact]
    public void Keen_typos_are_preserved_verbatim()
    {
        // A test that literally guards the typos so nobody "fixes" them.
        Assert.NotNull(OptionRegistry.All.FirstOrDefault(o => o.XmlName == "AutoRestatTimeInMin"));
        Assert.NotNull(OptionRegistry.All.FirstOrDefault(o => o.XmlName == "AFKTimeountMin"));
    }

    [Fact]
    public void BlockLimits_enum_order_matches_DS()
    {
        OptionDefinition def = OptionRegistry.ById("Session.BlockLimitsEnabled");
        Assert.Equal("PER_FACTION", def.Choices.Single(c => c.Value == 2).XmlName);
        Assert.Equal("PER_PLAYER", def.Choices.Single(c => c.Value == 3).XmlName);
    }

    [Fact]
    public void Online_and_hostility_enums_use_exact_xml_names()
    {
        OptionDefinition online = OptionRegistry.ById("Session.OnlineMode");
        Assert.Equal(new[] { "OFFLINE", "PUBLIC", "FRIENDS", "PRIVATE" },
            online.Choices.OrderBy(c => c.Value).Select(c => c.XmlName).ToArray());

        OptionDefinition host = OptionRegistry.ById("Session.EnvironmentHostility");
        Assert.Equal("CATACLYSM_UNREAL", host.Choices.Single(c => c.Value == 3).XmlName);
    }

    [Fact]
    public void MaxPlayers_is_present_for_new_world_gate()
    {
        Assert.NotNull(OptionRegistry.ById("Session.MaxPlayers"));
    }
}
