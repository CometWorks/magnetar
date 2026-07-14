using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

/// <summary>
/// Proves a profile written by this tool is accepted by Magnetar's own
/// serializer: loads the deployed Magnetar.Shared.dll, deserializes the produced
/// Current.xml with XmlSerializer(typeof(Profile)) exactly as Magnetar's
/// ProfilesConfig.Load does, and checks Validate() + the enabled sets. Gated on
/// the Shared.dll being present (defaults to the standard Magnetar install).
/// </summary>
public class PluginInteropTests
{
    private static string SharedDllPath()
    {
        string env = Environment.GetEnvironmentVariable("MAGNETAR_SHARED");
        if (!string.IsNullOrEmpty(env))
            return env;
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".local", "share", "Magnetar", "Bin", "Magnetar.Shared.dll");
    }

    [Fact]
    public void Profile_round_trips_through_Magnetar_serializer()
    {
        string sharedDll = SharedDllPath();
        if (!File.Exists(sharedDll))
            return; // skipped when Magnetar is not installed

        string dir = Path.Combine(Path.GetTempPath(), "mcinterop_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            // Produce a profile with our model: a local DLL + a dev-folder plugin,
            // plus a pre-existing GitHub entry to ensure it survives.
            string profilesDir = Path.Combine(dir, "Profiles");
            Directory.CreateDirectory(profilesDir);
            File.WriteAllText(Path.Combine(profilesDir, "Current.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Profile>\n  <Name>Current</Name>\n  <GitHub><GitHubPluginConfig><Id>hub-x</Id></GitHubPluginConfig></GitHub>\n" +
                "  <DevFolder />\n  <Local />\n  <Mods />\n</Profile>\n");

            PluginProfileDocument doc = PluginProfileDocument.Open(dir);
            doc.EnableLocalDll("Essentials.dll");
            doc.EnableDevFolder("my-plugin", "Manifest.xml", true);
            doc.Save(new AtomicFile());

            // Deserialize with Magnetar's own serializer, resolving Shared's deps
            // from its own directory.
            string binDir = Path.GetDirectoryName(sharedDll);
            ResolveEventHandler resolver = (_, e) =>
            {
                string name = new AssemblyName(e.Name).Name;
                string candidate = Path.Combine(binDir, name + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            };
            AppDomain.CurrentDomain.AssemblyResolve += resolver;
            try
            {
                Assembly shared = Assembly.LoadFrom(sharedDll);
                Type profileType = shared.GetType("Pulsar.Shared.Data.Profile");
                Assert.NotNull(profileType);

                var serializer = new XmlSerializer(profileType);
                object profile;
                using (FileStream fs = File.OpenRead(Path.Combine(profilesDir, "Current.xml")))
                    profile = serializer.Deserialize(fs);

                Assert.NotNull(profile);
                bool valid = (bool)profileType.GetMethod("Validate").Invoke(profile, null);
                Assert.True(valid, "Magnetar's Profile.Validate() rejected the tool-written profile");

                var local = (IEnumerable)profileType.GetProperty("Local").GetValue(profile);
                Assert.Contains("Essentials.dll", System.Linq.Enumerable.Cast<string>(local));

                var dev = (IEnumerable)profileType.GetProperty("DevFolder").GetValue(profile);
                bool foundDev = false;
                foreach (object cfg in dev)
                {
                    string id = (string)cfg.GetType().GetProperty("Id").GetValue(cfg);
                    string dataFile = (string)cfg.GetType().GetProperty("DataFile").GetValue(cfg);
                    if (id == "my-plugin" && dataFile == "Manifest.xml")
                        foundDev = true;
                }
                Assert.True(foundDev, "the dev-folder entry did not round-trip through Magnetar's serializer");
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
            }
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void Hub_edits_round_trip_through_Magnetar_serializers()
    {
        string sharedDll = SharedDllPath();
        if (!File.Exists(sharedDll))
            return; // skipped when Magnetar is not installed

        string dir = Path.Combine(Path.GetTempPath(), "mcinterop2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            // Enable a hub plugin in the profile and add a remote hub in
            // sources.xml — all through the tool's own model.
            PluginProfileDocument profile = PluginProfileDocument.Open(dir);
            profile.EnableGitHub("HUB-GUID-1");
            profile.Save(new AtomicFile());

            PluginSourcesDocument sources = PluginSourcesDocument.Open(dir);
            sources.AddRemoteHub("MagnetarHub", "CometWorks/magnetar-hub", "main");
            sources.Save(new AtomicFile());

            string binDir = Path.GetDirectoryName(sharedDll);
            ResolveEventHandler resolver = (_, e) =>
            {
                string name = new AssemblyName(e.Name).Name;
                string candidate = Path.Combine(binDir, name + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            };
            AppDomain.CurrentDomain.AssemblyResolve += resolver;
            try
            {
                Assembly shared = Assembly.LoadFrom(sharedDll);

                // Profile: GitHub populates and Validate() passes (empty Mods is
                // still written by the skeleton, so validation stays green).
                Type profileType = shared.GetType("Pulsar.Shared.Data.Profile");
                object profileObj;
                using (FileStream fs = File.OpenRead(PluginProfileDocument.PathFor(dir)))
                    profileObj = new XmlSerializer(profileType).Deserialize(fs);
                Assert.True((bool)profileType.GetMethod("Validate").Invoke(profileObj, null));

                var github = (IEnumerable)profileType.GetProperty("GitHub").GetValue(profileObj);
                bool foundHub = false;
                foreach (object cfg in github)
                    if ((string)cfg.GetType().GetProperty("Id").GetValue(cfg) == "HUB-GUID-1")
                        foundHub = true;
                Assert.True(foundHub, "GitHub plugin id did not round-trip");

                // SourcesConfig: RemoteHub deserializes with the right values.
                Type sourcesType = shared.GetType("Pulsar.Shared.Config.SourcesConfig");
                Assert.NotNull(sourcesType);
                object sourcesObj;
                using (FileStream fs = File.OpenRead(PluginSourcesDocument.PathFor(dir)))
                    sourcesObj = new XmlSerializer(sourcesType).Deserialize(fs);

                var remoteHubs = (IEnumerable)sourcesType.GetProperty("RemoteHubSources").GetValue(sourcesObj);
                bool foundRepo = false;
                foreach (object h in remoteHubs)
                    if ((string)h.GetType().GetProperty("Repo").GetValue(h) == "CometWorks/magnetar-hub")
                        foundRepo = true;
                Assert.True(foundRepo, "RemoteHub repo did not round-trip");
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
            }
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void Named_profile_loads_through_Magnetar_ProfilesConfig()
    {
        string sharedDll = SharedDllPath();
        if (!File.Exists(sharedDll))
            return; // skipped when Magnetar is not installed

        string dir = Path.Combine(Path.GetTempPath(), "mcprofint_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            // The tool seeds an active set and saves it as a named profile.
            var writer = new AtomicFile();
            PluginProfileDocument current = PluginProfileDocument.Open(dir);
            current.EnableLocalDll("Essentials.dll");
            current.Save(writer);
            var catalog = new ProfileCatalog(dir, writer);
            catalog.SaveCurrentAs("Survival Preset");

            string binDir = Path.GetDirectoryName(sharedDll);
            ResolveEventHandler resolver = (_, e) =>
            {
                string name = new AssemblyName(e.Name).Name;
                string candidate = Path.Combine(binDir, name + ".dll");
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            };
            AppDomain.CurrentDomain.AssemblyResolve += resolver;
            try
            {
                Assembly shared = Assembly.LoadFrom(sharedDll);
                Type cfgType = shared.GetType("Pulsar.Shared.Config.ProfilesConfig");
                Assert.NotNull(cfgType);

                // Magnetar's own loader must discover the named profile and the
                // active Current, both passing Validate() (Load discards invalid ones).
                object config = cfgType.GetMethod("Load").Invoke(null, new object[] { dir });
                var namedProfiles = ((System.Collections.IEnumerable)cfgType.GetProperty("Profiles").GetValue(config))
                    .Cast<object>().ToList();

                Type profileType = shared.GetType("Pulsar.Shared.Data.Profile");
                bool foundPreset = namedProfiles.Any(p =>
                    (string)profileType.GetProperty("Name").GetValue(p) == "Survival Preset");
                Assert.True(foundPreset, "Magnetar's ProfilesConfig.Load did not load the tool-written profile");

                object current2 = cfgType.GetProperty("Current").GetValue(config);
                Assert.NotNull(current2);
                Assert.True((bool)profileType.GetMethod("Validate").Invoke(current2, null));
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
            }
        }
        finally
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}
