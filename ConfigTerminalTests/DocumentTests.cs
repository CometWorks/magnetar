using System;
using System.IO;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

public class DocumentTests : IDisposable
{
    private readonly string dir;

    public DocumentTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mctests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(dir, true); } catch { }
    }

    [Fact]
    public void Upsert_preserves_unknown_elements()
    {
        string path = Path.Combine(dir, "SpaceEngineers-Dedicated.cfg");
        File.WriteAllText(path,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<MyConfigDedicated>\n  <FutureUnknownField>keepme</FutureUnknownField>\n  <ServerName>old</ServerName>\n</MyConfigDedicated>\n");

        DedicatedConfigDocument doc = DedicatedConfigDocument.Open(path);
        doc.Set(OptionRegistry.ById("Dedicated.ServerName"), "new");
        var writer = new AtomicFile();
        doc.Save(writer);

        string result = File.ReadAllText(path);
        Assert.Contains("FutureUnknownField", result);
        Assert.Contains("keepme", result);
        Assert.Contains("<ServerName>new</ServerName>", result);
        Assert.True(File.Exists(path + ".bak"), "a .bak backup should be created");
    }

    [Fact]
    public void Enum_value_is_normalized_to_xml_name()
    {
        string path = Path.Combine(dir, "SpaceEngineers-Dedicated.cfg");
        DedicatedConfigDocument doc = DedicatedConfigDocument.Open(path);
        OptionDefinition online = OptionRegistry.ById("Session.OnlineMode");

        doc.Set(online, "Public");         // label
        Assert.Equal("PUBLIC", doc.Get(online));
        doc.Set(online, "1");              // int value
        Assert.Equal("PUBLIC", doc.Get(online));
    }

    [Fact]
    public void Unset_removes_the_element_so_the_default_applies()
    {
        string path = Path.Combine(dir, "SpaceEngineers-Dedicated.cfg");
        DedicatedConfigDocument doc = DedicatedConfigDocument.Open(path);
        OptionDefinition def = OptionRegistry.ById("Session.MaxPlayers");

        doc.Set(def, "16");
        Assert.True(doc.IsSet(def));
        doc.Unset(def);
        Assert.False(doc.IsSet(def));
        Assert.Equal(def.Default, doc.Get(def)); // falls back to registry default
    }

    [Fact]
    public void Administrators_serialize_as_unsignedLong_items()
    {
        string path = Path.Combine(dir, "SpaceEngineers-Dedicated.cfg");
        DedicatedConfigDocument doc = DedicatedConfigDocument.Open(path);
        doc.SetAdministrators(new[] { "111", "222" });
        doc.Save(new AtomicFile());

        string xml = File.ReadAllText(path);
        Assert.Contains("<Administrators>", xml);
        Assert.Contains("<unsignedLong>111</unsignedLong>", xml);
        Assert.Equal(new[] { "111", "222" }, doc.Administrators);
    }

    [Fact]
    public void Password_writes_hash_and_salt_and_can_clear()
    {
        string path = Path.Combine(dir, "SpaceEngineers-Dedicated.cfg");
        DedicatedConfigDocument doc = DedicatedConfigDocument.Open(path);
        doc.SetPassword("hunter2");
        Assert.True(doc.HasPassword);
        doc.Save(new AtomicFile());

        string xml = File.ReadAllText(path);
        Assert.Contains("ServerPasswordHash", xml);
        Assert.Contains("ServerPasswordSalt", xml);

        doc.SetPassword(null);
        Assert.False(doc.HasPassword);
    }

    [Fact]
    public void Mods_round_trip_in_load_order_with_sbm_name()
    {
        string path = Path.Combine(dir, "Sandbox_config.sbc");
        WorldConfigDocument doc = WorldConfigDocument.Open(path);
        var mods = new ModList();
        mods.Items.Add(new ModItem { PublishedFileId = 100, FriendlyName = "First" });
        mods.Items.Add(new ModItem { PublishedFileId = 200, FriendlyName = "Second", IsDependency = true });
        doc.WriteMods(mods);
        doc.Save(new AtomicFile());

        string xml = File.ReadAllText(path);
        Assert.Contains("<Name>100.sbm</Name>", xml);
        Assert.Contains("FriendlyName=\"Second\"", xml);
        Assert.True(xml.IndexOf("100.sbm") < xml.IndexOf("200.sbm"), "order preserved");

        ModList read = WorldConfigDocument.Open(path).ReadMods();
        Assert.Equal(2, read.Items.Count);
        Assert.Equal(100UL, read.Items[0].PublishedFileId);
        Assert.True(read.Items[1].IsDependency);
    }
}
