using System;
using System.Collections.Generic;
using System.Linq;
using NStack;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// The generic, registry-driven settings form used for the DS global config,
/// the cfg's new-world defaults, and each world's settings. Left pane: category
/// list. Right pane: a scrollable form of field widgets. F2 saves through the
/// edit session (validate → backup → atomic write).
/// </summary>
internal sealed class OptionFormView : Window
{
    private readonly IReadOnlyList<OptionDefinition> options;
    private readonly ConfigDocumentBase document;
    private readonly EditSession session;
    private readonly AtomicFile writer;
    private readonly Action onSaved;
    private readonly string banner;

    private readonly List<string> categories;
    private readonly ListView categoryList;
    private readonly FrameView formFrame;
    private readonly ScrollView form;
    private readonly Label hint;
    private string currentCategory;

    public OptionFormView(
        string title,
        IReadOnlyList<OptionDefinition> options,
        ConfigDocumentBase document,
        EditSession session,
        AtomicFile writer,
        Action onSaved,
        string banner = null) : base(title)
    {
        this.options = options;
        this.document = document;
        this.session = session;
        this.writer = writer;
        this.onSaved = onSaved;
        this.banner = banner;

        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        int top = 0;
        if (!string.IsNullOrEmpty(banner))
        {
            Add(new Label(banner) { X = 1, Y = 0, Width = Dim.Fill(1), ColorScheme = TurboVisionTheme.Window });
            top = 1;
        }

        categories = options.Select(o => o.Category).Distinct().ToList();
        categoryList = new ListView(categories)
        {
            X = 1,
            Y = top,
            Width = 24,
            Height = Dim.Fill(2),
            ColorScheme = TurboVisionTheme.Window,
        };
        categoryList.SelectedItemChanged += _ => RebuildForm();

        formFrame = new FrameView("Options")
        {
            X = 26,
            Y = top,
            Width = Dim.Fill(1),
            Height = Dim.Fill(2),
            ColorScheme = TurboVisionTheme.Window,
        };
        form = new ScrollView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShowVerticalScrollIndicator = true,
            ShowHorizontalScrollIndicator = false,
            ColorScheme = TurboVisionTheme.Window,
        };
        formFrame.Add(form);

        hint = new Label("F2 Save · Tab move · categories on the left")
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1),
            Height = 1,
            ColorScheme = TurboVisionTheme.Window,
        };

        Add(categoryList, formFrame, hint);

        if (categories.Count > 0)
        {
            categoryList.SelectedItem = 0;
            RebuildForm();
        }
    }

    public override bool ProcessKey(KeyEvent kb)
    {
        if (kb.Key == Key.F2)
        {
            Save();
            return true;
        }
        return base.ProcessKey(kb);
    }

    private void RebuildForm()
    {
        if (categoryList.SelectedItem < 0 || categoryList.SelectedItem >= categories.Count)
            return;
        currentCategory = categories[categoryList.SelectedItem];
        form.RemoveAll();

        int y = 0;
        OptionDefinition prev = null;
        foreach (OptionDefinition def in options.Where(o => o.Category == currentCategory && !o.Hidden))
        {
            // Set a multi-line field apart from its neighbours with a blank row on
            // each side (a single blank between two adjacent multi-line fields).
            if (prev != null && (IsMultiline(prev) || IsMultiline(def)))
                y += 1;
            View widget = BuildRow(def, ref y);
            if (widget != null)
                form.Add(widget);
            prev = def;
        }

        form.ContentSize = new Size(80, Math.Max(y + 1, 1));
        form.SetNeedsDisplay();
    }

    // Vertical rows a field's editor occupies; multi-line text needs several so
    // it isn't clipped to one line and doesn't overlap the fields below it.
    private const int MultilineRows = 3;
    private static bool IsMultiline(OptionDefinition def) => def.Kind == OptionKind.MultilineText;
    private static int RowHeight(OptionDefinition def) =>
        IsMultiline(def) ? MultilineRows : 1;

    private View BuildRow(OptionDefinition def, ref int y)
    {
        const int labelWidth = 34;
        int rows = RowHeight(def);
        var container = new View { X = 0, Y = y, Width = Dim.Fill(), Height = rows };

        var label = new Label(def.Label + StatusGlyph(def))
        {
            X = 0,
            Y = 0,
            Width = labelWidth,
            ColorScheme = TurboVisionTheme.Window,
        };
        container.Add(label);

        View editor = BuildEditor(def, labelWidth);
        if (editor != null)
        {
            editor.Enter += _ => hint.Text = HintFor(def);
            container.Add(editor);
        }

        y += rows;
        return container;
    }

    private View BuildEditor(OptionDefinition def, int x)
    {
        switch (def.Kind)
        {
            case OptionKind.Bool:
            {
                var cb = new CheckBox(string.Empty) { X = x, Y = 0, Checked = document.GetBool(def) };
                cb.Toggled += _ =>
                {
                    document.Set(def, cb.Checked ? "true" : "false");
                    session.NotifyChanged();
                };
                return cb;
            }
            case OptionKind.Enum:
            {
                string[] labels = def.Choices.Select(c => c.Label).ToArray();
                int idx = Math.Max(0, Array.FindIndex(def.Choices, c =>
                    c.XmlName.Equals(document.Get(def), StringComparison.OrdinalIgnoreCase)));
                if (def.Choices.Length <= 4)
                {
                    // Lay the choices out horizontally so they all fit on the row's
                    // single line — a vertical group is clipped to Height = 1 and
                    // only its selected choice shows, hiding the rest.
                    var rg = new RadioGroup(labels.Select(l => ustring.Make(l)).ToArray())
                    {
                        X = x, Y = 0, SelectedItem = idx,
                        DisplayMode = DisplayModeLayout.Horizontal, HorizontalSpace = 2,
                    };
                    rg.SelectedItemChanged += e =>
                    {
                        document.Set(def, def.Choices[e.SelectedItem].XmlName);
                        session.NotifyChanged();
                    };
                    return rg;
                }
                var combo = new ComboBox { X = x, Y = 0, Width = 24, Height = 5 };
                combo.SetSource(labels);
                combo.SelectedItem = idx;
                combo.SelectedItemChanged += e =>
                {
                    if (e.Item >= 0 && e.Item < def.Choices.Length)
                    {
                        document.Set(def, def.Choices[e.Item].XmlName);
                        session.NotifyChanged();
                    }
                };
                return combo;
            }
            case OptionKind.MultilineText:
            {
                var tv = new TextView { X = x, Y = 0, Width = Dim.Fill(1), Height = MultilineRows, Text = document.Get(def) };
                tv.Leave += _ =>
                {
                    document.Set(def, tv.Text.ToString().TrimEnd('\n'));
                    session.NotifyChanged();
                };
                return tv;
            }
            case OptionKind.BlockTypeLimits:
            case OptionKind.StringList:
            {
                // Complex structured editors are out of scope for this form; show
                // the raw current state read-only so nothing is silently dropped.
                return new Label("(edited elsewhere)") { X = x, Y = 0, ColorScheme = TurboVisionTheme.Window };
            }
            default:
            {
                var tf = new TextField(document.Get(def) ?? string.Empty) { X = x, Y = 0, Width = 24 };
                tf.Leave += _ =>
                {
                    document.Set(def, tf.Text.ToString());
                    session.NotifyChanged();
                };
                return tf;
            }
        }
    }

    private string StatusGlyph(OptionDefinition def)
    {
        string g = document.IsSet(def) ? " •" : " ○";
        if (def.Liveness == Liveness.LiveViaReload)
            g += " ⚡";
        if (def.Experimental && def.Kind == OptionKind.Bool && document.GetBool(def))
            g += " ▲";
        return g;
    }

    private static string HintFor(OptionDefinition def)
    {
        string help = string.IsNullOrEmpty(def.Help) ? "" : def.Help + "  ";
        string live = def.Liveness == Liveness.LiveViaReload ? "applies live via reload" : "requires restart";
        return $"{help}[default: {def.Default}] <{def.XmlName}> — {live}";
    }

    private void Save()
    {
        IReadOnlyList<OptionIssue> issues = session.Validate();
        var errors = issues.Where(i => i.IsError).ToList();
        if (errors.Count > 0)
        {
            Dialogs.ErrorDetails("Cannot save", "Fix these errors before saving:",
                string.Join("\n", errors.Take(10).Select(e => "• " + e.Message)));
            return;
        }

        try
        {
            session.Save(writer);
            onSaved?.Invoke();
            var warnings = issues.Where(i => !i.IsError).ToList();
            string saved = "Saved " + System.IO.Path.GetFileName(document.FilePath) + ".";
            if (warnings.Count > 0)
                Dialogs.InfoDetails("Saved", saved,
                    "Warnings:\n" + string.Join("\n", warnings.Take(6).Select(w => "• " + w.Message)));
            else
                Dialogs.Info("Saved", saved);
        }
        catch (Exception e)
        {
            Dialogs.Error("Save failed", e.Message);
        }
    }
}
