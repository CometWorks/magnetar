using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Magnetar.ConfigTerminal.Io;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>One enabled dev-folder plugin in the active profile.</summary>
internal sealed class DevFolderEntry
{
    public string Id;         // == the source folder name (last path segment)
    public string DataFile;   // manifest filename, relative to the folder
    public bool DebugBuild = true;
    public string Folder;     // resolved from the matching sources entry (may be null)
}

/// <summary>
/// XDocument wrapper for Magnetar's active profile (<c>Profiles/Current.xml</c>,
/// root <c>Profile</c>). Edits only the enabled-set collections
/// (<c>Local</c> DLLs and <c>DevFolder</c> configs), preserving <c>GitHub</c>,
/// <c>Mods</c> and any unknown elements — same upsert philosophy as the DS files.
/// Magnetar's <c>Profile.Validate()</c> requires all four collections to be
/// present, so the skeleton always writes them.
/// </summary>
internal sealed class PluginProfileDocument
{
    private const string ProfileName = "Current";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

    private readonly XDocument xml;

    public string FilePath { get; }

    private PluginProfileDocument(string filePath, XDocument xml)
    {
        FilePath = filePath;
        this.xml = xml;
    }

    public static string PathFor(string magnetarConfigDir) =>
        Path.Combine(magnetarConfigDir, "Profiles", ProfileName + ".xml");

    public static PluginProfileDocument Open(string magnetarConfigDir)
    {
        string path = PathFor(magnetarConfigDir);
        XDocument doc = File.Exists(path)
            ? XDocument.Load(path, LoadOptions.PreserveWhitespace)
            : CreateSkeleton();
        return new PluginProfileDocument(path, doc);
    }

    private static XDocument CreateSkeleton()
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Profile",
                new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsd", Xsd.NamespaceName),
                new XElement("Name", ProfileName),
                new XElement("GitHub"),
                new XElement("DevFolder"),
                new XElement("Local"),
                new XElement("Mods")));
    }

    private XElement Root => xml.Root;

    private XElement List(string name)
    {
        XElement el = Root.Element(name);
        if (el == null)
        {
            el = new XElement(name);
            Root.Add(el);
        }
        return el;
    }

    // --- local DLLs (Profile.Local: HashSet<string> of DLL file names) ---

    public IReadOnlyList<string> LocalDlls =>
        Root.Element("Local")?.Elements("string").Select(e => e.Value.Trim())
            .Where(v => v.Length > 0).ToList() ?? new List<string>();

    public bool EnableLocalDll(string dllFileName)
    {
        XElement list = List("Local");
        if (list.Elements("string").Any(e => IdEq(e.Value, dllFileName)))
            return false;
        list.Add(new XElement("string", dllFileName));
        return true;
    }

    public bool DisableLocalDll(string dllFileName)
    {
        XElement removed = Root.Element("Local")?.Elements("string")
            .FirstOrDefault(e => IdEq(e.Value, dllFileName));
        if (removed == null)
            return false;
        removed.Remove();
        return true;
    }

    // --- dev folders (Profile.DevFolder: HashSet<LocalFolderConfig>) ---

    public IReadOnlyList<DevFolderEntry> DevFolders =>
        Root.Element("DevFolder")?.Elements("LocalFolderConfig").Select(e => new DevFolderEntry
        {
            Id = e.Element("Id")?.Value?.Trim(),
            DataFile = e.Element("DataFile")?.Value?.Trim(),
            DebugBuild = ConfigDocumentBase.ParseBool(e.Element("DebugBuild")?.Value),
        }).ToList() ?? new List<DevFolderEntry>();

    public bool EnableDevFolder(string id, string dataFile, bool debugBuild)
    {
        XElement list = List("DevFolder");
        if (list.Elements("LocalFolderConfig").Any(e => IdEq(e.Element("Id")?.Value, id)))
            return false;

        list.Add(new XElement("LocalFolderConfig",
            new XElement("Id", id),
            dataFile == null ? null : new XElement("DataFile", dataFile),
            new XElement("DebugBuild", debugBuild ? "true" : "false")));
        return true;
    }

    public bool DisableDevFolder(string id)
    {
        XElement removed = Root.Element("DevFolder")?.Elements("LocalFolderConfig")
            .FirstOrDefault(e => IdEq(e.Element("Id")?.Value, id));
        if (removed == null)
            return false;
        removed.Remove();
        return true;
    }

    public void Save(AtomicFile writer) => writer.WriteText(FilePath, XmlOut.ToXmlString(xml));

    private static bool IdEq(string a, string b) =>
        string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
}
