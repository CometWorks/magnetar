using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// The live HTTP transport for <see cref="WorkshopResolver"/>: a plain
/// <see cref="HttpClient"/> with a short timeout and a friendly user agent.
/// Kept tiny and dependency-free so it builds identically on net48 and net10.0.
/// Network use is confined here; all parsing is pure and testable.
/// </summary>
internal sealed class DefaultHttpFetcher : IHttpFetcher
{
    private static readonly HttpClient Client = CreateClient();

    private static HttpClient CreateClient()
    {
#if NETFRAMEWORK
        // Steam requires TLS 1.2; the net48 default can be older. On net10.0 this
        // is handled by the platform, and ServicePointManager no longer applies.
        try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12; } catch { }
#endif
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "MagnetarConfig/1.0");
        return client;
    }

    public string Get(string url)
    {
        using HttpResponseMessage resp = Client.GetAsync(url).GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();
        return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    public string Post(string url, IReadOnlyList<KeyValuePair<string, string>> form)
    {
        using var content = new FormUrlEncodedContent(form);
        using HttpResponseMessage resp = Client.PostAsync(url, content).GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();
        return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}
