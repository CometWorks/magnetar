using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Magnetar.Legacy.Launcher;
using Pulsar.Shared;

internal static class Program
{
    private const int StdOutputHandle = -11;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetStdHandle(int handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    private static int Main(string[] args)
    {
        if (args.Length != 0)
        {
            LogFile.Path = args[1];
            try
            {
                if (args[0] == "attached")
                {
                    FreeConsole();
                    if (!AllocConsole())
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    if (Console.IsOutputRedirected || Console.IsErrorRedirected)
                        throw new InvalidOperationException("Expected a console for stdout and stderr.");
                }

                // Force both managed writers to cache their handles before detaching.
                Console.WriteLine("stdout before detach");
                Console.Error.WriteLine("stderr before detach");

                if (args[0] == "invalid-stdout" && !CloseHandle(GetStdHandle(StdOutputHandle)))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                Daemon.Detach();

                LogFile.WriteLine("startup reached");
                Console.WriteLine("stdout after detach");
                Console.Error.WriteLine("stderr after detach");
                LogFile.WriteLine("startup completed");
                return 0;
            }
            catch (Exception error)
            {
                LogFile.WriteLine($"{error.GetType().Name} (0x{error.HResult:X8}): {error.Message}");
                return 1;
            }
        }

        try
        {
            RunCase("redirected", redirect: true);
            RunCase("attached", redirect: false);
            RunCase("invalid-stdout", redirect: true);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static void RunCase(string mode, bool redirect)
    {
        string log = Path.GetTempFileName();
        try
        {
            using var current = Process.GetCurrentProcess();
            string executable = current.MainModule.FileName;
            string command = Path.GetFileNameWithoutExtension(executable).Equals(
                "dotnet",
                StringComparison.OrdinalIgnoreCase
            )
                ? $"\"{Assembly.GetExecutingAssembly().Location}\" "
                : "";
            var start = new ProcessStartInfo(executable, $"{command}{mode} \"{log}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = redirect,
                RedirectStandardError = redirect,
            };

            using var child = Process.Start(start);
            if (!child.WaitForExit(10_000))
            {
                child.Kill();
                throw new TimeoutException(mode);
            }

            string fileLog = File.ReadAllText(log);
            if (child.ExitCode != 0 || !fileLog.Contains("startup completed"))
                throw new InvalidOperationException(
                    $"{mode} failed. File log:{Environment.NewLine}{fileLog}"
                );

            if (redirect)
            {
                CheckOutput(child.StandardOutput.ReadToEnd(), "stdout", mode != "invalid-stdout");
                CheckOutput(child.StandardError.ReadToEnd(), "stderr");
            }

            Console.WriteLine($"{mode} passed");
        }
        finally
        {
            File.Delete(log);
        }
    }

    private static void CheckOutput(string output, string stream, bool afterDetach = true)
    {
        string expected = $"{stream} before detach";
        if (afterDetach)
            expected += $"{Environment.NewLine}{stream} after detach";
        if (output.TrimEnd('\r', '\n') != expected)
            throw new InvalidOperationException($"Unexpected {stream} output: {output}");
    }
}

// Daemon only needs the file logging API. Keep this test independent of the DS.
namespace Pulsar.Shared
{
    internal static class LogFile
    {
        internal static string Path;

        internal static void WriteLine(string message) =>
            File.AppendAllText(Path, message + Environment.NewLine);

        internal static void Warn(string message) => WriteLine(message);

#if NETCOREAPP
        internal static void Error(string message) => WriteLine(message);

        internal static void Dispose() { }
#endif
    }
}
