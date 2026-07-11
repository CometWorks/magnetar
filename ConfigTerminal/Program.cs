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
}
