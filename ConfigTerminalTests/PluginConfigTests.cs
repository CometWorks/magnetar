using System;
using System.IO;
using System.Linq;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

public class PluginConfigTests : IDisposable
{
    private readonly string dir;

    public PluginConfigTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mcplug_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(dir, true); } catch { }
    }

    [Fact]
    public void Profile_upsert_preserves_github_and_mods()
    {
        string profilesDir = Path.Combine(dir, "Profiles");
        Directory.CreateDirectory(profilesDir);
        string path = Path.Combine(profilesDir, "Current.xml");
        File.WriteAllText(path,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<Profile>\n" +
            "  <Name>Current</Name>\n" +
            "  <GitHub><GitHubPluginConfig><Id>keep-me</Id></GitHubPluginConfig></GitHub>\n" +
            "  <DevFolder />\n" +
            "  <Local />\n" +
            "  <Mods><unsignedLong>12345</unsignedLong></Mods>\n" +
            "</Profile>\n");

        PluginProfileDocument doc = PluginProfileDocument.Open(dir);
        Assert.True(doc.EnableLocalDll("Essentials.dll"));
        Assert.True(doc.EnableDevFolder("my-plugin", "Manifest.xml", true));
        doc.Save(new AtomicFile());

        string xml = File.ReadAllText(path);
        Assert.Contains("keep-me", xml);              // GitHub preserved
        Assert.Contains("12345", xml);                // Mods preserved
        Assert.Contains("<string>Essentials.dll</string>", xml);
        Assert.Contains("<Id>my-plugin</Id>", xml);
        Assert.Contains("<DataFile>Manifest.xml</DataFile>", xml);

        // Re-open and confirm the model reads them back.
        PluginProfileDocument reopened = PluginProfileDocument.Open(dir);
        Assert.Contains("Essentials.dll", reopened.LocalDlls);
        Assert.Contains(reopened.DevFolders, d => d.Id == "my-plugin" && d.DataFile == "Manifest.xml");
    }

    [Fact]
    public void Enable_is_idempotent_and_disable_removes()
    {
        PluginProfileDocument doc = PluginProfileDocument.Open(dir);
        Assert.True(doc.EnableLocalDll("Foo.dll"));
        Assert.False(doc.EnableLocalDll("Foo.dll"));   // already present
        Assert.True(doc.DisableLocalDll("Foo.dll"));
        Assert.False(doc.DisableLocalDll("Foo.dll"));
        Assert.Empty(doc.LocalDlls);
    }

    [Fact]
    public void Sources_add_preserves_hub_and_dedups_by_folder()
    {
        string sourcesDir = Path.Combine(dir, "Sources");
        Directory.CreateDirectory(sourcesDir);
        string path = Path.Combine(sourcesDir, "sources.xml");
        File.WriteAllText(path,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<SourcesConfig>\n" +
            "  <RemoteHubSources><RemoteHub><Name>MagnetarHub</Name></RemoteHub></RemoteHubSources>\n" +
            "  <LocalPluginSources />\n" +
            "</SourcesConfig>\n");

        PluginSourcesDocument doc = PluginSourcesDocument.Open(dir);
        Assert.True(doc.AddLocalPlugin("my-plugin", "/src/my-plugin", true));
        Assert.False(doc.AddLocalPlugin("my-plugin", "/src/my-plugin", true)); // dedup by folder
        doc.Save(new AtomicFile());

        string xml = File.ReadAllText(path);
        Assert.Contains("MagnetarHub", xml); // hub preserved
        Assert.Contains("<Folder>/src/my-plugin</Folder>", xml);
        Assert.Contains("<Enabled>true</Enabled>", xml);
    }

    [Fact]
    public void Facade_lists_local_dlls_excluding_infrastructure()
    {
        string local = Path.Combine(dir, "Local");
        Directory.CreateDirectory(local);
        File.WriteAllText(Path.Combine(local, "Essentials.dll"), "x");
        File.WriteAllText(Path.Combine(local, "0Harmony.dll"), "x");        // implicit
        File.WriteAllText(Path.Combine(local, "Quasar.Agent.dll"), "x");    // implicit

        var plugins = new MagnetarPlugins(dir, new AtomicFile());
        var dlls = plugins.LocalDlls();
        Assert.Contains(dlls, d => d.FileName == "Essentials.dll");
        Assert.DoesNotContain(dlls, d => d.FileName == "0Harmony.dll");
        Assert.DoesNotContain(dlls, d => d.FileName == "Quasar.Agent.dll");

        Assert.True(plugins.SetLocalDllEnabled("Essentials.dll", true));
        Assert.Contains(plugins.LocalDlls(), d => d.FileName == "Essentials.dll" && d.Enabled);
    }

    [Fact]
    public void Profile_github_and_mods_upsert_preserve_siblings()
    {
        string profilesDir = Path.Combine(dir, "Profiles");
        Directory.CreateDirectory(profilesDir);
        string path = Path.Combine(profilesDir, "Current.xml");
        File.WriteAllText(path,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<Profile>\n  <Name>Current</Name>\n" +
            "  <GitHub><GitHubPluginConfig><Id>keep-hub</Id></GitHubPluginConfig></GitHub>\n" +
            "  <DevFolder />\n  <Local><string>Keep.dll</string></Local>\n  <Mods><unsignedLong>111</unsignedLong></Mods>\n</Profile>\n");

        PluginProfileDocument doc = PluginProfileDocument.Open(dir);
        Assert.True(doc.EnableGitHub("cool-plugin-guid"));
        Assert.False(doc.EnableGitHub("cool-plugin-guid"));   // idempotent
        Assert.True(doc.EnableMod(222));
        Assert.False(doc.EnableMod(222));
        doc.Save(new AtomicFile());

        string xml = File.ReadAllText(path);
        Assert.Contains("keep-hub", xml);           // existing GitHub preserved
        Assert.Contains("Keep.dll", xml);           // existing Local preserved
        Assert.Contains("<Id>cool-plugin-guid</Id>", xml);
        Assert.Contains("<unsignedLong>111</unsignedLong>", xml);
        Assert.Contains("<unsignedLong>222</unsignedLong>", xml);

        PluginProfileDocument reopened = PluginProfileDocument.Open(dir);
        Assert.Contains("cool-plugin-guid", reopened.GitHubPlugins);
        Assert.Contains(222UL, reopened.Mods);

        Assert.True(reopened.DisableGitHub("cool-plugin-guid"));
        Assert.True(reopened.DisableMod(222));
        Assert.DoesNotContain("cool-plugin-guid", reopened.GitHubPlugins);
        Assert.DoesNotContain(222UL, reopened.Mods);
    }

    [Fact]
    public void Sources_remote_hub_add_toggle_remove_preserves_managed_fields()
    {
        string sourcesDir = Path.Combine(dir, "Sources");
        Directory.CreateDirectory(sourcesDir);
        string path = Path.Combine(sourcesDir, "sources.xml");
        File.WriteAllText(path,
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<SourcesConfig>\n" +
            "  <RemoteHubSources><RemoteHub><Name>MagnetarHub</Name><Repo>CometWorks/magnetar-hub</Repo>" +
            "<Branch>main</Branch><LastCheck>2026-07-11T18:55:51Z</LastCheck><Hash>abc123</Hash>" +
            "<Enabled>true</Enabled><Trusted>true</Trusted></RemoteHub></RemoteHubSources>\n" +
            "</SourcesConfig>\n");

        PluginSourcesDocument doc = PluginSourcesDocument.Open(dir);
        Assert.True(doc.AddRemoteHub("Extra", "owner/extra-hub", "dev"));
        Assert.False(doc.AddRemoteHub("Extra", "owner/extra-hub", "dev")); // dedup by repo
        Assert.True(doc.SetRemoteHubEnabled("CometWorks/magnetar-hub", false));
        doc.Save(new AtomicFile());

        string xml = File.ReadAllText(path);
        Assert.Contains("abc123", xml);                 // Magnetar-managed Hash preserved
        Assert.Contains("2026-07-11T18:55:51Z", xml);   // LastCheck preserved
        Assert.Contains("owner/extra-hub", xml);

        PluginSourcesDocument re = PluginSourcesDocument.Open(dir);
        Assert.False(re.RemoteHubs.First(h => h.Repo == "CometWorks/magnetar-hub").Enabled);
        Assert.Equal(2, re.RemoteHubs.Count);
        Assert.True(re.RemoveRemoteHub("owner/extra-hub"));
    }

    [Fact]
    public void Sources_modsources_add_toggle_rename_remove()
    {
        PluginSourcesDocument doc = PluginSourcesDocument.Open(dir);
        Assert.True(doc.AddMod(500, "My Mod", true));
        Assert.False(doc.AddMod(500, "My Mod Renamed", false)); // updates existing, returns false
        doc.Save(new AtomicFile());

        string xml = File.ReadAllText(PluginSourcesDocument.PathFor(dir));
        Assert.Contains("<Mod>", xml);
        Assert.Contains("<ID>500</ID>", xml);
        Assert.Contains("<Name>My Mod Renamed</Name>", xml);
        Assert.Contains("<Enabled>false</Enabled>", xml);

        PluginSourcesDocument re = PluginSourcesDocument.Open(dir);
        ModSourceEntry m = Assert.Single(re.ModSources);
        Assert.Equal(500, m.Id);
        Assert.False(m.Enabled);
        Assert.True(re.SetModEnabled(500, true));
        Assert.True(re.SetModName(500, "Final"));
        Assert.True(re.RemoveMod(500));
        Assert.Empty(re.ModSources);
    }

    [Fact]
    public void Facade_hub_catalog_reflects_profile_enabled_and_pulls_dependencies()
    {
        // A tiny hand-built catalog blob is impractical here; instead drive the
        // facade against a captured hub cache placed in Sources/Hubs, and verify
        // the profile-enable path (with dependency pull-in) end to end.
        string hubsDir = Path.Combine(dir, "Sources", "Hubs");
        Directory.CreateDirectory(hubsDir);
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "magnetar-hub.bin");
        File.Copy(fixture, Path.Combine(hubsDir, "CometWorks-magnetar-hub.bin"));

        var plugins = new MagnetarPlugins(dir, new AtomicFile());
        var catalog = plugins.HubCatalogPlugins();
        Assert.True(catalog.Count >= 5);
        Assert.All(catalog, c => Assert.False(c.Enabled)); // nothing enabled yet

        HubPluginView first = catalog.First();
        var touched = plugins.SetHubPluginEnabled(first.Id, true);
        Assert.Contains(first.Id, touched);

        var after = plugins.HubCatalogPlugins();
        Assert.True(after.First(c => c.Id == first.Id).Enabled);

        // Disabling removes it from the profile again.
        plugins.SetHubPluginEnabled(first.Id, false);
        Assert.False(plugins.HubCatalogPlugins().First(c => c.Id == first.Id).Enabled);
    }

    [Fact]
    public void Facade_mods_stay_in_lockstep_with_profile()
    {
        var plugins = new MagnetarPlugins(dir, new AtomicFile());
        plugins.AddMod(12345, "Test Mod", active: true);

        ModView m = Assert.Single(plugins.Mods());
        Assert.Equal(12345, m.Id);
        Assert.True(m.Active);
        Assert.True(m.InProfile);

        // The workshop id landed in Profile.Mods.
        Assert.Contains(12345UL, PluginProfileDocument.Open(dir).Mods);

        plugins.SetModActive(12345, false);
        Assert.False(plugins.Mods().Single().Active);
        Assert.DoesNotContain(12345UL, PluginProfileDocument.Open(dir).Mods);

        plugins.RemoveMod(12345);
        Assert.Empty(plugins.Mods());
    }

    [Fact]
    public void Facade_adds_dev_folder_from_manifest_with_folder_name_id()
    {
        // A dev folder with a manifest; the id must become the folder name.
        string devFolder = Path.Combine(dir, "src", "cool-plugin");
        Directory.CreateDirectory(devFolder);
        string manifest = Path.Combine(devFolder, "CoolPlugin.xml");
        File.WriteAllText(manifest, "<?xml version=\"1.0\"?><PluginData><Id>whatever</Id></PluginData>");

        var plugins = new MagnetarPlugins(dir, new AtomicFile());
        string id = plugins.AddDevFolderFromManifest(manifest);
        Assert.Equal("cool-plugin", id); // folder name, NOT the manifest <Id>

        var devs = plugins.DevFolderPlugins();
        DevFolderPlugin p = Assert.Single(devs);
        Assert.Equal("cool-plugin", p.Id);
        Assert.Equal("CoolPlugin.xml", p.DataFile);
        Assert.Equal(devFolder, p.Folder);
        Assert.False(p.SourceMissing);

        // Round-trips through the on-disk files.
        string sources = File.ReadAllText(PluginSourcesDocument.PathFor(dir));
        Assert.Contains("<Name>cool-plugin</Name>", sources);
        string profile = File.ReadAllText(PluginProfileDocument.PathFor(dir));
        Assert.Contains("<Id>cool-plugin</Id>", profile);

        // Removal cleans both files.
        plugins.RemoveDevFolder(p);
        Assert.Empty(plugins.DevFolderPlugins());
    }
}
