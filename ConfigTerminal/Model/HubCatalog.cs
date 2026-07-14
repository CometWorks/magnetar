using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Magnetar.ConfigTerminal.Model;

internal enum HubPluginKind { GitHub, Mod, Obsolete, Unknown }

/// <summary>
/// One plugin entry read from a hub/plugin catalog cache. Mirrors the subset of
/// Magnetar's <c>PluginData</c> (namespace <c>Pulsar.Shared.Data</c>) that the
/// tool needs to browse and enable a plugin — never the runtime type itself.
/// </summary>
internal sealed class HubPluginInfo
{
    public string Id;            // PluginData.Id — the key stored in Profile.GitHub
    public string FriendlyName;
    public string Author;
    public string Tooltip;
    public string Description;
    public bool Hidden;
    public string[] DependencyIds = Array.Empty<string>();
    public HubPluginKind Kind = HubPluginKind.Unknown;
    public string RepoId;        // GitHubPlugin.RepoId, e.g. "CometWorks/block-limits" (for display / links)
    public string SourceLabel;   // hub source Name (e.g. "MagnetarHub") or the .bin file stem
}

/// <summary>
/// Reads Magnetar's cached plugin catalogs — the protobuf-net blobs Magnetar
/// downloads into <c>Sources/Hubs/*.bin</c> (a <c>PluginData[]</c>) and
/// <c>Sources/Plugins/*.bin</c> (a single-element <c>PluginData[]</c>). Parsed by
/// wire-field number with <see cref="ProtoReader"/> so the tool never references
/// <c>Shared</c>/protobuf-net; unknown fields are skipped so a schema growth
/// degrades gracefully. Offline by design: it reflects exactly what Magnetar has
/// already fetched, so browsing needs no network.
/// </summary>
internal static class HubCatalog
{
    // PluginData [ProtoMember] field numbers (Shared/Data/PluginData.cs).
    private const int FieldId = 1;
    private const int FieldFriendlyName = 2;
    private const int FieldHidden = 3;
    private const int FieldGroupId = 4;
    private const int FieldTooltip = 5;
    private const int FieldAuthor = 6;
    private const int FieldDescription = 7;
    private const int FieldRuntimes = 8;
    private const int FieldDependencyIds = 9;
    private const int FieldPlatforms = 10;

    // [ProtoInclude] subtype markers on PluginData.
    private const int IncludeObsolete = 100;
    private const int IncludeGitHub = 103;
    private const int IncludeMod = 104;

    // GitHubPlugin.RepoId (Shared/Data/GitHubPlugin.cs [ProtoMember(6)]).
    private const int GitHubFieldRepoId = 6;

    /// <summary>Parses a serialized <c>PluginData[]</c> blob into plugin entries.</summary>
    public static List<HubPluginInfo> Parse(byte[] blob, string sourceLabel = null)
    {
        var result = new List<HubPluginInfo>();
        if (blob == null || blob.Length == 0)
            return result;

        // The root is a bare array/list: protobuf-net emits each element as a
        // repeated length-delimited field 1.
        var root = new ProtoReader(blob);
        while (root.ReadTag(out int field, out int wire))
        {
            if (field == 1 && wire == ProtoReader.WireLengthDelimited)
            {
                HubPluginInfo info = ReadPlugin(root.ReadMessage());
                if (info != null)
                {
                    info.SourceLabel = sourceLabel;
                    result.Add(info);
                }
            }
            else
            {
                root.Skip(wire);
            }
        }
        return result;
    }

    /// <summary>Reads and parses one catalog <c>.bin</c> file; empty list on any error.</summary>
    public static List<HubPluginInfo> ReadFile(string binPath, string sourceLabel = null)
    {
        try
        {
            if (!File.Exists(binPath) || new FileInfo(binPath).Length == 0)
                return new List<HubPluginInfo>();
            return Parse(File.ReadAllBytes(binPath), sourceLabel);
        }
        catch
        {
            return new List<HubPluginInfo>();
        }
    }

    private static HubPluginInfo ReadPlugin(ProtoReader r)
    {
        var info = new HubPluginInfo();
        var deps = new List<string>();

        while (r.ReadTag(out int field, out int wire))
        {
            switch (field)
            {
                case FieldId when wire == ProtoReader.WireLengthDelimited:
                    info.Id = r.ReadString();
                    break;
                case FieldFriendlyName when wire == ProtoReader.WireLengthDelimited:
                    info.FriendlyName = r.ReadString();
                    break;
                case FieldHidden when wire == ProtoReader.WireVarint:
                    info.Hidden = r.ReadVarint() != 0;
                    break;
                case FieldGroupId when wire == ProtoReader.WireLengthDelimited:
                    r.ReadString(); // GroupId — not surfaced yet
                    break;
                case FieldTooltip when wire == ProtoReader.WireLengthDelimited:
                    info.Tooltip = r.ReadString();
                    break;
                case FieldAuthor when wire == ProtoReader.WireLengthDelimited:
                    info.Author = r.ReadString();
                    break;
                case FieldDescription when wire == ProtoReader.WireLengthDelimited:
                    info.Description = r.ReadString();
                    break;
                case FieldRuntimes when wire == ProtoReader.WireLengthDelimited:
                    r.ReadString();
                    break;
                case FieldPlatforms when wire == ProtoReader.WireLengthDelimited:
                    r.ReadString();
                    break;
                case FieldDependencyIds when wire == ProtoReader.WireLengthDelimited:
                    deps.Add(r.ReadString());
                    break;
                case IncludeObsolete when wire == ProtoReader.WireLengthDelimited:
                    info.Kind = HubPluginKind.Obsolete;
                    r.ReadMessage();
                    break;
                case IncludeGitHub when wire == ProtoReader.WireLengthDelimited:
                    info.Kind = HubPluginKind.GitHub;
                    info.RepoId = ReadGitHubRepoId(r.ReadMessage());
                    break;
                case IncludeMod when wire == ProtoReader.WireLengthDelimited:
                    info.Kind = HubPluginKind.Mod;
                    r.ReadMessage();
                    break;
                default:
                    r.Skip(wire);
                    break;
            }
        }

        info.DependencyIds = deps.ToArray();
        if (string.IsNullOrEmpty(info.FriendlyName))
            info.FriendlyName = info.RepoId ?? info.Id ?? "(unnamed)";
        return string.IsNullOrEmpty(info.Id) ? null : info;
    }

    private static string ReadGitHubRepoId(ProtoReader r)
    {
        string repoId = null;
        while (r.ReadTag(out int field, out int wire))
        {
            if (field == GitHubFieldRepoId && wire == ProtoReader.WireLengthDelimited)
                repoId = r.ReadString();
            else
                r.Skip(wire);
        }
        return repoId;
    }
}
