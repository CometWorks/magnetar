using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using PluginSdk.Config;
using Pulsar.Shared;
using Pulsar.Shared.Assets;
using Pulsar.Shared.Config;
using Pulsar.Shared.Data;
using VRage.Plugins;

namespace Magnetar.Legacy.Preparation;

/// <summary>One-shot compiler/exporter. Never constructs plugins or invokes preload hooks.</summary>
internal static class ManagedPreparation
{
    internal static void Export(string output, string[] corePlugins)
    {
#if NETFRAMEWORK
        throw new PlatformNotSupportedException("Managed preparation requires MagnetarInterim on Linux.");
#else
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("Managed preparation currently targets Linux/CoreCLR.");
        if (ManagedPluginConfiguration.Required || PluginSdk.Clustering.PluginCluster.IsClusterProcess)
            throw new InvalidOperationException("Run preparation outside a managed cluster process.");
        var manager = ConfigManager.Instance;
        var selected = new SortedDictionary<string, PluginData>(StringComparer.Ordinal);
        foreach (string id in manager.Profiles.Current.GetPluginIDs().Concat(corePlugins).Concat(new[] { "direct-transport" }))
            Add(id);
        void Add(string id)
        {
            SafeName(id);
            if (selected.ContainsKey(id)) return;
            if (!manager.List.TryGetPlugin(id, out var data)) throw new InvalidDataException("Selected plugin is unavailable: " + id);
            if (data is ModPlugin) throw new InvalidDataException("Managed preparation does not export Workshop mods: " + id);
            if (id is "cluster-node" or "cluster-wa") throw new InvalidDataException("Cluster role plugins belong to the cluster runtime package: " + id);
            selected.Add(id, data);
            foreach (string dependency in data.DependencyIds ?? []) Add(dependency);
        }

        var loaded = new List<(PluginData Data, Assembly Assembly, GitHubPlugin Metadata)>();
        foreach (var data in selected.Values)
        {
            GitHubPlugin metadata = ReadMetadata(data, manager.Profiles.Current);
            if (!data.TryLoadAssembly(out var assembly)) throw new InvalidDataException("Failed to prepare plugin: " + data.Id);
            if (loaded.Any(item => item.Assembly == assembly)) throw new InvalidDataException("Ambiguous assembly ownership: " + data.Id);
            loaded.Add((data, assembly, metadata));
        }
        foreach (var item in loaded.Where(item => item.Data is LocalPlugin))
            if (loaded.Any(other => other.Data.Id != item.Data.Id
                    && Path.GetDirectoryName(other.Assembly.Location) == Path.GetDirectoryName(item.Assembly.Location)))
                throw new InvalidDataException("Local plugins require separate bundle directories: " + item.Data.Id);
        string outputParent = Path.GetDirectoryName(output);
        foreach (string source in loaded.Select(item => Path.GetDirectoryName(item.Assembly.Location))
                     .Concat(loaded.SelectMany(item => item.Data.GetNamedAssets().Values).Where(Directory.Exists)))
        {
            string fullSource = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
            if (outputParent == fullSource || outputParent.StartsWith(fullSource + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidDataException("Preparation output must be outside plugin and asset source directories.");
        }

        // Publish only after every file, schema and default was captured successfully.
        string staging = output + ".preparing-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(staging);
        try
        {
            Directory.CreateDirectory(Path.Combine(staging, "CommonPlugins"));
            var configurations = new SortedDictionary<string, object>(StringComparer.Ordinal);
            var provenance = new List<object>();
            foreach (var (data, assembly, metadata) in loaded)
            {
                Type[] types = assembly.GetTypes();
                Type[] plugins = types.Where(type => !type.IsAbstract && !type.IsInterface && typeof(IPlugin).IsAssignableFrom(type)).ToArray();
                if (plugins.Length > 1) throw new InvalidDataException("Ambiguous IPlugin entry points: " + data.Id);
                var defaults = ConfigurationDefaults.Export(types, plugins);
                configurations.Add(data.Id, defaults.Count == 1
                    ? (object)new { configType = defaults.First().Key, configuration = Parse(defaults.First().Value) }
                    : new { configurations = defaults.Select(pair => new { configType = pair.Key, configuration = Parse(pair.Value) }).ToArray() });

                bool transport = data.Id == "direct-transport";
                string name = transport ? "DirectTransport" : data.Id;
                string bundle = Path.Combine(staging, transport ? "DirectTransport" : "CommonPlugins/" + name);
                string bin = Path.GetDirectoryName(assembly.Location);
                Directory.CreateDirectory(bundle);
                CopyTree(bin, bundle, assembly.Location);
                CopyFile(assembly.Location, Path.Combine(bundle, name + ".dll"));
                string sourceMetadata = Path.Combine(bundle, "source-metadata.xml");
                if (!File.Exists(sourceMetadata))
                    using (var file = File.Create(sourceMetadata))
                        new XmlSerializer(typeof(PluginData)).Serialize(file, metadata);
                var assets = new List<PluginAsset>();
                foreach (var asset in data.GetNamedAssets().OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    SafeName(asset.Key);
                    string source = Path.GetFullPath(asset.Value);
                    string relative = Path.GetRelativePath(bin, source);
                    if (relative == ".." || relative.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(relative))
                    {
                        relative = "Assets/" + asset.Key;
                        string target = Path.Combine(bundle, relative);
                        if (Directory.Exists(source)) CopyTree(source, target);
                        else CopyFile(source, target);
                    }
                    assets.Add(new PluginAsset { Name = asset.Key, Path = relative.Replace('\\', '/') });
                }
                metadata.Assets = assets.ToArray();
                metadata.Runtimes = "CoreCLR";
                metadata.Platforms = "Linux";
                metadata.AlternateVersions = null;
                metadata.NuGetReferences = null;
                using (var file = File.Create(Path.Combine(bundle, name + ".xml")))
                    new XmlSerializer(typeof(PluginData)).Serialize(file, metadata);
                if (transport) File.Copy(Path.Combine(bundle, name + ".xml"), Path.Combine(bundle, name + ".dll.xml"), true);
                provenance.Add(new { id = data.Id, repository = metadata.RepoId, commit = metadata.Commit,
                    source = data is LocalPlugin ? "local-metadata" : "github", assemblySha256 = Tools.GetFileHash(assembly.Location) });
            }
            var version = manager.GameVersion;
            string gameVersion = checked(version.Major * 1000000 + version.Minor * 1000 + version.Build).ToString(CultureInfo.InvariantCulture);
            var files = Directory.GetFiles(staging, "*", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal).ToDictionary(path => Path.GetRelativePath(staging, path).Replace('\\', '/'),
                    path => new { sha256 = Tools.GetFileHash(path), bytes = new FileInfo(path).Length });
            File.WriteAllText(Path.Combine(staging, "preparation.json"), JsonSerializer.Serialize(new {
                schemaVersion = 1, gameVersion, pluginSdkSha256 = Tools.GetFileHash(typeof(PluginConfig).Assembly.Location),
                magnetarVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(), runtimeIdentifier = Tools.RuntimeIdentifier,
                pluginConfigurations = configurations, plugins = provenance, files
            }, new JsonSerializerOptions { WriteIndented = true }));
            Directory.Move(staging, output);
            Console.WriteLine("Managed preparation exported: " + output);
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
#endif
    }

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static GitHubPlugin ReadMetadata(PluginData data, Profile profile)
    {
        GitHubPlugin metadata;
        if (data is GitHubPlugin remote)
        {
            metadata = Tools.DeepCopy(remote);
            if (profile.GetData(data.Id) is GitHubPluginConfig settings && !string.IsNullOrWhiteSpace(settings.SelectedVersion))
            {
                var alternate = remote.AlternateVersions?.SingleOrDefault(version => version.Name.Equals(settings.SelectedVersion, StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidDataException("Unknown selected plugin version: " + data.Id);
                metadata.RepoId = alternate.Repo ?? remote.RepoId;
                metadata.Commit = alternate.Commit ?? remote.Commit;
            }
        }
        else if (data is LocalPlugin local)
        {
            string path = Path.ChangeExtension(local.Dll, ".xml");
            if (!File.Exists(path)) path = local.Dll + ".xml";
            RefuseLink(path);
            using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null, MaxCharactersInDocument = 1024 * 1024 });
            metadata = new XmlSerializer(typeof(PluginData)).Deserialize(reader) as GitHubPlugin
                ?? throw new InvalidDataException("Local plugin needs GitHubPlugin provenance metadata: " + data.Id);
        }
        else throw new InvalidDataException("Managed preparation requires pinned GitHub source or a metadata-backed local binary: " + data.Id);
        if (metadata.Id != data.Id || metadata.Commit?.Length != 40 || !metadata.Commit.All(Uri.IsHexDigit)
            || string.IsNullOrWhiteSpace(metadata.RepoId) || metadata.RepoId.Split('/').Length != 2)
            throw new InvalidDataException("Plugin provenance requires its exact ID, repository and 40-character commit: " + data.Id);
        return metadata;
    }

    private static void SafeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "." || name == ".."
            || name.Any(character => !(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.')))
            throw new InvalidDataException("Unsafe plugin or asset name: " + name);
    }

    private static void RefuseLink(string path)
    {
        for (string current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Preparation does not follow symbolic links: " + current);
    }

    private static void CopyTree(string source, string destination, string skip = null)
    {
        RefuseLink(source);
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
            if (file != skip) CopyFile(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (string directory in Directory.GetDirectories(source))
            CopyTree(directory, Path.Combine(destination, Path.GetFileName(directory)), skip);
    }

    private static void CopyFile(string source, string destination)
    {
        RefuseLink(source);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Copy(source, destination, false);
    }
}
