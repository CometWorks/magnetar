using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Magnetar.ConfigTerminal.Io;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>One saved plugin profile on disk (a <c>Profiles/&lt;Key&gt;.xml</c> file).</summary>
internal sealed class ProfileInfo
{
    public string Name;         // <Name> element
    public string Key;          // file-name stem (CleanFileName(Name))
    public string FilePath;
    public bool MatchesActive;  // its enabled set is identical to Current's
}

/// <summary>
/// Manages the instance's plugin <em>profiles</em> — named presets of enabled
/// plugins stored as <c>Profiles/&lt;Key&gt;.xml</c>, with <c>Current.xml</c> the
/// active set the server loads. Mirrors Magnetar's own
/// <c>Pulsar.Shared.Config.ProfilesConfig</c> (load, save/add, update, rename,
/// remove) but from outside the game, editing the files directly through
/// <see cref="AtomicFile"/>. "Current" is reserved and never listed as a preset.
/// </summary>
internal sealed class ProfileCatalog
{
    private const string CurrentKey = "Current";

    private readonly string configDir;
    private readonly AtomicFile writer;

    public ProfileCatalog(string magnetarConfigDir, AtomicFile writer)
    {
        configDir = magnetarConfigDir;
        this.writer = writer;
    }

    private string ProfilesDir => PluginProfileDocument.ProfilesDir(configDir);

    /// <summary>The saved named profiles (excludes the active Current.xml and .bak files).</summary>
    public IReadOnlyList<ProfileInfo> NamedProfiles()
    {
        var result = new List<ProfileInfo>();
        if (!Directory.Exists(ProfilesDir))
            return result;

        string activeSig = ActiveSignature();
        foreach (string file in Directory.EnumerateFiles(ProfilesDir, "*.xml"))
        {
            string stem = Path.GetFileNameWithoutExtension(file);
            if (string.Equals(stem, CurrentKey, StringComparison.OrdinalIgnoreCase))
                continue;
            PluginProfileDocument doc = PluginProfileDocument.OpenNamed(configDir, stem);
            result.Add(new ProfileInfo
            {
                Name = doc.Name,
                Key = stem,
                FilePath = file,
                MatchesActive = doc.CollectionsSignature() == activeSig,
            });
        }
        return result.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>The key of the saved profile whose enabled set matches Current, or null.</summary>
    public string ActiveMatchKey() => NamedProfiles().FirstOrDefault(p => p.MatchesActive)?.Key;

    private string ActiveSignature() => PluginProfileDocument.Open(configDir).CollectionsSignature();

    public bool Exists(string key) =>
        File.Exists(PluginProfileDocument.PathForKey(configDir, key));

    /// <summary>
    /// Saves the active set (Current.xml) as a new named profile. Throws if the
    /// name resolves to the reserved "Current" key; returns false (without writing)
    /// when a profile with that key already exists so the caller can confirm an
    /// overwrite via <see cref="Update"/>.
    /// </summary>
    public bool SaveCurrentAs(string name)
    {
        string key = KeyFor(name);
        if (Exists(key))
            return false;
        WriteSnapshot(name, key);
        return true;
    }

    /// <summary>Overwrites an existing named profile with the active set, keeping its name.</summary>
    public void Update(string key)
    {
        string name = PluginProfileDocument.OpenNamed(configDir, key).Name;
        WriteSnapshot(name, key);
    }

    /// <summary>Copies a named profile's enabled set into the active Current.xml.</summary>
    public void Load(string key)
    {
        PluginProfileDocument named = PluginProfileDocument.OpenNamed(configDir, key);
        PluginProfileDocument current = PluginProfileDocument.Open(configDir);
        current.CopyCollectionsFrom(named);
        current.Name = CurrentKey;   // the active profile is always "Current"
        current.Save(writer);
    }

    /// <summary>Renames a saved profile (delete old file, write under the new key) — Magnetar's semantics.</summary>
    public void Rename(string key, string newName)
    {
        string newKey = KeyFor(newName);
        PluginProfileDocument doc = PluginProfileDocument.OpenNamed(configDir, key);
        doc.Name = newName;
        doc.SaveTo(writer, PluginProfileDocument.PathForKey(configDir, newKey));
        if (!string.Equals(newKey, key, StringComparison.Ordinal))
            TryDelete(PluginProfileDocument.PathForKey(configDir, key));
    }

    /// <summary>Deletes a saved profile. The active Current.xml is never touched here.</summary>
    public void Delete(string key)
    {
        if (string.Equals(key, CurrentKey, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The active 'Current' profile cannot be deleted.");
        TryDelete(PluginProfileDocument.PathForKey(configDir, key));
    }

    private void WriteSnapshot(string name, string key)
    {
        PluginProfileDocument target = PluginProfileDocument.OpenNamed(configDir, key);
        target.CopyCollectionsFrom(PluginProfileDocument.Open(configDir));
        target.Name = name;
        target.SaveTo(writer, PluginProfileDocument.PathForKey(configDir, key));
    }

    private static string KeyFor(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Profile name cannot be empty.");
        string key = PluginProfileDocument.CleanKey(name.Trim());
        if (string.Equals(key, CurrentKey, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("'Current' is reserved for the active profile.");
        return key;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }
}
