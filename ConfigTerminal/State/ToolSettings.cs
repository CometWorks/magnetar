using System;
using System.IO;
using System.Xml.Linq;
using Magnetar.ConfigTerminal.Io;

namespace Magnetar.ConfigTerminal.State;

/// <summary>
/// The tool's own per-instance settings, persisted as a small XML
/// (<c>ConfigTerminal.xml</c>) next to Magnetar's <c>config.xml</c> in the
/// selected config dir, so per-instance state travels with the instance. Tolerant
/// of a missing/partial file; never throws on load or save.
/// </summary>
internal sealed class ToolSettings
{
    private const string FileName = "ConfigTerminal.xml";

    private readonly string filePath;

    /// <summary>Directory the plugin-manifest picker should open at next (remembered across sessions).</summary>
    public string LastPluginFolder { get; set; }

    private ToolSettings(string filePath) => this.filePath = filePath;

    public static ToolSettings Load(string magnetarConfigDir)
    {
        string path = Path.Combine(magnetarConfigDir ?? ".", FileName);
        var settings = new ToolSettings(path);
        try
        {
            if (File.Exists(path))
            {
                XElement root = XDocument.Load(path).Root;
                settings.LastPluginFolder = root?.Element("LastPluginFolder")?.Value;
            }
        }
        catch
        {
            // Corrupt settings are non-fatal — fall back to defaults.
        }
        return settings;
    }

    public void Save(AtomicFile writer)
    {
        try
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("ConfigTerminal",
                    string.IsNullOrEmpty(LastPluginFolder) ? null : new XElement("LastPluginFolder", LastPluginFolder)));
            writer.WriteText(filePath, XmlOut.ToXmlString(doc));
        }
        catch
        {
            // Losing tool settings must never break the operation that triggered the save.
        }
    }
}
