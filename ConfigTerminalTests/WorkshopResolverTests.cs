using System.Collections.Generic;
using System.Linq;
using Magnetar.ConfigTerminal.Model;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

public class WorkshopResolverTests
{
    // A scripted HTTP transport: matches on a substring of the URL.
    private sealed class FakeFetcher : IHttpFetcher
    {
        private readonly List<(string match, string body)> gets = new();
        private readonly List<(string match, string body)> posts = new();

        public FakeFetcher OnGet(string match, string body) { gets.Add((match, body)); return this; }
        public FakeFetcher OnPost(string match, string body) { posts.Add((match, body)); return this; }

        public string Get(string url) =>
            gets.FirstOrDefault(g => url.Contains(g.match)).body ?? "{}";

        public string Post(string url, IReadOnlyList<KeyValuePair<string, string>> form) =>
            posts.FirstOrDefault(p => url.Contains(p.match)).body ?? "{}";
    }

    [Fact]
    public void ExtractIds_from_urls_and_text()
    {
        List<long> ids = WorkshopResolver.ExtractIds(
            "https://steamcommunity.com/sharedfiles/filedetails/?id=1234567 and 7654321, dup 1234567");
        Assert.Equal(new long[] { 1234567, 7654321 }, ids);
    }

    [Fact]
    public void Parses_published_file_details()
    {
        string json = @"{""response"":{""publishedfiledetails"":[
            {""publishedfileid"":""111"",""result"":1,""title"":""Cool Mod"",""file_type"":0,
             ""tags"":[{""tag"":""Mod""}],""children"":[{""publishedfileid"":""222"",""sortorder"":0}]},
            {""publishedfileid"":""999"",""result"":9,""title"":""Gone""}
        ]}}";
        List<WorkshopItem> items = WorkshopResolver.ParsePublishedFileDetails(json);
        Assert.Equal(2, items.Count);

        WorkshopItem cool = items.First(i => i.Id == 111);
        Assert.True(cool.Ok);
        Assert.Equal("Cool Mod", cool.Title);
        Assert.Equal(new long[] { 222 }, cool.ChildIds);
        Assert.False(cool.IsClearlyNonMod);

        Assert.False(items.First(i => i.Id == 999).Ok);
    }

    [Fact]
    public void Parses_collection_details_ordered()
    {
        string json = @"{""response"":{""collectiondetails"":[
            {""publishedfileid"":""500"",""children"":[
                {""publishedfileid"":""22"",""sortorder"":1},
                {""publishedfileid"":""11"",""sortorder"":0}]}
        ]}}";
        Dictionary<long, List<long>> map = WorkshopResolver.ParseCollectionDetails(json);
        Assert.Equal(new long[] { 11, 22 }, map[500]); // sorted by sortorder
    }

    [Fact]
    public void ResolveNames_expands_collections_and_skips_nonmods()
    {
        // The first GetPublishedFileDetails call returns the collection + a plain
        // mod; after collection expansion a second call resolves the members. The
        // SequencedFetcher answers those two calls in order.
        var fetcher = new SequencedFetcher(
            post500And111: @"{""response"":{""publishedfiledetails"":[
                {""publishedfileid"":""500"",""result"":1,""title"":""My Collection"",""file_type"":2},
                {""publishedfileid"":""111"",""result"":1,""title"":""Cool Mod""}
            ]}}",
            postMembers: @"{""response"":{""publishedfiledetails"":[
                {""publishedfileid"":""222"",""result"":1,""title"":""Dependency A""},
                {""publishedfileid"":""333"",""result"":1,""title"":""Blueprint X"",""tags"":[{""tag"":""blueprint""}]}
            ]}}",
            collection: @"{""response"":{""collectiondetails"":[
                {""publishedfileid"":""500"",""children"":[
                    {""publishedfileid"":""222"",""sortorder"":0},
                    {""publishedfileid"":""333"",""sortorder"":1}]}
            ]}}");

        var resolver = new WorkshopResolver(fetcher);
        Dictionary<long, string> names = resolver.ResolveNames(
            new long[] { 500, 111 }, out List<string> warnings, out List<long> collections);

        Assert.Equal("Cool Mod", names[111]);
        Assert.Equal("Dependency A", names[222]);
        Assert.False(names.ContainsKey(500));  // collection itself is not a mod
        Assert.False(names.ContainsKey(333));  // blueprint skipped
        Assert.Contains(warnings, w => w.Contains("333"));
        Assert.Equal(new long[] { 500 }, collections); // the pasted collection id is reported
    }

    [Fact]
    public void ResolveDependencies_without_key_returns_input_with_warning()
    {
        var resolver = new WorkshopResolver(new FakeFetcher());
        DependencyResult r = resolver.ResolveDependencies(new[] { (111L, "Cool Mod") }, apiKey: null);
        Assert.Single(r.Mods);
        Assert.Equal(0, r.AddedDependencies);
        Assert.Contains(r.Warnings, w => w.Contains("API key"));
    }

    [Fact]
    public void ResolveDependencies_bfs_appends_transitive_deps()
    {
        // 111 depends on 222; 222 depends on 333. A single GetDetails call returns
        // details for whatever ids were requested; the SequencedFetcher answers
        // each batch from a lookup so children resolve transitively.
        var fetcher = new GraphFetcher(new Dictionary<long, (string name, long[] children, string[] tags)>
        {
            [111] = ("Root Mod", new long[] { 222 }, null),
            [222] = ("Dep A", new long[] { 333 }, null),
            [333] = ("Dep B", new long[0], null),
        });

        var resolver = new WorkshopResolver(fetcher);
        DependencyResult r = resolver.ResolveDependencies(new[] { (111L, "Root Mod") }, apiKey: "KEY");

        Assert.Equal(2, r.AddedDependencies);
        Assert.Equal(new long[] { 111, 222, 333 }, r.Mods.Select(m => m.Id).ToArray());
        Assert.True(r.Mods[0].IsDependency == false);
        Assert.True(r.Mods[1].IsDependency);
        Assert.True(r.Mods[2].IsDependency);
    }

    // A fetcher that answers GetPublishedFileDetails calls in sequence.
    private sealed class SequencedFetcher : IHttpFetcher
    {
        private readonly string post500And111, postMembers, collection;
        private int postDetailsCall;

        public SequencedFetcher(string post500And111, string postMembers, string collection)
        {
            this.post500And111 = post500And111;
            this.postMembers = postMembers;
            this.collection = collection;
        }

        public string Get(string url) => "{}";

        public string Post(string url, IReadOnlyList<KeyValuePair<string, string>> form)
        {
            if (url.Contains("GetCollectionDetails"))
                return collection;
            // GetPublishedFileDetails: first call = roots, subsequent = members.
            return postDetailsCall++ == 0 ? post500And111 : postMembers;
        }
    }

    // Answers keyed GetDetails GET calls by looking up the requested ids in a graph.
    private sealed class GraphFetcher : IHttpFetcher
    {
        private readonly Dictionary<long, (string name, long[] children, string[] tags)> graph;
        public GraphFetcher(Dictionary<long, (string, long[], string[])> graph) => this.graph = graph;

        public string Post(string url, IReadOnlyList<KeyValuePair<string, string>> form) => "{}";

        public string Get(string url)
        {
            // Parse publishedfileids[i]=id back out of the query string.
            var ids = new List<long>();
            foreach (string part in url.Split('&'))
            {
                int eq = part.IndexOf('=');
                if (eq > 0 && part.Contains("publishedfileids") &&
                    long.TryParse(part.Substring(eq + 1), out long id))
                    ids.Add(id);
            }

            var sb = new System.Text.StringBuilder();
            sb.Append(@"{""response"":{""publishedfiledetails"":[");
            for (int i = 0; i < ids.Count; i++)
            {
                if (!graph.TryGetValue(ids[i], out var g))
                    continue;
                if (i > 0) sb.Append(',');
                sb.Append(@"{""publishedfileid"":""").Append(ids[i]).Append(@""",""result"":1,""title"":""").Append(g.name).Append(@"""");
                if (g.children != null && g.children.Length > 0)
                {
                    sb.Append(@",""children"":[");
                    sb.Append(string.Join(",", g.children.Select(c => $@"{{""publishedfileid"":""{c}"",""sortorder"":0}}")));
                    sb.Append(']');
                }
                sb.Append('}');
            }
            sb.Append("]}}");
            return sb.ToString();
        }
    }
}
