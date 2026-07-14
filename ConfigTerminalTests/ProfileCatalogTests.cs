using System;
using System.IO;
using System.Linq;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

public class ProfileCatalogTests : IDisposable
{
    private readonly string dir;
    private readonly AtomicFile writer = new();

    public ProfileCatalogTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mcprof_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(dir, true); } catch { }
    }

    private void SeedCurrent(params string[] localDlls)
    {
        PluginProfileDocument current = PluginProfileDocument.Open(dir);
        foreach (string dll in localDlls)
            current.EnableLocalDll(dll);
        current.Save(writer);
    }

    [Fact]
    public void CleanKey_matches_magnetar_semantics()
    {
        // Invalid filename chars become '-'; valid chars pass through.
        Assert.Equal("My Preset", PluginProfileDocument.CleanKey("My Preset"));
        Assert.Equal("a-b", PluginProfileDocument.CleanKey("a/b"));  // '/' is invalid on every platform
    }

    [Fact]
    public void Save_new_then_load_round_trips_the_enabled_set()
    {
        SeedCurrent("Essentials.dll", "Concealment.dll");
        var catalog = new ProfileCatalog(dir, writer);

        Assert.True(catalog.SaveCurrentAs("Survival"));
        Assert.False(catalog.SaveCurrentAs("Survival"));   // collision → false, no overwrite

        // "Current" is reserved.
        Assert.Throws<InvalidOperationException>(() => catalog.SaveCurrentAs("Current"));

        ProfileInfo saved = Assert.Single(catalog.NamedProfiles());
        Assert.Equal("Survival", saved.Name);
        Assert.True(saved.MatchesActive);                  // it was snapshotted from Current
        Assert.Equal("Survival", catalog.ActiveMatchKey());

        // Change the active set, then load the profile back.
        PluginProfileDocument current = PluginProfileDocument.Open(dir);
        current.DisableLocalDll("Essentials.dll");
        current.Save(writer);
        Assert.Null(catalog.ActiveMatchKey());             // active no longer matches

        catalog.Load("Survival");
        Assert.Contains("Essentials.dll", PluginProfileDocument.Open(dir).LocalDlls);
        Assert.Equal("Current", PluginProfileDocument.Open(dir).Name);   // stays the active profile
        Assert.Equal("Survival", catalog.ActiveMatchKey());
    }

    [Fact]
    public void Update_overwrites_existing_profile_with_active_set()
    {
        SeedCurrent("A.dll");
        var catalog = new ProfileCatalog(dir, writer);
        catalog.SaveCurrentAs("P");

        // Change the active set and update the saved profile.
        PluginProfileDocument current = PluginProfileDocument.Open(dir);
        current.EnableLocalDll("B.dll");
        current.Save(writer);
        catalog.Update("P");

        PluginProfileDocument reloaded = PluginProfileDocument.OpenNamed(dir, "P");
        Assert.Contains("A.dll", reloaded.LocalDlls);
        Assert.Contains("B.dll", reloaded.LocalDlls);
        Assert.Equal("P", reloaded.Name);   // name preserved
    }

    [Fact]
    public void Rename_moves_the_file_and_updates_name()
    {
        SeedCurrent("A.dll");
        var catalog = new ProfileCatalog(dir, writer);
        catalog.SaveCurrentAs("Old");
        Assert.True(catalog.Exists("Old"));

        catalog.Rename("Old", "New Name");
        Assert.False(catalog.Exists("Old"));
        Assert.True(catalog.Exists("New Name"));
        ProfileInfo p = Assert.Single(catalog.NamedProfiles());
        Assert.Equal("New Name", p.Name);
        Assert.Contains("A.dll", PluginProfileDocument.OpenNamed(dir, "New Name").LocalDlls);
    }

    [Fact]
    public void Delete_removes_the_profile_but_not_current()
    {
        SeedCurrent("A.dll");
        var catalog = new ProfileCatalog(dir, writer);
        catalog.SaveCurrentAs("Temp");

        catalog.Delete("Temp");
        Assert.Empty(catalog.NamedProfiles());
        // Current.xml untouched.
        Assert.Contains("A.dll", PluginProfileDocument.Open(dir).LocalDlls);
        Assert.Throws<InvalidOperationException>(() => catalog.Delete("Current"));
    }

    [Fact]
    public void Current_is_never_listed_as_a_profile()
    {
        SeedCurrent("A.dll");
        var catalog = new ProfileCatalog(dir, writer);
        catalog.SaveCurrentAs("Real");
        Assert.DoesNotContain(catalog.NamedProfiles(), p =>
            string.Equals(p.Key, "Current", StringComparison.OrdinalIgnoreCase));
    }
}
