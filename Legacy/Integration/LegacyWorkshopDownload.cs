using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Pulsar.Legacy.Launcher;
using Pulsar.Shared;
using VRage.FileSystem;
using VRage.GameServices;

namespace Magnetar.Legacy.Integration;

/// <summary>
/// Fallback for legacy Workshop items (a single <c>*_legacy.bin</c> archive)
/// whose Steam download fails. Steam timed out on such items on some Windows
/// servers (k_EResultTimeout after ~20 s), while the same file downloads fine
/// over HTTP. The public Steam Web API returns a CDN <c>file_url</c> for legacy
/// items only, so newer items never take this path.
/// </summary>
public static class LegacyWorkshopDownload
{
    private const string SteamWorkshopService = "Steam";
    private const string DetailsUrl =
        "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromMinutes(2) };

    // Folder has a protected setter
    private static readonly MethodInfo FolderSetter = AccessTools.PropertySetter(
        typeof(MyWorkshopItem),
        nameof(MyWorkshopItem.Folder)
    );

    /// <summary>
    /// Downloads the legacy archive of the item into the DS Workshop folder,
    /// where Steam would install it, and points the item at the archive the
    /// way Steam does for legacy items. Returns false if the item is not a
    /// legacy Steam item or the download fails.
    /// </summary>
    public static bool TryDownload(MyWorkshopItem item, MyGameServiceCallResult steamResult)
    {
        if (item is null || item.Id == 0 || item.ServiceName != SteamWorkshopService)
            return false;

        try
        {
            JToken details = QueryDetails(item.Id);
            string url = (string)details?["file_url"];
            if (string.IsNullOrEmpty(url))
                return false;

            long size = (long?)details["file_size"] ?? -1;
            string folder = Path.Combine(
                MyFileSystem.UserDataPath,
                "content",
                Game.SteamId.ToString(),
                item.Id.ToString()
            );
            string archive = Path.Combine(folder, (string)details["hcontent_file"] + "_legacy.bin");

            if (!File.Exists(archive) || new FileInfo(archive).Length != size)
            {
                LogFile.WriteLine(
                    $"Steam download of legacy workshop item {item.Id} failed ({steamResult}), downloading it from {url}"
                );
                Directory.CreateDirectory(folder);
                string partial = archive + ".part";
                using (Stream input = Client.GetStreamAsync(url).GetAwaiter().GetResult())
                using (FileStream output = File.Create(partial))
                    input.CopyTo(output);

                if (File.Exists(archive))
                    File.Delete(archive);
                File.Move(partial, archive);
            }

            FolderSetter.Invoke(item, [archive]);
            LogFile.WriteLine($"Loading legacy workshop item {item.Id} from {archive}");
            return true;
        }
        catch (Exception e)
        {
            LogFile.Error($"Failed downloading legacy workshop item {item.Id}: {e}");
            return false;
        }
    }

    private static JToken QueryDetails(ulong id)
    {
        using FormUrlEncodedContent form = new(
            new Dictionary<string, string>
            {
                ["itemcount"] = "1",
                ["publishedfileids[0]"] = id.ToString(),
            }
        );
        using HttpResponseMessage response = Client
            .PostAsync(DetailsUrl, form)
            .GetAwaiter()
            .GetResult();
        response.EnsureSuccessStatusCode();
        string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return JObject.Parse(json)["response"]?["publishedfiledetails"]?[0];
    }
}
