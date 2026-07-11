using System;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>Display-only info pulled from a checkpoint without loading the entity tree.</summary>
internal sealed class CheckpointInfo
{
    public string SessionName = string.Empty;
    public DateTime? LastSaveTime;
}

/// <summary>
/// Reads only the handful of header fields needed for display from a
/// <c>Sandbox.sbc</c> checkpoint, which may be GZip-compressed. Uses a
/// forward-only reader and stops early so it never materializes the (possibly
/// huge) grid/entity data. A fallback for worlds missing <c>Sandbox_config.sbc</c>.
/// </summary>
internal static class CheckpointReader
{
    public static CheckpointInfo TryRead(string sandboxSbcPath)
    {
        if (!File.Exists(sandboxSbcPath))
            return null;

        try
        {
            using Stream raw = File.OpenRead(sandboxSbcPath);
            using Stream stream = Decompress(raw);
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true, IgnoreWhitespace = true };
            using XmlReader reader = XmlReader.Create(stream, settings);

            var info = new CheckpointInfo();
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                switch (reader.Name)
                {
                    case "SessionName":
                        info.SessionName = reader.ReadElementContentAsString();
                        break;
                    // Once we hit the heavy sections there is nothing more we need.
                    case "Settings":
                    case "AppVersion":
                    case "SectorEncounters":
                    case "Factions":
                        if (!string.IsNullOrEmpty(info.SessionName))
                            return info;
                        break;
                }
            }
            return info;
        }
        catch
        {
            return null;
        }
    }

    private static Stream Decompress(Stream raw)
    {
        // Sniff the GZip magic (0x1F 0x8B) and unwrap; otherwise read as plain XML.
        int b0 = raw.ReadByte();
        int b1 = raw.ReadByte();
        raw.Seek(0, SeekOrigin.Begin);

        if (b0 == 0x1F && b1 == 0x8B)
            return new GZipStream(raw, CompressionMode.Decompress, leaveOpen: false);

        return raw;
    }
}
