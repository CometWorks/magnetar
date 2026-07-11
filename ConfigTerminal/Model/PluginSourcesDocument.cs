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
        }).ToList() ?? new List<LocalPluginSource>();

    /// <summary>Adds a dev-folder source (dedup by folder path). Returns false if already present.</summary>
    public bool AddLocalPlugin(string name, string folder, bool enabled = true)
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
            new XElement("Enabled", enabled ? "true" : "false")));
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

    public void Save(AtomicFile writer) => writer.WriteText(FilePath, XmlOut.ToXmlString(xml));

    private static bool PathEq(string a, string b) =>
        PlatformPaths.PathComparer.Equals(
            (a ?? string.Empty).TrimEnd('/', '\\'),
            (b ?? string.Empty).TrimEnd('/', '\\'));
}
