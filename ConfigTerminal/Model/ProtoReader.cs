using System;
using System.Text;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// A tiny, forward-only reader for the Protocol Buffers wire format — just enough
/// to walk Magnetar's protobuf-net hub-catalog cache (<c>Sources/Hubs/*.bin</c> and
/// <c>Sources/Plugins/*.bin</c>) by field number, without referencing
/// <c>Shared</c>/protobuf-net or loading any Magnetar type. Unknown fields are
/// skipped, so the reader degrades gracefully if the catalog schema grows.
/// See <see cref="HubCatalog"/> for the field-number mapping.
/// </summary>
internal sealed class ProtoReader
{
    // Protobuf wire types.
    public const int WireVarint = 0;
    public const int WireFixed64 = 1;
    public const int WireLengthDelimited = 2;
    public const int WireFixed32 = 5;

    private readonly byte[] buffer;
    private int pos;
    private readonly int end;

    public ProtoReader(byte[] buffer) : this(buffer, 0, buffer?.Length ?? 0) { }

    public ProtoReader(byte[] buffer, int offset, int length)
    {
        this.buffer = buffer ?? Array.Empty<byte>();
        pos = offset;
        end = offset + length;
    }

    public bool AtEnd => pos >= end;

    /// <summary>Reads the next tag. Returns false at end of the message.</summary>
    public bool ReadTag(out int fieldNumber, out int wireType)
    {
        fieldNumber = 0;
        wireType = 0;
        if (AtEnd)
            return false;
        ulong tag = ReadVarint();
        fieldNumber = (int)(tag >> 3);
        wireType = (int)(tag & 0x7);
        return fieldNumber > 0;
    }

    public ulong ReadVarint()
    {
        ulong result = 0;
        int shift = 0;
        while (pos < end && shift < 64)
        {
            byte b = buffer[pos++];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                return result;
            shift += 7;
        }
        return result;
    }

    public string ReadString()
    {
        int len = (int)ReadVarint();
        if (len < 0 || pos + len > end)
        {
            pos = end;
            return string.Empty;
        }
        string s = Encoding.UTF8.GetString(buffer, pos, len);
        pos += len;
        return s;
    }

    /// <summary>Returns a sub-reader over the next length-delimited field's bytes.</summary>
    public ProtoReader ReadMessage()
    {
        int len = (int)ReadVarint();
        if (len < 0 || pos + len > end)
        {
            var empty = new ProtoReader(buffer, end, 0);
            pos = end;
            return empty;
        }
        var sub = new ProtoReader(buffer, pos, len);
        pos += len;
        return sub;
    }

    /// <summary>Skips a field of the given wire type.</summary>
    public void Skip(int wireType)
    {
        switch (wireType)
        {
            case WireVarint:
                ReadVarint();
                break;
            case WireFixed64:
                pos = Math.Min(end, pos + 8);
                break;
            case WireLengthDelimited:
                int len = (int)ReadVarint();
                pos = len < 0 ? end : Math.Min(end, pos + len);
                break;
            case WireFixed32:
                pos = Math.Min(end, pos + 4);
                break;
            default:
                // Unknown wire type (groups are obsolete) — bail to avoid a loop.
                pos = end;
                break;
        }
    }
}
