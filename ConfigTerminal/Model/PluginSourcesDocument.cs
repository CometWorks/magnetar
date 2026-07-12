using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Magnetar.ConfigTerminal.Io;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>One registered dev-folder source (sources.xml LocalPluginSources).</summary>
internal sealed class LocalPluginSource
{
    public string Name;
    public string Folder;
    public bool Enabled = true;
    // The picked manifest filename, kept as a hint for enabling the folder later.
    // Not part of Magnetar's own LocalPlugin schema, so Magnetar strips it on its
    // next save — the manifest is re-derived from the folder when it's gone.
    public string DataFile;
}

/// <summary>A remote GitHub hub catalog source (sources.xml RemoteHubSources).</summary>
internal sealed class RemoteHubSource
{
    public string Name;
    public string Repo;      // "owner/name"
    public string Branch;
    public bool Enabled = true;
    public bool Trusted = true;
}

/// <summary>A single remote plugin source (sources.xml RemotePluginSources).</summary>
internal sealed class RemotePluginSource
{
    public string Name;
    public string Repo;
    public string Branch;
    public string File;      // manifest path within the repo
    public bool Enabled = true;
    public bool Trusted = true;
}

/// <summary>A local folder hub catalog source (sources.xml LocalHubSources).</summary>
internal sealed class LocalHubSource
{
    public string Name;
    public string Folder;
    public bool Enabled = true;
}

/// <summary>A Magnetar mod source (sources.xml ModSources: Mod { Name, ID, Enabled }).</summary>
internal sealed class ModSourceEntry
{
    public string Name;
    public long Id;          // Steam Workshop id (element is <ID>, type long)
    public bool Enabled = true;
}

/// <summary>
/// XDocument wrapper for <c>Sources/sources.xml</c> (root <c>SourcesConfig</c>).
/// Edits only the <c>LocalPluginSources</c> list (the dev-folder sources),
/// preserving hub sources, remote/mod sources and unknown elements.
/// </summary>
internal sealed class PluginSourcesDocument
{
    private const string ListName = "LocalPluginSources";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

    private readonly XDocument xml;

    public string FilePath { get; }

    private PluginSourcesDocument(string filePath, XDocument xml)
    {
        FilePath = filePath;
        this.xml = xml;
    }

    public static string PathFor(string magnetarConfigDir) =>
        Path.Combine(magnetarConfigDir, "Sources", "sources.xml");

    public static PluginSourcesDocument Open(string magnetarConfigDir)
    {
        string path = PathFor(magnetarConfigDir);
        XDocument doc = File.Exists(path)
            ? XDocument.Load(path, LoadOptions.PreserveWhitespace)
            : CreateSkeleton();
        return new PluginSourcesDocument(path, doc);
    }

    private static XDocument CreateSkeleton()
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("SourcesConfig",
                new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsd", Xsd.NamespaceName),
                new XElement("ShowWarning", "true"),
                new XElement("MaxSourceAge", "2"),
                new XElement("LocalHubSources"),
                new XElement("RemoteHubSources"),
                new XElement("RemotePluginSources"),
                new XElement(ListName),
                new XElement("ModSources")));
    }

    private XElement Root => xml.Root;

    public IReadOnlyList<LocalPluginSource> LocalPlugins =>
        Root.Element(ListName)?.Elements("LocalPlugin").Select(e => new LocalPluginSource
        {
            Name = e.Element("Name")?.Value?.Trim(),
            Folder = e.Element("Folder")?.Value?.Trim(),
            Enabled = ConfigDocumentBase.ParseBool(e.Element("Enabled")?.Value),
            DataFile = e.Element("DataFile")?.Value?.Trim(),
        }).ToList() ?? new List<LocalPluginSource>();

    /// <summary>Adds a dev-folder source (dedup by folder path). Returns false if already present.</summary>
    public bool AddLocalPlugin(string name, string folder, string dataFile = null, bool enabled = true)
    {
        XElement list = Root.Element(ListName);
        if (list == null)
        {
            list = new XElement(ListName);
            Root.Add(list);
        }

        if (list.Elements("LocalPlugin").Any(e => PathEq(e.Element("Folder")?.Value, folder)))
            return false;

        list.Add(new XElement("LocalPlugin",
            new XElement("Name", name),
            new XElement("Folder", folder),
            new XElement("Enabled", enabled ? "true" : "false"),
            string.IsNullOrEmpty(dataFile) ? null : new XElement("DataFile", dataFile)));
        return true;
    }

    /// <summary>Removes the dev-folder source with the given folder path.</summary>
    public bool RemoveByFolder(string folder)
    {
        XElement removed = Root.Element(ListName)?.Elements("LocalPlugin")
            .FirstOrDefault(e => PathEq(e.Element("Folder")?.Value, folder));
        if (removed == null)
            return false;
        removed.Remove();
        return true;
    }

    public LocalPluginSource FindById(string id) =>
        LocalPlugins.FirstOrDefault(p => string.Equals(
            Path.GetFileName((p.Folder ?? string.Empty).TrimEnd('/', '\\')), id, StringComparison.OrdinalIgnoreCase));

    // --- remote hub sources (RemoteHubSources: RemoteHub items) ---

    public IReadOnlyList<RemoteHubSource> RemoteHubs =>
        Root.Element("RemoteHubSources")?.Elements("RemoteHub").Select(e => new RemoteHubSource
        {
            Name = e.Element("Name")?.Value?.Trim(),
            Repo = e.Element("Repo")?.Value?.Trim(),
            Branch = e.Element("Branch")?.Value?.Trim(),
            Enabled = ConfigDocumentBase.ParseBool(e.Element("Enabled")?.Value),
            Trusted = ConfigDocumentBase.ParseBool(e.Element("Trusted")?.Value),
        }).ToList() ?? new List<RemoteHubSource>();

    /// <summary>Adds a remote hub source (dedup by repo). Fresh — no LastCheck/Hash so Magnetar refetches.</summary>
    public bool AddRemoteHub(string name, string repo, string branch, bool trusted = true)
    {
        XElement list = ListEl("RemoteHubSources");
        if (list.Elements("RemoteHub").Any(e => RepoEq(e.Element("Repo")?.Value, repo)))
            return false;
        list.Add(new XElement("RemoteHub",
            new XElement("Name", name),
            new XElement("Repo", repo),
            new XElement("Branch", string.IsNullOrWhiteSpace(branch) ? "main" : branch),
            new XElement("Enabled", "true"),
            new XElement("Trusted", trusted ? "true" : "false")));
        return true;
    }

    public bool RemoveRemoteHub(string repo) => RemoveByChild("RemoteHubSources", "RemoteHub", "Repo", repo, RepoEq);

    public bool SetRemoteHubEnabled(string repo, bool enabled) =>
        SetChildFlag("RemoteHubSources", "RemoteHub", "Repo", repo, RepoEq, "Enabled", enabled);

    // --- remote plugin sources (RemotePluginSources: RemotePlugin items) ---

    public IReadOnlyList<RemotePluginSource> RemotePlugins =>
        Root.Element("RemotePluginSources")?.Elements("RemotePlugin").Select(e => new RemotePluginSource
        {
            Name = e.Element("Name")?.Value?.Trim(),
            Repo = e.Element("Repo")?.Value?.Trim(),
            Branch = e.Element("Branch")?.Value?.Trim(),
            File = e.Element("File")?.Value?.Trim(),
            Enabled = ConfigDocumentBase.ParseBool(e.Element("Enabled")?.Value),
            Trusted = ConfigDocumentBase.ParseBool(e.Element("Trusted")?.Value),
        }).ToList() ?? new List<RemotePluginSource>();

    public bool AddRemotePlugin(string name, string repo, string branch, string file, bool trusted = true)
    {
        XElement list = ListEl("RemotePluginSources");
        if (list.Elements("RemotePlugin").Any(e => RepoEq(e.Element("Repo")?.Value, repo)))
            return false;
        list.Add(new XElement("RemotePlugin",
            new XElement("Name", name),
            new XElement("Repo", repo),
            new XElement("Branch", string.IsNullOrWhiteSpace(branch) ? "main" : branch),
            new XElement("File", file ?? string.Empty),
            new XElement("Enabled", "true"),
            new XElement("Trusted", trusted ? "true" : "false")));
        return true;
    }

    public bool RemoveRemotePlugin(string repo) => RemoveByChild("RemotePluginSources", "RemotePlugin", "Repo", repo, RepoEq);

    public bool SetRemotePluginEnabled(string repo, bool enabled) =>
        SetChildFlag("RemotePluginSources", "RemotePlugin", "Repo", repo, RepoEq, "Enabled", enabled);

    // --- local hub sources (LocalHubSources: LocalHub items) ---

    public IReadOnlyList<LocalHubSource> LocalHubs =>
        Root.Element("LocalHubSources")?.Elements("LocalHub").Select(e => new LocalHubSource
        {
            Name = e.Element("Name")?.Value?.Trim(),
            Folder = e.Element("Folder")?.Value?.Trim(),
            Enabled = ConfigDocumentBase.ParseBool(e.Element("Enabled")?.Value),
        }).ToList() ?? new List<LocalHubSource>();

    public bool AddLocalHub(string name, string folder)
    {
        XElement list = ListEl("LocalHubSources");
        if (list.Elements("LocalHub").Any(e => PathEq(e.Element("Folder")?.Value, folder)))
            return false;
        list.Add(new XElement("LocalHub",
            new XElement("Name", name),
            new XElement("Folder", folder),
            new XElement("Enabled", "true")));
        return true;
    }

    public bool RemoveLocalHub(string folder) => RemoveByChild("LocalHubSources", "LocalHub", "Folder", folder, PathEq);

    public bool SetLocalHubEnabled(string folder, bool enabled) =>
        SetChildFlag("LocalHubSources", "LocalHub", "Folder", folder, PathEq, "Enabled", enabled);

    // --- mod sources (ModSources: Mod { Name, ID (long), Enabled }) ---

    public IReadOnlyList<ModSourceEntry> ModSources =>
        Root.Element("ModSources")?.Elements("Mod").Select(e => new ModSourceEntry
        {
            Name = e.Element("Name")?.Value?.Trim(),
            Id = long.TryParse((e.Element("ID")?.Value ?? string.Empty).Trim(), out long v) ? v : 0,
            Enabled = ConfigDocumentBase.ParseBool(e.Element("Enabled")?.Value),
        }).Where(m => m.Id != 0).ToList() ?? new List<ModSourceEntry>();

    /// <summary>Adds or updates a mod source (keyed by ID). Returns false when it already existed.</summary>
    public bool AddMod(long id, string name, bool enabled = true)
    {
        XElement list = ListEl("ModSources");
        XElement existing = list.Elements("Mod")
            .FirstOrDefault(e => (e.Element("ID")?.Value ?? string.Empty).Trim() == id.ToString());
        if (existing != null)
        {
            SetChild(existing, "Name", name ?? id.ToString());
            SetChild(existing, "Enabled", enabled ? "true" : "false");
            return false;
        }
        list.Add(new XElement("Mod",
            new XElement("Name", name ?? id.ToString()),
            new XElement("ID", id.ToString()),
            new XElement("Enabled", enabled ? "true" : "false")));
        return true;
    }

    public bool RemoveMod(long id)
    {
        XElement removed = Root.Element("ModSources")?.Elements("Mod")
            .FirstOrDefault(e => (e.Element("ID")?.Value ?? string.Empty).Trim() == id.ToString());
        if (removed == null)
            return false;
        removed.Remove();
        return true;
    }

    public bool SetModEnabled(long id, bool enabled) =>
        SetChildFlag("ModSources", "Mod", "ID", id.ToString(), (a, b) =>
            (a ?? string.Empty).Trim() == (b ?? string.Empty).Trim(), "Enabled", enabled);

    public bool SetModName(long id, string name)
    {
        XElement el = Root.Element("ModSources")?.Elements("Mod")
            .FirstOrDefault(e => (e.Element("ID")?.Value ?? string.Empty).Trim() == id.ToString());
        if (el == null)
            return false;
        SetChild(el, "Name", name ?? id.ToString());
        return true;
    }

    // --- shared helpers ---

    private XElement ListEl(string name)
    {
        XElement el = Root.Element(name);
        if (el == null)
        {
            el = new XElement(name);
            Root.Add(el);
        }
        return el;
    }

    private bool RemoveByChild(string listName, string itemName, string keyChild, string keyValue,
        Func<string, string, bool> eq)
    {
        XElement removed = Root.Element(listName)?.Elements(itemName)
            .FirstOrDefault(e => eq(e.Element(keyChild)?.Value, keyValue));
        if (removed == null)
            return false;
        removed.Remove();
        return true;
    }

    private bool SetChildFlag(string listName, string itemName, string keyChild, string keyValue,
        Func<string, string, bool> eq, string flagChild, bool value)
    {
        XElement el = Root.Element(listName)?.Elements(itemName)
            .FirstOrDefault(e => eq(e.Element(keyChild)?.Value, keyValue));
        if (el == null)
            return false;
        SetChild(el, flagChild, value ? "true" : "false");
        return true;
    }

    private static void SetChild(XElement parent, string name, string value)
    {
        XElement child = parent.Element(name);
        if (child == null)
            parent.Add(new XElement(name, value));
        else
            child.Value = value;
    }

    private static bool RepoEq(string a, string b) =>
        string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

    public void Save(AtomicFile writer) => writer.WriteText(FilePath, XmlOut.ToXmlString(xml));

    private static bool PathEq(string a, string b) =>
        PlatformPaths.PathComparer.Equals(
            (a ?? string.Empty).TrimEnd('/', '\\'),
            (b ?? string.Empty).TrimEnd('/', '\\'));
}
