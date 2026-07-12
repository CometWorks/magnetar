using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// XDocument wrapper for a world's <c>Sandbox_config.sbc</c>
/// (<c>MyObjectBuilder_WorldConfiguration</c>). Editing session settings here is
/// the correct minimal way to change a world: on load the DS reads
/// <c>Sandbox.sbc</c> then overrides Settings/Mods/SessionName from this file.
/// Session options live under <c>&lt;Settings&gt;</c>.
/// </summary>
internal sealed class WorldConfigDocument : ConfigDocumentBase
{
    private const string RootName = "MyObjectBuilder_WorldConfiguration";
    private const string SettingsName = "Settings";
    private const string ModsName = "Mods";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

    private WorldConfigDocument(string filePath, XDocument xml, bool existsOnDisk)
        : base(filePath, xml, existsOnDisk)
    {
    }

    public static WorldConfigDocument Open(string filePath)
    {
        if (File.Exists(filePath))
        {
            XDocument xml = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
            return new WorldConfigDocument(filePath, xml, existsOnDisk: true);
        }

        return new WorldConfigDocument(filePath, CreateSkeleton(), existsOnDisk: false);
    }

    private static XDocument CreateSkeleton()
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(RootName,
                new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsd", Xsd.NamespaceName),
                new XElement(SettingsName)));
    }

    private XElement Root => Xml.Root;

    protected override XElement ResolveScopeRoot(OptionScope scope, bool create)
    {
        // A world config only carries session settings; the root scope is unused.
        XElement settings = Root.Element(SettingsName);
        if (settings == null && create)
        {
            settings = new XElement(SettingsName);
            Root.AddFirst(settings);
        }
        return settings;
    }

    public string SessionName
    {
        get => Root.Element("SessionName")?.Value ?? string.Empty;
        set
        {
            XElement el = Root.Element("SessionName");
            if (el == null)
                Root.Add(new XElement("SessionName", value ?? string.Empty));
            else
                el.Value = value ?? string.Empty;
        }
    }

    public DateTime? LastSaveTime
    {
        get
        {
            string raw = Root.Element("LastSaveTime")?.Value;
            return DateTime.TryParse(raw, out DateTime dt) ? dt : (DateTime?)null;
        }
    }

    /// <summary>
    /// Updates an existing <c>&lt;LastSaveTime&gt;</c> to now so a freshly created
    /// world sorts to the top of the list. Only touches the element when present
    /// (avoids reordering the DS's serialized element sequence); when absent the
    /// catalog falls back to the checkpoint file's modification time, which is also
    /// fresh for a just-copied world.
    /// </summary>
    public void RefreshLastSaveTime()
    {
        XElement el = Root.Element("LastSaveTime");
        if (el != null)
            el.Value = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff");
    }

    /// <summary>Reads the current mod list from the document.</summary>
    public ModList ReadMods()
    {
        var list = new ModList();
        XElement mods = Root.Element(ModsName);
        if (mods == null)
            return list;

        foreach (XElement item in mods.Elements("ModItem"))
        {
            string idText = item.Element("PublishedFileId")?.Value?.Trim();
            if (!ulong.TryParse(idText, out ulong id) || id == 0)
                continue;

            if (list.Items.Any(m => m.PublishedFileId == id))
                continue;

            list.Items.Add(new ModItem
            {
                PublishedFileId = id,
                FriendlyName = (string)item.Attribute("FriendlyName") ?? string.Empty,
                ServiceName = item.Element("PublishedServiceName")?.Value ?? "Steam",
                IsDependency = ParseBool(item.Element("IsDependency")?.Value),
            });
        }
        return list;
    }

    /// <summary>Rebuilds the <c>&lt;Mods&gt;</c> element in list (load) order.</summary>
    public void WriteMods(ModList list)
    {
        XElement mods = Root.Element(ModsName);
        if (mods == null)
        {
            mods = new XElement(ModsName);
            // Place Mods right after Settings, matching DS output.
            XElement settings = Root.Element(SettingsName);
            if (settings != null)
                settings.AddAfterSelf(mods);
            else
                Root.Add(mods);
        }
        else
        {
            mods.RemoveNodes();
        }

        foreach (ModItem mod in list.Items.Where(m => m.PublishedFileId != 0))
        {
            string id = mod.PublishedFileId.ToString();
            mods.Add(new XElement("ModItem",
                new XAttribute("FriendlyName", mod.FriendlyName ?? string.Empty),
                new XElement("Name", id + ".sbm"),
                new XElement("PublishedFileId", id),
                new XElement("PublishedServiceName", string.IsNullOrEmpty(mod.ServiceName) ? "Steam" : mod.ServiceName),
                new XElement("IsDependency", mod.IsDependency ? "true" : "false")));
        }
    }

    /// <summary>Seeds this (in-memory) document from a checkpoint reader when no Sandbox_config.sbc exists.</summary>
    public void SeedFrom(CheckpointInfo info)
    {
        if (info == null)
            return;
        if (!string.IsNullOrEmpty(info.SessionName))
            SessionName = info.SessionName;
    }
}
