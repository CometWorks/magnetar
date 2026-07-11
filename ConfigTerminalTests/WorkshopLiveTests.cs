using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

/// <summary>
/// Exercises the real Steam Workshop endpoints through the resolver's live HTTP
/// transport. Gated behind MAGNETAR_LIVE=1 so the normal test run stays offline
/// and deterministic.
/// </summary>
public class WorkshopLiveTests
{
    private static bool Live => Environment.GetEnvironmentVariable("MAGNETAR_LIVE") == "1";

    [Fact]
    public void Resolves_a_real_workshop_mod_name()
    {
        if (!Live)
            return;

        var resolver = new WorkshopResolver();
        // Water Mod by Jakaria — a long-lived, popular Space Engineers mod.
        Dictionary<long, string> names = resolver.ResolveNames(new long[] { 2200451495 }, out _);
        Assert.True(names.ContainsKey(2200451495), "expected the mod to resolve");
        Assert.Contains("Water", names[2200451495], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Enables_a_hub_plugin_on_a_copy_of_the_installed_instance()
    {
        if (!Live)
            return;

        string liveConfig = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "Magnetar");
        if (!Directory.Exists(Path.Combine(liveConfig, "Sources", "Hubs")))
            return; // no installed instance with a cached hub catalog

        // Work on a throwaway copy so the real instance is never mutated.
        string dir = Path.Combine(Path.GetTempPath(), "mclive_" + Guid.NewGuid().ToString("N"));
        CopyDir(liveConfig, dir);
        try
        {
            var plugins = new MagnetarPlugins(dir, new AtomicFile());
            var catalog = plugins.HubCatalogPlugins();
            Assert.NotEmpty(catalog);

            HubPluginView target = catalog.First();
            Assert.False(target.Enabled);

            plugins.SetHubPluginEnabled(target.Id, true);
            Assert.True(plugins.HubCatalogPlugins().First(p => p.Id == target.Id).Enabled);

            // The real catalog Id (a GUID) lands in Profiles/Current.xml.
            string profile = File.ReadAllText(PluginProfileDocument.PathFor(dir));
            Assert.Contains(target.Id, profile);
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    private static void CopyDir(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (string file in Directory.GetFiles(src))
            File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
        foreach (string sub in Directory.GetDirectories(src))
            CopyDir(sub, Path.Combine(dst, Path.GetFileName(sub)));
    }
}
