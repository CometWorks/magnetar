#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using PluginSdk.Logging;

namespace PluginSdk.Storage
{
    /// <summary>
    /// Replaces a file so that a crash, kill or power cut at any point leaves
    /// either the old content or the new content, never neither and never a
    /// partial file.
    /// </summary>
    internal static class DurableFile
    {
        private static readonly Logger Log = Logger.Create("PluginSdk");
        private static readonly HashSet<string> UnsyncedFolders = new HashSet<string>(
            StringComparer.Ordinal
        );

        // One lock per file. Windows paths are case-insensitive.
        private static readonly Dictionary<string, object> FileLocks = new Dictionary<
            string,
            object
        >(
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal
        );

        /// <summary>
        /// Writes through <c>path + ".tmp"</c>, flushes it to disk, then
        /// renames it over <paramref name="path"/> in one step. Deleting the
        /// old file first would leave nothing between the two steps. Writes to
        /// the same file from several threads, and reads through
        /// <see cref="Read"/>, run one at a time, each with
        /// <paramref name="write"/> inside, so they never share the temporary
        /// file and the last one to start writing is the one left on disk.
        /// Another program holding the file open can still make the move
        /// fail. A failed
        /// move throws what File.Move with overwrite throws on newer .NET:
        /// UnauthorizedAccessException when access is denied, which includes a
        /// target another program holds open, otherwise IOException.
        /// </summary>
        /// <param name="directorySyncRequired">
        /// Outside Windows the folder is synced after the rename, so the rename
        /// survives a power cut. True throws when the folder can't be synced.
        /// False keeps the save, which is complete and in place by then, and
        /// logs a warning once for that folder.
        /// </param>
        public static void Write(
            string path,
            Action<Stream> write,
            bool directorySyncRequired = true
        )
        {
            lock (LockFor(path))
                Replace(path, write, directorySyncRequired);
        }

        /// <summary>
        /// Reads <paramref name="path"/> under the same lock as
        /// <see cref="Write"/>, so a save to it never meets this read's open
        /// handle and fails.
        /// </summary>
        public static T Read<T>(string path, Func<Stream, T> read)
        {
            lock (LockFor(path))
                using (var input = File.OpenRead(path))
                    return read(input);
        }

        private static object LockFor(string path)
        {
            string key = Path.GetFullPath(path);
            lock (FileLocks)
            {
                if (!FileLocks.TryGetValue(key, out object fileLock))
                    FileLocks[key] = fileLock = new object();
                return fileLock;
            }
        }

        private static void Replace(string path, Action<Stream> write, bool directorySyncRequired)
        {
            string temporary = path + ".tmp";
            using (
                var output = new FileStream(
                    temporary,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                )
            )
            {
                write(output);
                output.Flush(true);
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH. File.Replace
                // on Windows moves the old file away first, and netstandard2.0
                // has no File.Move with overwrite.
                if (!MoveFileEx(temporary, path, 9))
                    throw MoveFailed(Marshal.GetLastWin32Error(), path);
            }
            else
            {
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
                string folder = Path.GetDirectoryName(Path.GetFullPath(path));
                if (directorySyncRequired)
                    SyncDirectory(folder);
                else
                    TrySyncDirectory(folder);
            }
        }

        /// <summary>The exception File.Move with overwrite throws for this Win32 error.</summary>
        private static Exception MoveFailed(int error, string path)
        {
            string message = $"{new Win32Exception(error).Message} : '{path}'";
            if (error == ErrorAccessDenied)
                return new UnauthorizedAccessException(message);
            return new IOException(message, unchecked((int)0x80070000 | (error & 0xFFFF)));
        }

        private static void TrySyncDirectory(string path)
        {
            try
            {
                SyncDirectory(path);
            }
            catch (Exception e)
                when (e is IOException
                    || e is DllNotFoundException
                    || e is EntryPointNotFoundException
                )
            {
                bool first;
                lock (UnsyncedFolders)
                    first = UnsyncedFolders.Add(path);
                if (first)
                    Log.Warning(
                        $"Could not sync the folder {path} after saving a file there ({e.Message}). Files saved there are complete, but less certain to survive a power cut."
                    );
            }
        }

        /// <summary>Makes a rename or new entry in <paramref name="path"/> survive a power cut. Not for Windows.</summary>
        public static void SyncDirectory(string path)
        {
            int directory = open(path, 0);
            if (directory < 0)
            {
                var error = new Win32Exception(Marshal.GetLastWin32Error());
                throw new IOException($"Could not open {path} to sync it: {error.Message}", error);
            }
            try
            {
                if (fsync(directory) != 0)
                {
                    var error = new Win32Exception(Marshal.GetLastWin32Error());
                    throw new IOException($"Could not sync {path}: {error.Message}", error);
                }
            }
            finally
            {
                close(directory);
            }
        }

        private const int ErrorAccessDenied = 5;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string source, string destination, int flags);

        [DllImport("libc", SetLastError = true)]
        private static extern int open(string path, int flags);

        [DllImport("libc", SetLastError = true)]
        private static extern int fsync(int descriptor);

        [DllImport("libc")]
        private static extern int close(int descriptor);
    }
}
