using System;
using System.Collections.Generic;
using System.Linq;
using Magnetar.ConfigTerminal.Model;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

public class WorkshopResolverTests
{
    // A canned HTTP transport: matches a request by the ids it carries and returns
    // a pre-baked JSON body, so the resolver's parsing/expansion can be exercised
    // with no network. Records the endpoints hit for assertions.
    private sealed class FakeFetcher : IHttpFetcher
    {
        private readonly Func<string, IReadOnlyList<KeyValuePair<string, string>>, string> post;
        private readonly Func<string, string> get;
        public readonly List<string> Calls = new();

        public FakeFetcher(
            Func<string, IReadOnlyList<KeyValuePair<string, string>>, string> post = null,
            Func<string, string> get = null)
        {
            this.post = post;
            this.get = get;
        }

        public string Get(string url) { Calls.Add(url); return get?.Invoke(url) ?? "{}"; }

        public string Post(string url, IReadOnlyList<KeyValuePair<string, string>> form)
        {
            Calls.Add(url);
            return post?.Invoke(url, form) ?? "{}";
        }
    }

    [Theory]
    [InlineData("657749341", 657749341)]
    [InlineData("  657749341  ", 657749341)]
    [InlineData("https://steamcommunity.com/sharedfiles/filedetails/?id=657749341", 657749341)]
    [InlineData("https://steamcommunity.com/sharedfiles/filedetails/?id=657749341&searchtext=ore", 657749341)]
    [InlineData("https://steamcommunity.com/workshop/filedetails/?id=657749341", 657749341)]
    public void ExtractIds_pulls_the_id_from_urls_and_bare_numbers(string input, long expected)
    {
        Assert.Equal(new List<long> { expected }, WorkshopResolver.ExtractIds(input));
    }

    [Fact]
    public void ExtractIds_dedupes_and_keeps_order_for_multiple_ids()
    {
        var ids = WorkshopResolver.ExtractIds(
            "657749341 https://steamcommunity.com/sharedfiles/filedetails/?id=123456 657749341");
        Assert.Equal(new List<long> { 657749341, 123456 }, ids);
    }

    [Fact]
    public void ExtractIds_ignores_short_numbers_and_empty_input()
    {
        Assert.Empty(WorkshopResolver.ExtractIds("12345"));   // < 6 digits
        Assert.Empty(WorkshopResolver.ExtractIds(""));
        Assert.Empty(WorkshopResolver.ExtractIds(null));
    }

    [Fact]
    public void ParsePublishedFileDetails_reads_id_title_type_and_tags()
    {
        const string json = """
        {"response":{"publishedfiledetails":[
          {"publishedfileid":"657749341","result":1,"title":"Automatic Ore Pickup","file_type":0,
           "tags":[{"tag":"mod"}]}
        ]}}
        """;
        WorkshopItem item = Assert.Single(WorkshopResolver.ParsePublishedFileDetails(json));
        Assert.Equal(657749341, item.Id);
        Assert.Equal("Automatic Ore Pickup", item.DisplayName);
        Assert.True(item.Ok);
        Assert.False(item.IsCollection);
    }

    [Fact]
    public void Resolve_returns_the_friendly_name_for_a_single_mod()
    {
        var fake = new FakeFetcher(post: (_, _) => """
        {"response":{"publishedfiledetails":[
          {"publishedfileid":"657749341","result":1,"title":"Automatic Ore Pickup","file_type":0}
        ]}}
        """);

        WorkshopResolver.ResolveResult r = new WorkshopResolver(fake).Resolve(new long[] { 657749341 });

        (long id, string name) = Assert.Single(r.Mods);
        Assert.Equal(657749341, id);
        Assert.Equal("Automatic Ore Pickup", name);
        Assert.Empty(r.Warnings);
    }

    [Fact]
    public void Resolve_expands_a_collection_into_its_members_in_sort_order()
    {
        var fake = new FakeFetcher(post: (url, form) =>
        {
            if (url.Contains("GetCollectionDetails"))
                return """
                {"response":{"collectiondetails":[
                  {"publishedfileid":"999","children":[
                    {"publishedfileid":"20","sortorder":1},
                    {"publishedfileid":"10","sortorder":0}
                  ]}
                ]}}
                """;
            // GetPublishedFileDetails: the collection root, then its members.
            bool wantsCollection = form.Any(kv => kv.Value == "999");
            if (wantsCollection)
                return """
                {"response":{"publishedfiledetails":[
                  {"publishedfileid":"999","result":1,"title":"My Collection","file_type":2}
                ]}}
                """;
            return """
            {"response":{"publishedfiledetails":[
              {"publishedfileid":"10","result":1,"title":"Mod Ten","file_type":0},
              {"publishedfileid":"20","result":1,"title":"Mod Twenty","file_type":0}
            ]}}
            """;
        });

        WorkshopResolver.ResolveResult r = new WorkshopResolver(fake).Resolve(new long[] { 999 });

        Assert.Equal(new[] { (10L, "Mod Ten"), (20L, "Mod Twenty") }, r.Mods.ToArray());
    }

    [Fact]
    public void Resolve_skips_non_mods_with_a_warning()
    {
        var fake = new FakeFetcher(post: (_, _) => """
        {"response":{"publishedfiledetails":[
          {"publishedfileid":"555","result":1,"title":"Some Blueprint","file_type":0,
           "tags":[{"tag":"blueprint"}]}
        ]}}
        """);

        WorkshopResolver.ResolveResult r = new WorkshopResolver(fake).Resolve(new long[] { 555 });

        Assert.Empty(r.Mods);
        Assert.Single(r.Warnings);
    }

    [Fact]
    public void Resolve_adds_unavailable_ids_without_a_name_and_warns()
    {
        // result != 1 => the id isn't available; keep it (added by id) but warn.
        var fake = new FakeFetcher(post: (_, _) => """
        {"response":{"publishedfiledetails":[
          {"publishedfileid":"777","result":9,"title":""}
        ]}}
        """);

        WorkshopResolver.ResolveResult r = new WorkshopResolver(fake).Resolve(new long[] { 777 });

        (long id, string name) = Assert.Single(r.Mods);
        Assert.Equal(777, id);
        Assert.Equal(string.Empty, name);
        Assert.Single(r.Warnings);
    }

    [Fact]
    public void Resolve_live_reads_the_example_mod_name()
    {
        if (Environment.GetEnvironmentVariable("MAGNETAR_LIVE") != "1")
            return; // hits the real Steam Workshop API; skipped by default

        WorkshopResolver.ResolveResult r = new WorkshopResolver().Resolve(new long[] { 657749341 });

        (long id, string name) = Assert.Single(r.Mods);
        Assert.Equal(657749341, id);
        Assert.Equal("Automatic Ore Pickup", name);
    }
}
