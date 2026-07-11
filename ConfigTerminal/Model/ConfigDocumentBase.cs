using System;
using System.Globalization;
using System.Xml.Linq;
using Magnetar.ConfigTerminal.Io;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// Base for the XDocument-backed DS config wrappers. Editing is per-element
/// upsert: unknown elements, comments and the ordering of untouched elements are
/// preserved, so the tool coexists with hand edits, the DS's own saves and newer
/// game versions. Values are read tolerantly; a field the user never touched
/// stays absent so the DS's own default (for its version) still applies.
/// </summary>
internal abstract class ConfigDocumentBase
{
    protected XDocument Xml;

    protected ConfigDocumentBase(string filePath, XDocument xml, bool existsOnDisk)
    {
        FilePath = filePath;
        Xml = xml;
        ExistsOnDisk = existsOnDisk;
    }

    public string FilePath { get; }
    public bool ExistsOnDisk { get; private set; }

    /// <summary>Resolves the parent element an option's XML element lives under, optionally creating it.</summary>
    protected abstract XElement ResolveScopeRoot(OptionScope scope, bool create);

    /// <summary>Raw string value of the option's element, or its registry default when absent.</summary>
    public string Get(OptionDefinition def)
    {
        XElement parent = ResolveScopeRoot(def.Scope, create: false);
        XElement el = parent?.Element(def.XmlName);
        return el?.Value ?? def.Default;
    }

    /// <summary>True when the option's element is physically present in the document.</summary>
    public bool IsSet(OptionDefinition def)
    {
        XElement parent = ResolveScopeRoot(def.Scope, create: false);
        return parent?.Element(def.XmlName) != null;
    }

    /// <summary>Upserts the option's element, normalizing enum values to their exact XML name.</summary>
    public void Set(OptionDefinition def, string value)
    {
        XElement parent = ResolveScopeRoot(def.Scope, create: true);
        string normalized = def.NormalizeEnum(value) ?? string.Empty;

        XElement el = parent.Element(def.XmlName);
        if (el == null)
            parent.Add(new XElement(def.XmlName, normalized));
        else
            el.Value = normalized;
    }

    /// <summary>Removes the option's element so the DS default applies again. No-op when absent.</summary>
    public void Unset(OptionDefinition def)
    {
        XElement parent = ResolveScopeRoot(def.Scope, create: false);
        parent?.Element(def.XmlName)?.Remove();
    }

    public bool GetBool(OptionDefinition def) => ParseBool(Get(def));

    /// <summary>Canonical serialized form, used for content-based dirty tracking and saving.</summary>
    public string ToCanonicalString() => XmlOut.ToXmlString(Xml);

    /// <summary>Validates, backs up and atomically writes the document to disk.</summary>
    public void Save(AtomicFile writer)
    {
        writer.WriteText(FilePath, XmlOut.ToXmlString(Xml));
        ExistsOnDisk = true;
    }

    /// <summary>Replaces the in-memory document (used by Revert / external reload).</summary>
    protected void ReplaceXml(XDocument xml) => Xml = xml;

    // --- tolerant parsing helpers, matching the DS/Quasar leniency ---

    public static bool ParseBool(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        string t = raw.Trim();
        if (bool.TryParse(t, out bool b))
            return b;
        return t == "1";
    }

    public static bool TryParseLong(string raw, out long value)
        => long.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    public static bool TryParseDouble(string raw, out double value)
        => double.TryParse((raw ?? string.Empty).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
