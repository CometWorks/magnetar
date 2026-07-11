using System.Collections.Generic;
using System.Linq;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>One workshop mod entry. List order is the SE load order.</summary>
internal sealed class ModItem
{
    public ulong PublishedFileId;
    public string FriendlyName = string.Empty;
    public string ServiceName = "Steam";
    public bool IsDependency;

    /// <summary>The DS always names the mod file "{id}.sbm"; derived, never stored separately.</summary>
    public string Name => PublishedFileId + ".sbm";
}

/// <summary>Ordered mod list with reorder + validation.</summary>
internal sealed class ModList
{
    public List<ModItem> Items { get; } = new();

    public void MoveUp(int i)
    {
        if (i > 0 && i < Items.Count)
            (Items[i - 1], Items[i]) = (Items[i], Items[i - 1]);
    }

    public void MoveDown(int i)
    {
        if (i >= 0 && i < Items.Count - 1)
            (Items[i + 1], Items[i]) = (Items[i], Items[i + 1]);
    }

    /// <summary>Duplicate or zero ids the DS would reject.</summary>
    public IReadOnlyList<string> Validate()
    {
        var issues = new List<string>();
        if (Items.Any(m => m.PublishedFileId == 0))
            issues.Add("A mod has a zero PublishedFileId.");

        var dupes = Items.GroupBy(m => m.PublishedFileId)
            .Where(g => g.Key != 0 && g.Count() > 1)
            .Select(g => g.Key);
        foreach (ulong id in dupes)
            issues.Add($"Mod id {id} appears more than once.");

        return issues;
    }
}
