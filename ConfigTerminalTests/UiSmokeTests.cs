using System;
using System.IO;
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
        var driver = new FakeDriver();
        // FakeMainLoop is internal to Terminal.Gui; construct it via reflection so
        // the smoke test can run entirely headless.
        Type fmlType = typeof(FakeDriver).Assembly.GetType("Terminal.Gui.FakeMainLoop");
        var mainLoop = (IMainLoopDriver)Activator.CreateInstance(
            fmlType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null, new object[] { driver }, null);
        Application.Init(driver, mainLoop);
        try
        {
            TurboVisionTheme.Apply();

            var binding = new InstanceBinding
            {
                DataDir = dir,
                MagnetarConfigDir = dir,
                MagnetarExePath = Path.Combine(dir, "nolauncher"),
                Ds64Dir = null,
            };

            var shell = new AppShell(binding);
            Application.RunState rs = Application.Begin(shell);

            // Pump a handful of iterations so timers/layout run at least once.
            for (int i = 0; i < 3; i++)
            {
                bool wait = false;
                Application.RunMainLoopIteration(ref rs, false, ref wait);
            }

            // Exercise navigation into the main content views.
            shell.ShowServerSettings();
            shell.ShowWorlds();
            shell.ShowNewWorldDefaults();
            shell.ShowPlugins();
            shell.ShowHubPlugins();
            shell.ShowPluginSources();
            shell.ShowModSources();
            shell.ShowProfiles();
            for (int i = 0; i < 2; i++)
            {
                bool wait = false;
                Application.RunMainLoopIteration(ref rs, false, ref wait);
            }

            Application.End(rs);
        }
        finally
        {
            Application.Shutdown();
        }
    }
}
