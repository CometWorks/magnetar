using System;
using System.Text;
using Terminal.Gui;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;
using Magnetar.ConfigTerminal.Ui;

namespace Magnetar.ConfigTerminal;

internal static class Program
{
    private static int Main(string[] args)
    {
        Cli cli = Cli.Parse(args);

        if (cli.Help)
        {
            Cli.PrintHelp();
            return 0;
        }
        if (cli.Error != null)
        {
            Console.Error.WriteLine(cli.Error);
            return 1;
        }

        // Headless read-only report — no Terminal.Gui, safe for scripts/CI.
        if (cli.Diag)
        {
            InstanceBinding diagBinding = cli.ToBinding();
            if (!System.IO.Directory.Exists(diagBinding.DataDir))
            {
                Console.Error.WriteLine($"Data dir does not exist: {diagBinding.DataDir}");
                return 1;
            }
            return Diagnostics.Run(diagBinding);
        }

        // Ensure the box-drawing / shade glyphs render on legacy Windows consoles.
        if (PlatformPaths.IsWindows)
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
        }

        // NetDriver is a portable fallback when curses/terminfo is broken.
        if (cli.NetDriver)
            Application.UseSystemConsole = true;

        try
        {
            Application.Init();
            TurboVisionTheme.Apply();

            InstanceBinding binding = cli.ToBinding();

            // Windows ships two launchers (Legacy = .NET Framework 4.8, Interim =
            // .NET 10). Let the operator pick which to configure when both are
            // installed; auto-select when only one is. Skipped when the launcher
            // or config dir was pinned on the command line.
            if (PlatformPaths.IsWindows && cli.MagnetarExe == null && cli.ConfigDir == null)
            {
                System.Collections.Generic.IReadOnlyList<MagnetarLauncher> launchers =
                    InstanceLocator.PresentWindowsLaunchers();
                if (launchers.Count > 0)
                {
                    MagnetarLauncher chosen = launchers.Count == 1
                        ? launchers[0]
                        : ChooseLauncher(launchers);
                    if (chosen == null)
                        return 0; // cancelled at the launcher picker
                    binding.MagnetarConfigDir = chosen.ConfigDir;
                    binding.MagnetarExePath = chosen.ExePath;
                }
            }

            // No usable data dir given and the default does not exist → picker.
            if (!cli.HasInstance && !System.IO.Directory.Exists(binding.DataDir))
            {
                InstanceBinding chosen = InstancePickerDialog.Show(binding);
                if (chosen == null)
                    return 0;
                binding = chosen;
            }

            var shell = new AppShell(binding);
            Application.Run(shell);
        }
        catch (Exception e)
        {
            Application.Shutdown();
            Console.Error.WriteLine("Fatal: " + e);
            return 1;
        }
        finally
        {
            Application.Shutdown();
        }

        return 0;
    }

    /// <summary>
    /// Startup prompt to pick which installed Windows launcher to configure.
    /// Returns the chosen launcher, or null if the operator cancelled.
    /// </summary>
    private static MagnetarLauncher ChooseLauncher(
        System.Collections.Generic.IReadOnlyList<MagnetarLauncher> launchers)
    {
        var buttons = new string[launchers.Count + 1];
        for (int i = 0; i < launchers.Count; i++)
            buttons[i] = launchers[i].Label;
        buttons[launchers.Count] = "Cancel";

        int pick = Dialogs.QueryDetails(
            "Select Magnetar",
            "Which Magnetar do you want to configure?",
            "Both launchers are installed on this machine.",
            error: false,
            buttons);

        return pick < launchers.Count ? launchers[pick] : null;
    }
}
