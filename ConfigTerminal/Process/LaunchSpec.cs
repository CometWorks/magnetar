using System;
using System.Collections.Generic;
using System.Linq;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Process;

/// <summary>
/// Builds the Magnetar launch command line from the binding. The tool owns the
/// managed switches (<c>-daemon</c>, <c>-config</c>, <c>-path</c>, <c>-ds64</c>)
/// and world selection; user extra args that would fight that are rejected.
/// </summary>
internal sealed class LaunchSpec
{
    private static readonly string[] ForbiddenExtra =
    {
        "-session:", "-ignorelastsession", "-path", "-config", "-daemon", "-ds64",
    };

    public InstanceBinding Binding { get; set; }

    /// <summary>Set for the world-creation start so the DS runs its new-world branch (§9.6).</summary>
    public bool IgnoreLastSession { get; set; }

    /// <summary>Extra launch args from tool settings (e.g. -noconsent).</summary>
    public string[] ExtraArgs { get; set; } = Array.Empty<string>();

    /// <summary>Validates extra args; returns the rejection reason or null when acceptable.</summary>
    public string RejectionReason()
    {
        foreach (string arg in ExtraArgs)
        {
            foreach (string bad in ForbiddenExtra)
            {
                bool hit = bad.EndsWith(":", StringComparison.Ordinal)
                    ? arg.StartsWith(bad, StringComparison.OrdinalIgnoreCase)
                    : arg.Equals(bad, StringComparison.OrdinalIgnoreCase);
                if (hit)
                    return $"Extra launch argument '{arg}' conflicts with a tool-managed switch ({bad}).";
            }
        }
        return null;
    }

    /// <summary>The full argument list (excluding the executable itself).</summary>
    public IReadOnlyList<string> BuildArgs()
    {
        var args = new List<string> { "-daemon" };

        if (!string.IsNullOrEmpty(Binding.MagnetarConfigDir))
        {
            args.Add("-config");
            args.Add(Binding.MagnetarConfigDir);
        }
        if (!string.IsNullOrEmpty(Binding.Ds64Dir))
        {
            args.Add("-ds64");
            args.Add(Binding.Ds64Dir);
        }
        if (!string.IsNullOrEmpty(Binding.DataDir))
        {
            args.Add("-path");
            args.Add(Binding.DataDir);
        }
        if (IgnoreLastSession)
            args.Add("-ignorelastsession");

        args.AddRange(ExtraArgs.Where(a => !string.IsNullOrWhiteSpace(a)));
        return args;
    }
}
