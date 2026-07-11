using System;
using System.Collections;
using System.IO;
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
    public void Hub_and_mod_edits_round_trip_through_Magnetar_serializers()
    {
        string sharedDll = SharedDllPath();
        if (!File.Exists(sharedDll))
            return; // skipped when Magnetar is not installed

        string dir = Path.Combine(Path.GetTempPath(), "mcinterop2_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            // Enable a hub plugin + a mod in the profile, and add a remote hub +
            // a mod source in sources.xml — all through the tool's own model.
            PluginProfileDocument profile = PluginProfileDocument.Open(dir);
            profile.EnableGitHub("HUB-GUID-1");
            profile.EnableMod(2599830339);
            profile.Save(new AtomicFile());

            PluginSourcesDocument sources = PluginSourcesDocument.Open(dir);
            sources.AddRemoteHub("MagnetarHub", "CometWorks/magnetar-hub", "main");
            sources.AddMod(2599830339, "Some Mod", true);
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

                // Profile: GitHub + Mods populate and Validate() passes.
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

                var modsSet = (IEnumerable)profileType.GetProperty("Mods").GetValue(profileObj);
                Assert.Contains(2599830339UL, System.Linq.Enumerable.Cast<ulong>(modsSet));

                // SourcesConfig: RemoteHub + ModSources deserialize with the right values.
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

                var modSources = (IEnumerable)sourcesType.GetProperty("ModSources").GetValue(sourcesObj);
                bool foundMod = false;
                foreach (object md in modSources)
                {
                    long id = Convert.ToInt64(md.GetType().GetProperty("ID").GetValue(md));
                    if (id == 2599830339)
                        foundMod = true;
                }
                Assert.True(foundMod, "ModSource ID did not round-trip");
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
