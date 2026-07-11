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
