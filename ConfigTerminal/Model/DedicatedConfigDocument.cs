using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Magnetar.ConfigTerminal.Io;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// XDocument wrapper for <c>SpaceEngineers-Dedicated.cfg</c> (root
/// <c>MyConfigDedicated</c>). Registry options are edited via the base upsert
/// helpers; the structures with dedicated editors (access lists, password,
/// world-selection flags) get typed accessors here.
/// </summary>
internal sealed class DedicatedConfigDocument : ConfigDocumentBase
{
    private const string RootName = "MyConfigDedicated";
    private const string SessionSettingsName = "SessionSettings";
    private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

    private DedicatedConfigDocument(string filePath, XDocument xml, bool existsOnDisk)
        : base(filePath, xml, existsOnDisk)
    {
    }

    /// <summary>Opens the cfg, tolerating a missing file (a minimal skeleton is built in memory).</summary>
    public static DedicatedConfigDocument Open(string filePath)
    {
        if (File.Exists(filePath))
        {
            XDocument xml = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
            return new DedicatedConfigDocument(filePath, xml, existsOnDisk: true);
        }

        return new DedicatedConfigDocument(filePath, CreateSkeleton(), existsOnDisk: false);
    }

    private static XDocument CreateSkeleton()
    {
        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(RootName,
                new XAttribute(XNamespace.Xmlns + "xsi", Xsi.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsd", Xsd.NamespaceName),
                new XElement(SessionSettingsName)));
    }

    private XElement Root => Xml.Root;

    protected override XElement ResolveScopeRoot(OptionScope scope, bool create)
    {
        if (scope == OptionScope.DedicatedRoot)
            return Root;

        XElement settings = Root.Element(SessionSettingsName);
        if (settings == null && create)
        {
            settings = new XElement(SessionSettingsName);
            Root.Add(settings);
        }
        return settings;
    }

    // --- world selection flags (see §2.3 precedence) ---

    public bool IgnoreLastSession
    {
        get => ParseBool(Root.Element("IgnoreLastSession")?.Value);
        set => UpsertRoot("IgnoreLastSession", value ? "true" : "false");
    }

    public string LoadWorld
    {
        get => Root.Element("LoadWorld")?.Value ?? string.Empty;
        set => UpsertRoot("LoadWorld", value ?? string.Empty);
    }

    public string PremadeCheckpointPath
    {
        get => Root.Element("PremadeCheckpointPath")?.Value ?? string.Empty;
        set => UpsertRoot("PremadeCheckpointPath", value ?? string.Empty);
    }

    public string WorldName
    {
        get => Root.Element("WorldName")?.Value ?? string.Empty;
        set => UpsertRoot("WorldName", value ?? string.Empty);
    }

    // --- access lists ---

    /// <summary>Administrators are serialized as string items (<c>&lt;unsignedLong&gt;</c>) — the DS models them as List&lt;string&gt;.</summary>
    public IReadOnlyList<string> Administrators => ReadItems("Administrators");
    public IReadOnlyList<string> Banned => ReadItems("Banned");
    public IReadOnlyList<string> Reserved => ReadItems("Reserved");

    public void SetAdministrators(IEnumerable<string> ids) => WriteItems("Administrators", ids);
    public void SetBanned(IEnumerable<string> ids) => WriteItems("Banned", ids);
    public void SetReserved(IEnumerable<string> ids) => WriteItems("Reserved", ids);

    public string GroupId
    {
        get => Root.Element("GroupID")?.Value ?? "0";
        set => UpsertRoot("GroupID", value ?? "0");
    }

    // --- password (write-only; existing hash preserved unless changed) ---

    public bool HasPassword
    {
        get
        {
            string hash = Root.Element("ServerPasswordHash")?.Value;
            return !string.IsNullOrEmpty(hash);
        }
    }

    /// <summary>Sets the server password (PBKDF2, DS-identical). Null/empty clears both hash and salt.</summary>
    public void SetPassword(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            UpsertRoot("ServerPasswordHash", string.Empty);
            UpsertRoot("ServerPasswordSalt", string.Empty);
            return;
        }

        PasswordHasher.HashedPassword hashed = PasswordHasher.Hash(plaintext);
        UpsertRoot("ServerPasswordHash", hashed.Hash);
        UpsertRoot("ServerPasswordSalt", hashed.Salt);
    }

    private void UpsertRoot(string name, string value)
    {
        XElement el = Root.Element(name);
        if (el == null)
            Root.Add(new XElement(name, value));
        else
            el.Value = value;
    }

    private IReadOnlyList<string> ReadItems(string listName)
    {
        XElement list = Root.Element(listName);
        if (list == null)
            return Array.Empty<string>();

        return list.Elements("unsignedLong")
            .Select(e => e.Value?.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();
    }

    private void WriteItems(string listName, IEnumerable<string> ids)
    {
        XElement list = Root.Element(listName);
        if (list == null)
        {
            list = new XElement(listName);
            Root.Add(list);
        }
        else
        {
            list.RemoveNodes();
        }

        foreach (string id in ids.Select(s => s?.Trim()).Where(s => !string.IsNullOrEmpty(s)))
            list.Add(new XElement("unsignedLong", id));
    }
}
