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

        var driver = InitHeadless();
        try
        {
            var shell = new AppShell(NewBinding());
            Application.RunState rs = Application.Begin(shell);
            shell.ShowLogs();
            Pump(ref rs, 3);

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

            Application.End(rs);
        }
        finally
        {
            Application.Shutdown();
        }
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

        InitHeadless();
        try
        {
            var shell = new AppShell(NewBinding());
            Application.RunState rs = Application.Begin(shell);
            shell.ShowLogs();
            Pump(ref rs, 3);

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

            Application.End(rs);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    [Fact]
    public void Log_viewer_search_selects_the_matching_line()
    {
        SeedGameLog();

        InitHeadless();
        try
        {
            var shell = new AppShell(NewBinding());
            Application.RunState rs = Application.Begin(shell);
            shell.ShowLogs();
            Pump(ref rs, 3);

            // Drive the viewer's own search wrapper (no public entry point — it is
            // key-triggered through a modal prompt) with a preset term, as pressing
            // '/'<term><Enter> then 'n' would.
            object logView = GetContent(shell);
            logView.GetType().GetField("searchTerm", Private).SetValue(logView, "Game ready");
            logView.GetType().GetMethod("FindNext", Private).Invoke(logView, new object[] { true });
            Application.Refresh();

            // The match is selected and scrolled into view — the viewer opens at the
            // tail, so this also covers the wrap-around back up to the first match.
            TextView pane = GetLogPane(shell);
            Assert.Equal("Game ready", pane.SelectedText?.ToString());
            Assert.Equal(1, pane.CurrentRow); // second line of the seeded log

            Application.End(rs);
        }
        finally
        {
            Application.Shutdown();
        }
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
