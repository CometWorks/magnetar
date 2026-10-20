using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Magnetar.Legacy.Preparation;

/// <summary>
/// Pins a dev folder build to its git checkout. The whole repository must be clean, so the
/// HEAD commit describes exactly the files the build read.
/// </summary>
internal static class DevFolderProvenance
{
    private const int MaxListedPaths = 10;
    private const int GitTimeout = 60000;

    /// <summary>Returns the HEAD commit of the clean git repository holding <paramref name="folder"/>.</summary>
    /// <param name="plugin">Names the plugin in errors.</param>
    internal static string CleanHead(string plugin, string folder)
    {
        string root = Git(plugin, folder, "rev-parse --show-toplevel").Trim();
        // Untracked files count, since the dev folder build compiles them too; ignored files don't.
        string[] dirty = Git(plugin, root, "status --porcelain --untracked-files=all")
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Length > 3 ? line.Substring(3) : line)
            .ToArray();
        if (dirty.Length > 0)
            throw new InvalidDataException(
                "Dev folder "
                    + plugin
                    + " has uncommitted changes in "
                    + root
                    + "; managed preparation pins the HEAD commit, so commit, stash or ignore them first: "
                    + string.Join(", ", dirty.Take(MaxListedPaths))
                    + (
                        dirty.Length > MaxListedPaths
                            ? " and " + (dirty.Length - MaxListedPaths) + " more"
                            : ""
                    )
            );
        return Git(plugin, root, "rev-parse --verify HEAD").Trim();
    }

    private static string Git(string plugin, string directory, string arguments)
    {
        var startInfo = new ProcessStartInfo("git", "-c core.fsmonitor=false " + arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = directory,
        };
        // Status must not refresh the index of a checkout another process may be using.
        startInfo.EnvironmentVariables["GIT_OPTIONAL_LOCKS"] = "0";
        Process process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception e)
        {
            throw new InvalidDataException(
                "Dev folder " + plugin + " needs git to pin its commit: " + e.Message,
                e
            );
        }
        using (process)
        {
            var error = process.StandardError.ReadToEndAsync();
            string output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(GitTimeout))
            {
                process.Kill();
                throw new TimeoutException(
                    "git " + arguments + " timed out for dev folder " + plugin + " in " + directory
                );
            }
            if (process.ExitCode != 0)
                throw new InvalidDataException(
                    "Dev folder "
                        + plugin
                        + " must be a git checkout to pin its commit; git "
                        + arguments
                        + " failed in "
                        + directory
                        + ": "
                        + error.Result.Trim()
                );
            return output;
        }
    }
}
