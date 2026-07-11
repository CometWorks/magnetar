using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Magnetar.ConfigTerminal.Io;

/// <summary>
/// Crash-safe text writes: content goes to a temp file in the same directory,
/// is flushed, then atomically renamed over the target. On any failure the temp
/// is removed and the target is left untouched — the destination is never seen
/// half-written or truncated. Before the first overwrite of an existing file in
/// this process, a <c>.bak</c> copy is made (Magnetar's existing convention).
/// </summary>
internal sealed class AtomicFile
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    // Files backed up at least once this session, so we back up once (the
    // pre-edit state), not on every save.
    private readonly HashSet<string> backedUp = new(PlatformPaths.PathComparer);

    public void WriteText(string path, string content)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        BackupOnce(path);

        string tempPath = Path.Combine(
            directory ?? ".",
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, Utf8NoBom))
            {
                writer.Write(content);
                writer.Flush();
                stream.Flush(true);
            }

            // File.Move with overwrite is atomic on the same filesystem.
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    private void BackupOnce(string path)
    {
        if (!backedUp.Add(path))
            return;

        try
        {
            if (File.Exists(path))
                File.Copy(path, path + ".bak", overwrite: true);
        }
        catch
        {
            // A missing backup must not block the real write; the atomic rename
            // below is what protects the target's integrity.
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}
