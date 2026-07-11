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

    public static string ProfilesDir(string magnetarConfigDir) =>
        Path.Combine(magnetarConfigDir, "Profiles");

    /// <summary>Path of the active profile (<c>Profiles/Current.xml</c>).</summary>
    public static string PathFor(string magnetarConfigDir) =>
        PathForKey(magnetarConfigDir, ProfileName);

    /// <summary>Path of a named profile (<c>Profiles/&lt;key&gt;.xml</c>).</summary>
    public static string PathForKey(string magnetarConfigDir, string key) =>
        Path.Combine(magnetarConfigDir, "Profiles", key + ".xml");

    /// <summary>Magnetar's profile file-name key: <c>Tools.CleanFileName(name)</c>.</summary>
    public static string CleanKey(string name)
    {
        var invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
        var sb = new System.Text.StringBuilder();
        foreach (char c in name ?? string.Empty)
            sb.Append(invalid.Contains(c) ? '-' : c);
        return sb.ToString();
    }

    /// <summary>Opens the active profile (Current.xml), creating a skeleton when absent.</summary>
    public static PluginProfileDocument Open(string magnetarConfigDir) =>
        OpenPath(PathFor(magnetarConfigDir), ProfileName);

    /// <summary>Opens the named profile <c>&lt;key&gt;.xml</c>, creating a skeleton when absent.</summary>
    public static PluginProfileDocument OpenNamed(string magnetarConfigDir, string key) =>
        OpenPath(PathForKey(magnetarConfigDir, key), key);

    private static PluginProfileDocument OpenPath(string path, string skeletonName)
    {
        XDocument doc = File.Exists(path)
            ? XDocument.Load(path, LoadOptions.PreserveWhitespace)
            : CreateSkeleton(skeletonName);
        return new PluginProfileDocument(path, doc);
    }

    private static XDocument CreateSkeleton(string name)
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("Profile",
                new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsd", Xsd.NamespaceName),
                new XElement("Name", name),
                new XElement("GitHub"),
                new XElement("DevFolder"),
                new XElement("Local"),
                new XElement("Mods")));
    }

    /// <summary>The profile's display name (<c>&lt;Name&gt;</c>); its key is <see cref="CleanKey"/> of it.</summary>
    public string Name
    {
        get => Root.Element("Name")?.Value?.Trim() ?? ProfileName;
        set
        {
            XElement el = Root.Element("Name");
            if (el == null)
                Root.AddFirst(new XElement("Name", value));
            else
                el.Value = value;
        }
    }

    /// <summary>Replaces this profile's four plugin collections with clones from another profile.</summary>
    public void CopyCollectionsFrom(PluginProfileDocument source)
    {
        foreach (string name in new[] { "GitHub", "DevFolder", "Local", "Mods" })
        {
            XElement mine = List(name);
            XElement theirs = source.Root.Element(name);
            mine.ReplaceNodes(theirs?.Elements().Select(e => new XElement(e)) ?? System.Linq.Enumerable.Empty<XElement>());
        }
    }

    /// <summary>
    /// A canonical signature of the four enabled sets (order-independent), used to
    /// tell whether the active profile matches a saved one.
    /// </summary>
    public string CollectionsSignature()
    {
        string Join(IEnumerable<string> vals) => string.Join(",",
            (vals ?? System.Linq.Enumerable.Empty<string>())
            .Select(v => (v ?? string.Empty).Trim())
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase));

        string gh = Join(Root.Element("GitHub")?.Elements("GitHubPluginConfig").Select(e => e.Element("Id")?.Value));
        string dev = Join(Root.Element("DevFolder")?.Elements("LocalFolderConfig").Select(e => e.Element("Id")?.Value));
        string loc = Join(Root.Element("Local")?.Elements("string").Select(e => e.Value));
        string mod = Join(Root.Element("Mods")?.Elements("unsignedLong").Select(e => e.Value));
        return $"GH[{gh}]|DEV[{dev}]|LOC[{loc}]|MOD[{mod}]";
    }

    /// <summary>Writes this profile to an explicit path (used by profile save/rename).</summary>
    public void SaveTo(AtomicFile writer, string path) => writer.WriteText(path, XmlOut.ToXmlString(xml));

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

    // --- hub / remote plugins (Profile.GitHub: HashSet<GitHubPluginConfig>) ---
    // A plugin is enabled by naming its PluginData.Id in a <GitHubPluginConfig>.

    public IReadOnlyList<string> GitHubPlugins =>
        Root.Element("GitHub")?.Elements("GitHubPluginConfig")
            .Select(e => e.Element("Id")?.Value?.Trim())
            .Where(v => !string.IsNullOrEmpty(v)).ToList() ?? new List<string>();

    public bool EnableGitHub(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;
        XElement list = List("GitHub");
        if (list.Elements("GitHubPluginConfig").Any(e => IdEq(e.Element("Id")?.Value, id)))
            return false;
        list.Add(new XElement("GitHubPluginConfig", new XElement("Id", id)));
        return true;
    }

    public bool DisableGitHub(string id)
    {
        XElement removed = Root.Element("GitHub")?.Elements("GitHubPluginConfig")
            .FirstOrDefault(e => IdEq(e.Element("Id")?.Value, id));
        if (removed == null)
            return false;
        removed.Remove();
        return true;
    }

    // --- mods (Profile.Mods: HashSet<ulong> of Steam Workshop ids) ---

    public IReadOnlyList<ulong> Mods =>
        Root.Element("Mods")?.Elements("unsignedLong")
            .Select(e => ulong.TryParse((e.Value ?? string.Empty).Trim(), out ulong v) ? v : 0UL)
            .Where(v => v != 0).ToList() ?? new List<ulong>();

    public bool EnableMod(ulong id)
    {
        if (id == 0)
            return false;
        XElement list = List("Mods");
        if (list.Elements("unsignedLong").Any(e => (e.Value ?? string.Empty).Trim() == id.ToString()))
            return false;
        list.Add(new XElement("unsignedLong", id.ToString()));
        return true;
    }

    public bool DisableMod(ulong id)
    {
        XElement removed = Root.Element("Mods")?.Elements("unsignedLong")
            .FirstOrDefault(e => (e.Value ?? string.Empty).Trim() == id.ToString());
        if (removed == null)
            return false;
        removed.Remove();
        return true;
    }

    public void Save(AtomicFile writer) => writer.WriteText(FilePath, XmlOut.ToXmlString(xml));

    private static bool IdEq(string a, string b) =>
        string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
}
