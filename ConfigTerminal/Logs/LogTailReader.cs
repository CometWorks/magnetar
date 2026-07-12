using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Magnetar.ConfigTerminal.Logs;

/// <summary>
/// Memory-bounded reader over a single log file: opens at EOF and keeps only the
/// last window of bytes (default 256 KB), so it stays cheap even on multi-GB
/// logs. <see cref="Load"/> establishes the window; <see cref="Poll"/> reads only
/// the bytes appended since the last read (tail -f), or reloads the window if the
/// file was truncated or rotated. Reads share the file so the DS can keep writing.
/// </summary>
internal sealed class LogTailReader
{
    private const int DefaultWindowBytes = 256 * 1024;
    private const int MaxLines = 20000; // hard cap so follow can never grow unbounded

    private readonly string path;
    private readonly int windowBytes;
    private readonly List<string> lines = new();
    private long position; // byte offset of the next unread byte
    private string pendingPartial = string.Empty; // last line so far, no newline yet

    public LogTailReader(string path, int windowBytes = DefaultWindowBytes)
    {
        this.path = path;
        this.windowBytes = windowBytes > 0 ? windowBytes : DefaultWindowBytes;
    }

    /// <summary>The lines currently held in the window, oldest first.</summary>
    public IReadOnlyList<string> Lines => lines;

    /// <summary>(Re)reads the tail window from the file. Never throws for IO problems.</summary>
    public void Load()
    {
        lines.Clear();
        pendingPartial = string.Empty;
        position = 0;

        try
        {
            using FileStream fs = Open();
            long length = fs.Length;
            long start = Math.Max(0, length - windowBytes);
            fs.Seek(start, SeekOrigin.Begin);

            byte[] buffer = new byte[length - start];
            int read = ReadFully(fs, buffer);
            position = length;

            string text = Decode(buffer, read);
            // If we started mid-file, drop the first (partial) line.
            if (start > 0)
            {
                int nl = text.IndexOf('\n');
                text = nl >= 0 ? text.Substring(nl + 1) : string.Empty;
            }
            AppendText(text);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>
    /// Reads any bytes appended since the last read. Returns true when the window
    /// changed. Detects truncation/rotation (length shrank) and reloads.
    /// </summary>
    public bool Poll()
    {
        try
        {
            using FileStream fs = Open();
            long length = fs.Length;
            if (length < position)
            {
                // File was truncated or replaced — start over.
                Load();
                return true;
            }
            if (length == position)
                return false;

            fs.Seek(position, SeekOrigin.Begin);
            byte[] buffer = new byte[length - position];
            int read = ReadFully(fs, buffer);
            position = length;

            AppendText(Decode(buffer, read));
            return read > 0;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private FileStream Open() =>
        new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    private static int ReadFully(FileStream fs, byte[] buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = fs.Read(buffer, total, buffer.Length - total);
            if (n == 0)
                break;
            total += n;
        }
        return total;
    }

    private static string Decode(byte[] buffer, int count) =>
        // Logs are UTF-8 (or ASCII); replace rather than throw on a split multibyte char.
        new UTF8Encoding(false, false).GetString(buffer, 0, count);

    /// <summary>Splits appended text into lines, carrying the trailing partial line across polls.</summary>
    private void AppendText(string text)
    {
        if (text.Length == 0)
            return;

        text = pendingPartial + text.Replace("\r\n", "\n").Replace('\r', '\n');
        int start = 0;
        int nl;
        while ((nl = text.IndexOf('\n', start)) >= 0)
        {
            lines.Add(text.Substring(start, nl - start));
            start = nl + 1;
        }
        pendingPartial = text.Substring(start);

        if (lines.Count > MaxLines)
            lines.RemoveRange(0, lines.Count - MaxLines);
    }
}
