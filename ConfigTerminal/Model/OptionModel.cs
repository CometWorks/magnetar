using System;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>Which DS file/scope an option lives in.</summary>
internal enum OptionScope
{
    /// <summary>Root of <c>SpaceEngineers-Dedicated.cfg</c> (<c>MyConfigDedicated</c>).</summary>
    DedicatedRoot,

    /// <summary>Session settings — either the cfg's <c>&lt;SessionSettings&gt;</c> template or a world's <c>&lt;Settings&gt;</c>.</summary>
    Session,
}

/// <summary>Editor widget / codec selection for an option.</summary>
internal enum OptionKind
{
    Bool,
    Int,
    UInt,
    Long,
    Float,
    Double,
    Text,
    MultilineText,
    Enum,
    UlongList,
    StringList,
    BlockTypeLimits,
    Password,
}

/// <summary>Whether a change applies to a running server via SIGHUP reload or needs a restart.</summary>
internal enum Liveness
{
    RestartRequired,
    LiveViaReload,
}

/// <summary>One member of an enum-typed option: its integer value, the exact XML name written to disk, and a human label.</summary>
internal sealed record EnumChoice(int Value, string XmlName, string Label);

/// <summary>
/// Declarative metadata for one config option — the single source of truth that
/// drives the editor UI, serialization, validation and liveness hints. See
/// Docs/ConfigTerminal.md §6.
/// </summary>
internal sealed record OptionDefinition(
    string Id,
    OptionScope Scope,
    string XmlName,
    OptionKind Kind,
    string Category,
    string Label,
    string Help,
    string Default,
    double? Min = null,
    double? Max = null,
    double? Step = null,
    EnumChoice[] Choices = null,
    Liveness Liveness = Liveness.RestartRequired,
    bool Hidden = false,
    bool Experimental = false,
    string ExperimentalRule = null)
{
    /// <summary>The XML name of the enum member for a given raw value, or the raw value if not an enum / not matched.</summary>
    public string NormalizeEnum(string raw)
    {
        if (Kind != OptionKind.Enum || Choices == null || raw == null)
            return raw;

        string trimmed = raw.Trim();
        foreach (EnumChoice c in Choices)
        {
            if (string.Equals(c.XmlName, trimmed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Label, trimmed, StringComparison.OrdinalIgnoreCase)
                || c.Value.ToString() == trimmed)
                return c.XmlName;
        }
        return trimmed;
    }
}
