using System;
using System.IO;
using System.Reflection;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.Ui;
using Terminal.Gui;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

/// <summary>
/// Headless UI smoke: builds the whole view tree against Terminal.Gui's
/// FakeDriver and pumps a few main-loop iterations, catching ctor/layout
/// exceptions without a real terminal. The logic lives below the UI on purpose,
/// so this stays deliberately thin.
/// </summary>
[Collection("ui-single-threaded")]
public class UiSmokeTests : IDisposable
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private readonly string dir;

    public UiSmokeTests()
    {
        dir = Path.Combine(Path.GetTempPath(), "mcui_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(dir, "Saves"));
    }

    public void Dispose()
    {
        try { Application.Shutdown(); } catch { }
        try { Directory.Delete(dir, true); } catch { }
    }

    [Fact]
    public void Shell_builds_and_pumps_without_throwing()
    {
        var driver = InitHeadless();
        try
        {
            var shell = new AppShell(NewBinding());
            Application.RunState rs = Application.Begin(shell);

            Pump(ref rs, 3);

            // Exercise navigation into the main content views.
            shell.ShowServerSettings();
            shell.ShowWorlds();
            shell.ShowNewWorldDefaults();
            shell.ShowPlugins();
            shell.ShowHubPlugins();
            shell.ShowPluginSources();
            shell.ShowProfiles();
            Pump(ref rs, 2);

            Application.End(rs);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void Log_viewer_renders_highlight_colours_into_the_cell_buffer()
    {
        SeedGameLog();
        WithLogViewer((shell, driver) =>
        {
            // The whole short log fits the pane, so opening it draws every line.
            // These fg/bg pairs are unique to the two highlighted line kinds — no other
            // widget in this view uses them — so their presence in the cell buffer
            // proves the per-line colour overrides actually reached the screen. (The
            // production view builds the same attributes the same way.)
            int readyAttr = Terminal.Gui.Attribute.Make(Color.Black, Color.Green).Value;
            int exceptionAttr = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Red).Value;

            Assert.True(BufferHasAttribute(driver, readyAttr),
                "The 'Game ready' line was not rendered with its highlight colour.");
            Assert.True(BufferHasAttribute(driver, exceptionAttr),
                "The 'Exception' line was not rendered with its highlight colour.");
        });
    }

    [Fact]
    public void Log_viewer_shows_a_full_screen_of_the_tail_on_open()
    {
        // A log longer than the pane, so "scrolled to the tail" means a screenful of
        // lines ending at the last, not just the last line on its own.
        var log = new System.Text.StringBuilder();
        for (int i = 0; i < 200; i++)
            log.Append($"2026-07-14 12:00:00.000 - log line {i:000}\n");
        File.WriteAllText(Path.Combine(dir, "SpaceEngineersDedicated.log"), log.ToString());

        WithLogViewer((shell, _) =>
        {
            // Regression: the initial scroll-to-bottom runs from the ctor before layout
            // gives the pane a height, which used to pin only the last line to the top.
            // After the deferred re-scroll on LayoutComplete, the last line sits at the
            // bottom of a full viewport.
            TextView pane = GetLogPane(shell);
            int height = pane.Frame.Height;
            Assert.True(height > 1, $"pane not laid out (height={height})");
            Assert.InRange(pane.Lines - 1, pane.TopRow, pane.TopRow + height - 1); // last line visible
            Assert.True(pane.TopRow <= pane.Lines - 2,
                $"only the last line is visible (TopRow={pane.TopRow}, Lines={pane.Lines})");
        });
    }

    [Fact]
    public void Log_viewer_search_selects_the_matching_line()
    {
        SeedGameLog();
        WithLogViewer((shell, _) =>
        {
            // Drive the viewer's own search wrapper (no public entry point — it is
            // key-triggered through a modal prompt) with a preset term, as pressing
            // '/'<term><Enter> then 'n' would.
            SetLogField(shell, "searchTerm", "Game ready");
            CallLog(shell, "FindNext", true);
            Application.Refresh();

            // The match is selected and scrolled into view — the viewer opens at the
            // tail, so this also covers the wrap-around back up to the first match.
            TextView pane = GetLogPane(shell);
            Assert.Equal("Game ready", pane.SelectedText?.ToString());
            Assert.Equal(1, pane.CurrentRow); // second line of the seeded log
        });
    }

    [Fact]
    public void Log_viewer_clear_search_drops_selection_and_restores_hint()
    {
        SeedGameLog();
        WithLogViewer((shell, _) =>
        {
            SetLogField(shell, "searchTerm", "Game ready");
            CallLog(shell, "FindNext", true);

            TextView pane = GetLogPane(shell);
            Assert.True(pane.Selecting); // a match is selected

            CallLog(shell, "ClearSearch");

            Assert.False(pane.Selecting); // selection dropped
            Assert.Equal("", (string)GetLogField(shell, "searchTerm")); // term forgotten
        });
    }

    [Fact]
    public void Log_viewer_navigates_between_highlighted_lines()
    {
        // Lines 1 (Game ready) and 3 (Exception) are the only highlighted ones.
        File.WriteAllText(
            Path.Combine(dir, "SpaceEngineersDedicated.log"),
            "loading world\n" +
            "Game ready...\n" +
            "players may now join\n" +
            "System.NullReferenceException: boom\n" +
            "shutting down\n");

        WithLogViewer((shell, _) =>
        {
            TextView pane = GetLogPane(shell);
            pane.CursorPosition = Point.Empty; // start at the top

            CallLog(shell, "GoToHighlight", true);
            Assert.Equal(1, pane.CurrentRow); // Game ready
            CallLog(shell, "GoToHighlight", true);
            Assert.Equal(3, pane.CurrentRow); // Exception
            CallLog(shell, "GoToHighlight", true);
            Assert.Equal(1, pane.CurrentRow); // wraps back to Game ready
            CallLog(shell, "GoToHighlight", false);
            Assert.Equal(3, pane.CurrentRow); // previous wraps to the last highlight
        });
    }

    [Fact]
    public void Log_viewer_esc_clears_highlight_navigation_status()
    {
        File.WriteAllText(
            Path.Combine(dir, "SpaceEngineersDedicated.log"),
            "loading world\nGame ready...\nplayers may now join\n");

        WithLogViewer((shell, _) =>
        {
            GetLogPane(shell).CursorPosition = Point.Empty;
            CallLog(shell, "GoToHighlight", true); // leaves a transient highlight status
            Assert.True((bool)GetLogField(shell, "transientStatus"));

            // Esc with no active search must still restore the default hint line.
            var esc = new View.KeyEventEventArgs(new KeyEvent(Key.Esc, new KeyModifiers()));
            CallLog(shell, "OnViewerKey", esc);

            Assert.True(esc.Handled);
            Assert.False((bool)GetLogField(shell, "transientStatus"));
            string status = ((Label)GetLogField(shell, "statusLabel")).Text.ToString();
            Assert.Contains("R: refresh", status); // back to the default hints
        });
    }

    [Fact]
    public void Log_viewer_search_honours_the_case_sensitivity_option()
    {
        SeedGameLog();
        WithLogViewer((shell, _) =>
        {
            TextView pane = GetLogPane(shell);
            SetLogField(shell, "searchTerm", "game ready"); // lower-case

            SetLogField(shell, "searchMatchCase", true);
            CallLog(shell, "FindNext", true);
            Assert.False(pane.Selecting); // case-sensitive: no match for the lower-case term

            SetLogField(shell, "searchMatchCase", false);
            CallLog(shell, "FindNext", true);
            Assert.Equal("Game ready", pane.SelectedText?.ToString()); // case-insensitive: matches
        });
    }

    [Fact]
    public void Log_viewer_search_honours_the_whole_word_option()
    {
        File.WriteAllText(
            Path.Combine(dir, "SpaceEngineersDedicated.log"),
            "an Exceptional event\n" +
            "a plain Exception here\n");

        WithLogViewer((shell, _) =>
        {
            TextView pane = GetLogPane(shell);
            pane.CursorPosition = Point.Empty;
            SetLogField(shell, "searchTerm", "Exception");
            SetLogField(shell, "searchWholeWord", true);

            CallLog(shell, "FindNext", true);

            // Whole-word skips "Exceptional" (line 0) and lands on the standalone word.
            Assert.Equal("Exception", pane.SelectedText?.ToString());
            Assert.Equal(1, pane.CurrentRow);
        });
    }

    // --- helpers ---

    private InstanceBinding NewBinding() => new()
    {
        DataDir = dir,
        MagnetarConfigDir = dir,
        MagnetarExePath = Path.Combine(dir, "nolauncher"),
        Ds64Dir = null,
    };

    // A short game log (fits the pane) with both highlight markers among plain lines.
    private void SeedGameLog() =>
        File.WriteAllText(
            Path.Combine(dir, "SpaceEngineersDedicated.log"),
            "2026-07-14 12:00:00.000 - Loading world 'Red Ship'\n" +
            "2026-07-14 12:01:00.000 - Game ready...\n" +
            "2026-07-14 12:01:01.000 - players may now join\n" +
            "System.NullReferenceException: boom\n" +
            "   at Foo.Bar()\n");

    // Boots the shell, opens the log viewer, pumps a few iterations so it lays out and
    // draws, then runs the test body with the shell and driver; always tears down.
    private void WithLogViewer(Action<AppShell, FakeDriver> body)
    {
        var driver = InitHeadless();
        try
        {
            var shell = new AppShell(NewBinding());
            Application.RunState rs = Application.Begin(shell);
            shell.ShowLogs();
            Pump(ref rs, 3);
            body(shell, driver);
            Application.End(rs);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    // --- reflection into the log viewer's private members (our own code; these
    // behaviours are key-triggered through a modal prompt, so there is no public entry
    // point the headless test can drive) ---

    private static object GetLogField(AppShell shell, string field)
    {
        object lv = GetContent(shell);
        return lv.GetType().GetField(field, Private).GetValue(lv);
    }

    private static void SetLogField(AppShell shell, string field, object value)
    {
        object lv = GetContent(shell);
        lv.GetType().GetField(field, Private).SetValue(lv, value);
    }

    private static void CallLog(AppShell shell, string method, params object[] args)
    {
        object lv = GetContent(shell);
        lv.GetType().GetMethod(method, Private).Invoke(lv, args);
    }

    // Boots Terminal.Gui against the FakeDriver with a headless main loop.
    // FakeMainLoop is internal to Terminal.Gui, so it is constructed via reflection.
    private static FakeDriver InitHeadless()
    {
        var driver = new FakeDriver();
        Type fmlType = typeof(FakeDriver).Assembly.GetType("Terminal.Gui.FakeMainLoop");
        var mainLoop = (IMainLoopDriver)Activator.CreateInstance(
            fmlType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { driver }, null);
        Application.Init(driver, mainLoop);
        TurboVisionTheme.Apply();
        return driver;
    }

    private static void Pump(ref Application.RunState rs, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            bool wait = false;
            Application.RunMainLoopIteration(ref rs, false, ref wait);
        }
    }

    // The shell's current content view — our own code, reached through a private
    // field the smoke test needs but which has no public accessor.
    private static object GetContent(AppShell shell)
    {
        object content = typeof(AppShell).GetField("content", Private).GetValue(shell);
        Assert.IsType<LogViewerView>(content);
        return content;
    }

    // The log viewer's read-only text pane.
    private static TextView GetLogPane(AppShell shell) =>
        (TextView)GetContent(shell).GetType().GetField("text", Private).GetValue(GetContent(shell));

    // Scans the FakeDriver's [row, col, {rune, attribute, dirty}] cell buffer for
    // any cell carrying the given attribute value.
    private static bool BufferHasAttribute(FakeDriver driver, int attribute)
    {
        int[,,] contents = driver.Contents;
        for (int row = 0; row < contents.GetLength(0); row++)
            for (int col = 0; col < contents.GetLength(1); col++)
                if (contents[row, col, 1] == attribute)
                    return true;
        return false;
    }
}
