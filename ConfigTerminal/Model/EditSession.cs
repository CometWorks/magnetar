using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Magnetar.ConfigTerminal.Io;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>A validation problem on one option; warnings do not block saving.</summary>
internal sealed class OptionIssue
{
    public OptionIssue(string optionId, string message, bool isError)
    {
        OptionId = optionId;
        Message = message;
        IsError = isError;
    }

    public string OptionId { get; }
    public string Message { get; }
    public bool IsError { get; }
}

/// <summary>
/// Dirty-tracking + validation for one open document. Dirty is decided by
/// content comparison against a snapshot taken at open/save, so editing a value
/// back to its original clears the flag. Save runs validate → backup → atomic
/// write → new snapshot (backup/atomic handled by <see cref="AtomicFile"/>).
/// </summary>
internal sealed class EditSession
{
    private readonly IReadOnlyList<OptionDefinition> options;
    private string snapshot;

    public EditSession(ConfigDocumentBase document, IReadOnlyList<OptionDefinition> options)
    {
        Document = document;
        this.options = options;
        snapshot = document.ToCanonicalString();
    }

    public ConfigDocumentBase Document { get; }

    public event Action DirtyChanged;

    public bool IsDirty => Document.ToCanonicalString() != snapshot;

    /// <summary>Call after any mutation so listeners (title bar, status) refresh.</summary>
    public void NotifyChanged() => DirtyChanged?.Invoke();

    /// <summary>Options whose current value differs from the registry default (for the save summary).</summary>
    public IReadOnlyList<OptionDefinition> ChangedFromDefault() =>
        options.Where(o => Document.IsSet(o) && Document.Get(o) != o.Default).ToList();

    public IReadOnlyList<OptionIssue> Validate()
    {
        var issues = new List<OptionIssue>();

        foreach (OptionDefinition o in options)
        {
            if (!Document.IsSet(o))
                continue;

            string raw = Document.Get(o);

            // Blocking errors (bad number / out of range) are shared with the
            // per-field live check so the form and the save agree on validity.
            string error = ErrorFor(o, raw);
            if (error != null)
                issues.Add(new OptionIssue(o.Id, error, true));

            // Non-blocking warnings do not prevent saving.
            if (o.Kind == OptionKind.Enum && o.Choices != null && !o.Choices.Any(c =>
                    c.XmlName.Equals(raw, StringComparison.OrdinalIgnoreCase)
                    || c.Value.ToString() == raw))
                issues.Add(new OptionIssue(o.Id, $"{o.Label}: '{raw}' is not a known value.", false));

            if (o.Experimental && o.Kind == OptionKind.Bool && ConfigDocumentBase.ParseBool(raw))
                issues.Add(new OptionIssue(o.Id, $"{o.Label} enables experimental mode.", false));
        }

        CrossFieldChecks(issues);
        return issues;
    }

    /// <summary>
    /// Blocking error for a candidate raw value, or null when it is acceptable.
    /// Only type/range problems block; unknown enums and experimental toggles are
    /// warnings and return null. Used both by <see cref="Validate"/> and by the
    /// form's live per-field validation.
    /// </summary>
    public static string ErrorFor(OptionDefinition o, string raw)
    {
        switch (o.Kind)
        {
            case OptionKind.Int:
            case OptionKind.UInt:
            case OptionKind.Long:
                if (!ConfigDocumentBase.TryParseLong(raw, out long lv))
                    return $"{o.Label}: '{raw}' is not a whole number.";
                return RangeError(o, lv);
            case OptionKind.Float:
            case OptionKind.Double:
                if (!ConfigDocumentBase.TryParseDouble(raw, out double dv))
                    return $"{o.Label}: '{raw}' is not a number.";
                return RangeError(o, dv);
        }
        return null;
    }

    private static string RangeError(OptionDefinition o, double value)
    {
        if (o.Min.HasValue && value < o.Min.Value)
            return $"{o.Label}: below minimum {Fmt(o.Min.Value)}.";
        if (o.Max.HasValue && value > o.Max.Value)
            return $"{o.Label}: above maximum {Fmt(o.Max.Value)}.";
        return null;
    }

    private void CrossFieldChecks(List<OptionIssue> issues)
    {
        // Port collisions between the dedicated ports the DS would fail to bind.
        if (Document is DedicatedConfigDocument)
        {
            var ports = new (string id, string label)[]
            {
                ("Dedicated.ServerPort", "Server Port"),
                ("Dedicated.SteamPort", "Steam Port"),
                ("Dedicated.RemoteApiPort", "Remote API Port"),
            };
            var seen = new Dictionary<long, string>();
            foreach ((string id, string label) in ports)
            {
                OptionDefinition def = OptionRegistry.ById(id);
                if (def == null) continue;
                if (!ConfigDocumentBase.TryParseLong(Document.Get(def), out long p))
                    continue;
                if (seen.TryGetValue(p, out string other))
                    issues.Add(new OptionIssue(id, $"{label} collides with {other} (both {p}).", true));
                else
                    seen[p] = label;
            }
        }
    }

    private static string Fmt(double v) => v.ToString(CultureInfo.InvariantCulture);

    public bool HasErrors() => Validate().Any(i => i.IsError);

    /// <summary>Atomically writes the document and rebases the snapshot.</summary>
    public void Save(AtomicFile writer)
    {
        Document.Save(writer);
        snapshot = Document.ToCanonicalString();
        NotifyChanged();
    }

    /// <summary>Rebases the snapshot to the current content (e.g. after "keep mine" on external change).</summary>
    public void Rebase() => snapshot = Document.ToCanonicalString();
}
