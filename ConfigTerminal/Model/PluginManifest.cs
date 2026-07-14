using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// The display metadata a dev-folder plugin declares in its manifest XML — a
/// <c>GitHubPlugin</c> serialized as <c>PluginData</c> (namespace
/// <c>Pulsar.Shared.Data</c>). Read by local element name so it is robust to the
/// root type, <c>xsi:type</c> and namespaces; never references <c>Shared</c>.
/// </summary>
internal sealed class PluginManifest
{
    public string FriendlyName;
    public string Author;
    public string Tooltip;
    public string Description;

    /// <summary>
    /// Reads a manifest XML. Returns an empty manifest (all-null) on any error, so
    /// a bad or missing file degrades to just the folder-name id in the UI.
    /// </summary>
    public static PluginManifest Read(string xmlPath)
    {
        var manifest = new PluginManifest();
        if (string.IsNullOrEmpty(xmlPath) || !File.Exists(xmlPath))
            return manifest;

        try
        {
            XElement root = XDocument.Load(xmlPath).Root;
            manifest.FriendlyName = Local(root, "FriendlyName");
            manifest.Author = Local(root, "Author");
            manifest.Tooltip = Local(root, "Tooltip");
            manifest.Description = Local(root, "Description");
        }
        catch
        {
            // Ignore — an unreadable manifest just means no metadata.
        }
        return manifest;
    }

    /// <summary>
    /// Finds the manifest file inside a dev folder when its name isn't known: the
    /// first top-level <c>*.xml</c> whose root is a <c>PluginData</c> document.
    /// Returns the filename (relative to the folder) or null. Used as a fallback
    /// when the sources.xml hint is absent (e.g. Magnetar rewrote sources.xml).
    /// </summary>
    public static string FindInFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            return null;

        try
        {
            foreach (string path in Directory.EnumerateFiles(folder, "*.xml", SearchOption.TopDirectoryOnly))
            {
                if (LooksLikeManifest(path))
                    return Path.GetFileName(path);
            }
        }
        catch
        {
            // Ignore — treat an unreadable folder as "no manifest found".
        }
        return null;
    }

    private static bool LooksLikeManifest(string path)
    {
        try
        {
            XElement root = XDocument.Load(path).Root;
            // A PluginData document, or anything that declares the plugin's name.
            return root != null &&
                   (root.Name.LocalName == "PluginData" || Local(root, "FriendlyName") != null);
        }
        catch
        {
            return false;
        }
    }

    private static string Local(XElement root, string name)
    {
        string v = root?.Elements().FirstOrDefault(e => e.Name.LocalName == name)?.Value?.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }
}
