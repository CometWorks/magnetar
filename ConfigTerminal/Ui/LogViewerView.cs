using System;
using System.Linq;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Logs;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Read-only log viewer over the game and Magnetar log files, with a follow
/// mode (tail -f). Memory-bounded via <see cref="LogTailReader"/>.
/// </summary>
internal sealed class LogViewerView : Window
{
    private readonly LogCatalog catalog;
    private readonly ListView fileList;
    private readonly TextView text;
    private readonly Label statusLabel;
    private LogTailReader reader;
    private bool following;
    private object followToken;

    public LogViewerView(InstanceBinding binding) : base("Logs")
    {
        ColorScheme = TurboVisionTheme.Window;
        Border.BorderStyle = BorderStyle.Double;

        catalog = new LogCatalog(binding);
        catalog.Scan();

        fileList = new ListView(Array.Empty<string>())
        {
            X = 1, Y = 1, Width = 34, Height = Dim.Fill(2), ColorScheme = TurboVisionTheme.Window,
        };
        fileList.SelectedItemChanged += _ => LoadSelected();

        text = new TextView
        {
            X = 36, Y = 1, Width = Dim.Fill(1), Height = Dim.Fill(2),
            ReadOnly = true, WordWrap = false, ColorScheme = TurboVisionTheme.Window,
        };

        statusLabel = new Label("End: follow · R: refresh · X: next exception")
        { X = 1, Y = Pos.AnchorEnd(1), Width = Dim.Fill(1), ColorScheme = TurboVisionTheme.Window };

        Add(new Label("Log files:") { X = 1, Y = 0 }, fileList, text, statusLabel);
        Populate();
    }

    private void Populate()
    {
        var items = catalog.Files
            .Select(f => $"{(f.IsActive ? "*" : " ")}[{f.Group.ToString()[0]}] {f.Name}")
            .ToList();
        fileList.SetSource(items);
        if (catalog.Files.Count > 0)
        {
            // Default to the active game log (where "Game ready" appears).
            LogFileInfo active = catalog.ActiveGameLog ?? catalog.Files[0];
            int idx = catalog.Files.ToList().FindIndex(f => f.Path == active.Path);
            fileList.SelectedItem = Math.Max(0, idx);
            LoadSelected();
        }
    }

    private void LoadSelected()
    {
        int i = fileList.SelectedItem;
        if (i < 0 || i >= catalog.Files.Count)
            return;
        reader = new LogTailReader(catalog.Files[i].Path);
        reader.Load();
        Render();
    }

    private void Render()
    {
        text.Text = string.Join("\n", reader.Lines);
        // Move to the bottom.
        text.MoveEnd();
    }

    public override bool ProcessKey(KeyEvent kb)
    {
        switch (kb.Key)
        {
            case Key.End:
                ToggleFollow();
                return true;
            case (Key)'r':
            case (Key)'R':
                LoadSelected();
                return true;
        }
        return base.ProcessKey(kb);
    }

    private void ToggleFollow()
    {
        following = !following;
        statusLabel.Text = following ? "FOLLOWING (End to stop)" : "End: follow · R: refresh";
        if (following)
        {
            followToken = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(700), _ =>
            {
                if (!following || reader == null)
                    return false;
                if (reader.Poll())
                    Render();
                return true;
            });
        }
        else if (followToken != null)
        {
            Application.MainLoop.RemoveTimeout(followToken);
            followToken = null;
        }
    }
}
