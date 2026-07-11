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

/// <summary>The result of a dependency-resolution pass over a set of mods.</summary>
internal sealed class DependencyResult
{
    public List<(long Id, string Name, bool IsDependency)> Mods = new();
    public int AddedDependencies;
    public List<string> Warnings = new();
}

/// <summary>Pluggable HTTP transport so the resolver's parsing can be unit-tested without network.</summary>
internal interface IHttpFetcher
{
    string Get(string url);
    string Post(string url, IReadOnlyList<KeyValuePair<string, string>> form);
}

/// <summary>
/// Looks up Steam Workshop mod metadata (names, collection members) and resolves
/// mod dependency graphs — the piece Magnetar itself leaves as a TODO
/// (<c>PluginList.AddMod</c>). Modelled on Quasar's <c>QuasarWorkshopModResolver</c>.
///
/// Two capability tiers: name lookup and collection expansion use the keyless
/// <c>ISteamRemoteStorage</c> endpoints; transitive dependency resolution needs a
/// Steam Web API key and the <c>IPublishedFileService/GetDetails</c> endpoint.
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

    // --- keyless: basic details (names, type, tags) ---

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

    /// <summary>
    /// Resolves display names for a set of ids, transparently expanding any
    /// collections into their members. Returns id → name for every usable mod.
    /// </summary>
    public Dictionary<long, string> ResolveNames(IEnumerable<long> ids, out List<string> warnings)
        => ResolveNames(ids, out warnings, out _);

    /// <inheritdoc cref="ResolveNames(IEnumerable{long}, out List{string})"/>
    /// <param name="collectionIds">Receives the input ids that turned out to be
    /// collections (expanded into their members) so the caller can drop them.</param>
    public Dictionary<long, string> ResolveNames(IEnumerable<long> ids, out List<string> warnings,
        out List<long> collectionIds)
    {
        warnings = new List<string>();
        collectionIds = new List<long>();
        var roots = ids.Where(x => x > 0).Distinct().ToList();
        var rootSet = new HashSet<long>(roots);
        var names = new Dictionary<long, string>();
        if (roots.Count == 0)
            return names;

        Dictionary<long, WorkshopItem> details = GetDetails(roots);

        // Expand collections one level (Quasar recurses; one level covers the
        // common "subscribe to this collection" case and keeps calls bounded).
        var collectionsToExpand = details.Values.Where(d => d.IsCollection).Select(d => d.Id).ToList();
        if (collectionsToExpand.Count > 0)
        {
            Dictionary<long, List<long>> children = GetCollectionChildren(collectionsToExpand);
            var extra = children.Values.SelectMany(c => c).Distinct().Where(id => !details.ContainsKey(id)).ToList();
            if (extra.Count > 0)
                foreach (var kv in GetDetails(extra))
                    details[kv.Key] = kv.Value;
        }

        foreach (WorkshopItem item in details.Values)
        {
            if (item.IsCollection)
            {
                if (rootSet.Contains(item.Id))
                    collectionIds.Add(item.Id);
                continue;
            }
            if (!item.Ok)
            {
                warnings.Add($"{item.Id}: not available on the Workshop.");
                continue;
            }
            if (item.IsClearlyNonMod)
            {
                warnings.Add($"{item.DisplayName} ({item.Id}): not a mod (world/blueprint/script), skipped.");
                continue;
            }
            names[item.Id] = item.DisplayName;
        }
        return names;
    }

    // --- keyed: transitive dependency resolution ---

    /// <summary>
    /// Resolves the transitive dependency closure of the given mods via the keyed
    /// <c>GetDetails</c> endpoint, BFS over each item's declared children. Returns
    /// the original mods followed by discovered dependencies (marked as such).
    /// Requires a Steam Web API key; without one, returns the input unchanged with
    /// a warning.
    /// </summary>
    public DependencyResult ResolveDependencies(IEnumerable<(long Id, string Name)> mods, string apiKey)
    {
        var result = new DependencyResult();
        var roots = mods.Where(m => m.Id > 0)
            .GroupBy(m => m.Id).Select(g => g.First()).ToList();
        var rootIds = new HashSet<long>(roots.Select(m => m.Id));

        foreach (var m in roots)
            result.Mods.Add((m.Id, m.Name, false));

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            result.Warnings.Add("No Steam Web API key set — dependency resolution needs one. Names only.");
            return result;
        }
        if (roots.Count == 0)
            return result;

        // BFS crawl of declared children (GetDetails includechildren=true).
        var detailsById = new Dictionary<long, WorkshopItem>();
        var requested = new HashSet<long>();
        var queue = new Queue<long>(rootIds);
        while (queue.Count > 0)
        {
            var chunk = new List<long>();
            while (queue.Count > 0 && chunk.Count < 100)
            {
                long id = queue.Dequeue();
                if (requested.Add(id))
                    chunk.Add(id);
            }
            if (chunk.Count == 0)
                continue;

            string json = GetDetailsWithChildren(chunk, apiKey);
            foreach (WorkshopItem item in ParsePublishedFileDetails(json))
            {
                detailsById[item.Id] = item;
                foreach (long child in item.ChildIds)
                    if (!requested.Contains(child))
                        queue.Enqueue(child);
            }
        }

        // Append newly-reachable dependencies in a stable order, preserving the
        // user's root order first (Quasar's PreserveSourceOrderAndAppendDependencies).
        var known = new HashSet<long>(rootIds);
        foreach (var root in roots)
            AppendDependencies(root.Id, detailsById, known, result);

        return result;
    }

    private void AppendDependencies(long id, Dictionary<long, WorkshopItem> details,
        HashSet<long> known, DependencyResult result)
    {
        if (!details.TryGetValue(id, out WorkshopItem item))
            return;
        foreach (long child in item.ChildIds)
        {
            if (!known.Add(child))
                continue;
            string name = details.TryGetValue(child, out WorkshopItem c) && c.Ok ? c.DisplayName : child.ToString();
            if (details.TryGetValue(child, out WorkshopItem cd) && cd.IsClearlyNonMod)
            {
                result.Warnings.Add($"{name} ({child}): dependency is not a mod, skipped.");
                continue;
            }
            result.Mods.Add((child, name, true));
            result.AddedDependencies++;
            AppendDependencies(child, details, known, result);
        }
    }

    private string GetDetailsWithChildren(IReadOnlyList<long> ids, string apiKey)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("https://api.steampowered.com/IPublishedFileService/GetDetails/v1/?key=")
          .Append(Uri.EscapeDataString(apiKey))
          .Append("&appid=").Append(SpaceEngineersAppId)
          .Append("&includechildren=true&includetags=true&short_description=true&return_details=true");
        for (int j = 0; j < ids.Count; j++)
            sb.Append($"&publishedfileids[{j}]=").Append(ids[j]);
        return http.Get(sb.ToString());
    }

    // --- static wire parsers (unit-testable, no network) ---

    /// <summary>
    /// Parses either envelope shape: <c>ISteamRemoteStorage/GetPublishedFileDetails</c>
    /// (<c>response.publishedfiledetails[]</c>, ids as <c>publishedfileid</c>) and
    /// <c>IPublishedFileService/GetDetails</c> (same, plus <c>children[]</c>).
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
