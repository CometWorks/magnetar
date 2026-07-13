using System;
using System.IO;
using System.Xml.Linq;
using Magnetar.ConfigTerminal.Io;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// Read/write <c>Saves/LastSession.sbl</c> (<c>MyObjectBuilder_LastSession</c>),
/// which selects the world the DS loads next. Despite the extension it is plain
/// uncompressed XML. The DS checks <c>RelativePath</c> first (keeps saves
/// portable), then <c>Path</c>.
/// </summary>
internal sealed class LastSessionFile
{
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

    public string Path;
    public string RelativePath;
    public string GameName;
    public bool IsContentWorlds;
    public bool IsOnline;
    public bool IsLobby;
    // The DS records the port it last bound (0 before the world has run). We
    // write 0 like a fresh DS-authored file and preserve whatever we read back.
    public int ServerPort;

    public static string PathFor(string savesPath) =>
        System.IO.Path.Combine(savesPath, "LastSession.sbl");

    /// <summary>Reads the file if present; returns null when absent or unreadable.</summary>
    public static LastSessionFile Read(string sblPath)
    {
        if (!File.Exists(sblPath))
            return null;

        try
        {
            XDocument xml = XDocument.Load(sblPath);
            XElement r = xml.Root;
            if (r == null)
                return null;

            return new LastSessionFile
            {
                Path = r.Element("Path")?.Value,
                RelativePath = r.Element("RelativePath")?.Value,
                GameName = r.Element("GameName")?.Value,
                IsContentWorlds = ConfigDocumentBase.ParseBool(r.Element("IsContentWorlds")?.Value),
                IsOnline = ConfigDocumentBase.ParseBool(r.Element("IsOnline")?.Value),
                IsLobby = ConfigDocumentBase.ParseBool(r.Element("IsLobby")?.Value),
                ServerPort = int.TryParse(r.Element("ServerPort")?.Value, out int sp) ? sp : 0,
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Builds a LastSession pointing at a world, computing RelativePath when it lives under Saves/.</summary>
    public static LastSessionFile ForWorld(WorldInfo world, string savesPath)
    {
        string relative = TryGetRelativePath(savesPath, world.FolderPath);
        return new LastSessionFile
        {
            Path = System.IO.Path.GetFullPath(world.FolderPath),
            RelativePath = relative,
            GameName = string.IsNullOrEmpty(world.SessionName) ? world.FolderName : world.SessionName,
            IsContentWorlds = false,
            IsOnline = false,
            IsLobby = false,
        };
    }

    public void Write(AtomicFile writer, string sblPath)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("MyObjectBuilder_LastSession",
                new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsd", Xsd.NamespaceName),
                new XElement("Path", Path ?? string.Empty),
                new XElement("IsContentWorlds", IsContentWorlds ? "true" : "false"),
                new XElement("IsOnline", IsOnline ? "true" : "false"),
                new XElement("IsLobby", IsLobby ? "true" : "false"),
                new XElement("GameName", GameName ?? string.Empty),
                new XElement("ServerPort", ServerPort.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                string.IsNullOrEmpty(RelativePath) ? null : new XElement("RelativePath", RelativePath)));

        writer.WriteText(sblPath, XmlOut.ToXmlString(doc));
    }

    private static string TryGetRelativePath(string savesPath, string worldPath)
    {
        try
        {
            string rel = PlatformPaths.GetRelativePath(savesPath, worldPath);
            // Only usable when the world actually lives under Saves/.
            if (string.IsNullOrEmpty(rel) || rel.StartsWith("..", StringComparison.Ordinal)
                || System.IO.Path.IsPathRooted(rel))
                return null;
            return rel;
        }
        catch
        {
            return null;
        }
    }
}
