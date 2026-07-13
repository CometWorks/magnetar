using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Magnetar.ConfigTerminal.Model.Json;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>Basic Steam Workshop metadata for one published file.</summary>
internal sealed class WorkshopItem
{
    public long Id;
    public int Result;           // 1 == OK
    public string Title;
    public int FileType;         // 2 == collection
    public string[] Tags = Array.Empty<string>();
    public long[] ChildIds = Array.Empty<long>();   // collection members / declared dependencies

    public bool Ok => Result == 1;
    public bool IsCollection => FileType == 2 || Tags.Any(t => string.Equals(t, "collection", StringComparison.OrdinalIgnoreCase));
    // Space Engineers Workshop content that is clearly not a mod.
    public bool IsClearlyNonMod => Tags.Any(t =>
        string.Equals(t, "world", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(t, "blueprint", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(t, "ingameScript", StringComparison.OrdinalIgnoreCase));
    public string DisplayName => string.IsNullOrWhiteSpace(Title) ? Id.ToString() : Title.Trim();
}

/// <summary>Pluggable HTTP transport so the resolver's parsing can be unit-tested without network.</summary>
internal interface IHttpFetcher
{
    string Get(string url);
    string Post(string url, IReadOnlyList<KeyValuePair<string, string>> form);
}

/// <summary>
/// Looks up Steam Workshop mod metadata (friendly names, collection members) so
/// the per-world mod-list editor can accept a Workshop URL or id and fill in the
/// name automatically instead of the user typing it. Modelled on Quasar's
/// <c>QuasarWorkshopModResolver</c>, trimmed to the keyless
/// <c>ISteamRemoteStorage</c> endpoints — no Steam Web API key required.
///
/// All wire parsing is in static methods so it is testable against captured
/// fixtures with no live calls.
/// </summary>
internal sealed class WorkshopResolver
{
    public const long SpaceEngineersAppId = 244850;

    private static readonly Regex IdPattern = new(@"(?:(?:[?&]id=)|\b)(\d{6,20})\b", RegexOptions.Compiled);

    private readonly IHttpFetcher http;

    public WorkshopResolver(IHttpFetcher fetcher = null) => http = fetcher ?? new DefaultHttpFetcher();

    /// <summary>Extracts Workshop ids from free text or Workshop/collection URLs, deduped, in order.</summary>
    public static List<long> ExtractIds(string text)
    {
        var ids = new List<long>();
        var seen = new HashSet<long>();
        if (string.IsNullOrEmpty(text))
            return ids;
        foreach (Match m in IdPattern.Matches(text))
        {
            if (long.TryParse(m.Groups[1].Value, out long id) && id > 0 && seen.Add(id))
                ids.Add(id);
        }
        return ids;
    }

    /// <summary>Fetches basic details for the given ids (name, type, tags). No API key needed.</summary>
    public Dictionary<long, WorkshopItem> GetDetails(IEnumerable<long> ids)
    {
        var list = ids.Where(x => x > 0).Distinct().ToList();
        var result = new Dictionary<long, WorkshopItem>();
        if (list.Count == 0)
            return result;

        const int batch = 100;
        for (int i = 0; i < list.Count; i += batch)
        {
            List<long> chunk = list.Skip(i).Take(batch).ToList();
            var form = new List<KeyValuePair<string, string>>
            {
                new("itemcount", chunk.Count.ToString()),
            };
            for (int j = 0; j < chunk.Count; j++)
                form.Add(new($"publishedfileids[{j}]", chunk[j].ToString()));

            string json = http.Post("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/", form);
            foreach (WorkshopItem item in ParsePublishedFileDetails(json))
                result[item.Id] = item;
        }
        return result;
    }

    /// <summary>Expands collection ids into their member ids (ordered). No API key needed.</summary>
    public Dictionary<long, List<long>> GetCollectionChildren(IEnumerable<long> collectionIds)
    {
        var list = collectionIds.Where(x => x > 0).Distinct().ToList();
        var result = new Dictionary<long, List<long>>();
        if (list.Count == 0)
            return result;

        var form = new List<KeyValuePair<string, string>>
        {
            new("collectioncount", list.Count.ToString()),
        };
        for (int j = 0; j < list.Count; j++)
            form.Add(new($"publishedfileids[{j}]", list[j].ToString()));

        string json = http.Post("https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/", form);
        foreach (var kv in ParseCollectionDetails(json))
            result[kv.Key] = kv.Value;
        return result;
    }

    /// <summary>The outcome of resolving a set of Workshop ids/URLs into addable mods.</summary>
    internal sealed class ResolveResult
    {
        /// <summary>Usable mods in stable order (typed order first, then expanded
        /// collection members), each with its resolved friendly name.</summary>
        public List<(long Id, string Name)> Mods = new();
        /// <summary>Human-readable notes about ids that were skipped (not on the
        /// Workshop, not a mod, or empty collections).</summary>
        public List<string> Warnings = new();
    }

    /// <summary>
    /// Resolves display names for a set of ids, expanding any collections (one
    /// level) into their ordered members and dropping ids that are not mods.
    /// Returns the addable mods with names, plus warnings for anything skipped.
    /// Throws on transport failure (the caller decides how to fall back offline).
    /// </summary>
    public ResolveResult Resolve(IEnumerable<long> ids)
    {
        var result = new ResolveResult();
        var roots = ids.Where(x => x > 0).Distinct().ToList();
        if (roots.Count == 0)
            return result;

        Dictionary<long, WorkshopItem> details = GetDetails(roots);

        // Expand collections one level (the common "I pasted a collection URL"
        // case) so their members get added in the collection's own sort order.
        var collections = roots.Where(id => details.TryGetValue(id, out WorkshopItem d) && d.IsCollection).ToList();
        Dictionary<long, List<long>> children = collections.Count > 0
            ? GetCollectionChildren(collections)
            : new Dictionary<long, List<long>>();
        var members = children.Values.SelectMany(c => c).Distinct().Where(id => !details.ContainsKey(id)).ToList();
        if (members.Count > 0)
            foreach (var kv in GetDetails(members))
                details[kv.Key] = kv.Value;

        // Flatten the typed ids in order, replacing each collection by its members.
        var ordered = new List<long>();
        var seen = new HashSet<long>();
        void AddId(long id)
        {
            if (seen.Add(id))
                ordered.Add(id);
        }
        foreach (long id in roots)
        {
            if (details.TryGetValue(id, out WorkshopItem d) && d.IsCollection)
            {
                if (children.TryGetValue(id, out List<long> ms) && ms.Count > 0)
                    foreach (long m in ms) AddId(m);
                else
                    result.Warnings.Add($"{d.DisplayName} ({id}): collection is empty or private, skipped.");
            }
            else
            {
                AddId(id);
            }
        }

        foreach (long id in ordered)
        {
            if (!details.TryGetValue(id, out WorkshopItem item) || !item.Ok)
            {
                result.Warnings.Add($"{id}: not available on the Workshop, added without a name.");
                result.Mods.Add((id, string.Empty));
                continue;
            }
            if (item.IsClearlyNonMod)
            {
                result.Warnings.Add($"{item.DisplayName} ({id}): not a mod (world/blueprint/script), skipped.");
                continue;
            }
            result.Mods.Add((id, item.DisplayName));
        }
        return result;
    }

    // --- static wire parsers (unit-testable, no network) ---

    /// <summary>
    /// Parses the <c>ISteamRemoteStorage/GetPublishedFileDetails</c> envelope
    /// (<c>response.publishedfiledetails[]</c>, ids as <c>publishedfileid</c>).
    /// </summary>
    public static List<WorkshopItem> ParsePublishedFileDetails(string json)
    {
        var items = new List<WorkshopItem>();
        if (string.IsNullOrWhiteSpace(json))
            return items;
        JsonValue root;
        try { root = MiniJson.Parse(json); }
        catch (FormatException) { return items; }

        JsonValue details = root["response"]["publishedfiledetails"];
        foreach (JsonValue d in details.Items)
        {
            if (!long.TryParse(d["publishedfileid"].AsString(), out long id) || id <= 0)
                continue;
            var item = new WorkshopItem
            {
                Id = id,
                Result = d["result"].AsInt(),
                Title = d["title"].AsString(),
                FileType = d["file_type"].AsInt(),
                Tags = d["tags"].Items
                    .Select(t => t["tag"].AsString())
                    .Where(t => !string.IsNullOrEmpty(t)).ToArray(),
                ChildIds = d["children"].Items
                    .Select(c => long.TryParse(c["publishedfileid"].AsString(), out long cid) ? cid : 0)
                    .Where(cid => cid > 0).ToArray(),
            };
            items.Add(item);
        }
        return items;
    }

    /// <summary>Parses <c>ISteamRemoteStorage/GetCollectionDetails</c> into collection id → ordered member ids.</summary>
    public static Dictionary<long, List<long>> ParseCollectionDetails(string json)
    {
        var result = new Dictionary<long, List<long>>();
        if (string.IsNullOrWhiteSpace(json))
            return result;
        JsonValue root;
        try { root = MiniJson.Parse(json); }
        catch (FormatException) { return result; }

        foreach (JsonValue c in root["response"]["collectiondetails"].Items)
        {
            if (!long.TryParse(c["publishedfileid"].AsString(), out long id) || id <= 0)
                continue;
            var children = c["children"].Items
                .Select(ch => (Id: long.TryParse(ch["publishedfileid"].AsString(), out long cid) ? cid : 0,
                               Sort: ch["sortorder"].AsInt()))
                .Where(x => x.Id > 0)
                .OrderBy(x => x.Sort)
                .Select(x => x.Id)
                .ToList();
            result[id] = children;
        }
        return result;
    }
}
