using System;
using System.Collections.Generic;
using System.IO;
using Magnetar.ConfigTerminal.Io;
using Magnetar.ConfigTerminal.Model;

namespace Magnetar.ConfigTerminal;

/// <summary>Parsed command-line options (see Docs/ConfigTerminal.md §10).</summary>
internal sealed class Cli
{
    public string DataDir;         // -path
    public string ConfigDir;       // -config
    public string MagnetarExe;     // -magnetar
    public string Ds64Dir;         // -ds64
    public bool NetDriver;         // -netdriver
    public bool Diag;              // -diag (headless read-only report, no UI)
    public bool Help;
    public string Error;

    public static Cli Parse(string[] args)
    {
        var cli = new Cli();
        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            switch (a.ToLowerInvariant())
            {
                case "-path": cli.DataDir = Next(args, ref i); break;
                case "-config": cli.ConfigDir = Next(args, ref i); break;
                case "-magnetar": cli.MagnetarExe = Next(args, ref i); break;
                case "-ds64": cli.Ds64Dir = Next(args, ref i); break;
                case "-netdriver": cli.NetDriver = true; break;
                case "-diag": cli.Diag = true; break;
                case "-h":
                case "-help":
                case "--help": cli.Help = true; break;
                default:
                    cli.Error = $"Unknown argument: {a}";
                    return cli;
            }
        }

        // Explicit directories must exist — never silently fall back past a value
        // the user gave (matching Magnetar's own refusal).
        if (cli.DataDir != null && !Directory.Exists(cli.DataDir))
            cli.Error = $"-path directory does not exist: {cli.DataDir}";
        if (cli.ConfigDir != null && !Directory.Exists(cli.ConfigDir))
            cli.Error = $"-config directory does not exist: {cli.ConfigDir}";

        return cli;
    }

    private static string Next(string[] args, ref int i)
    {
        if (i + 1 >= args.Length)
            return null;
        return args[++i];
    }

    /// <summary>True when the pair was fully specified and the picker can be skipped.</summary>
    public bool HasInstance => DataDir != null || ConfigDir != null;

    public InstanceBinding ToBinding()
    {
        var binding = new InstanceBinding
        {
            DataDir = DataDir,
            MagnetarConfigDir = ConfigDir,
            MagnetarExePath = MagnetarExe,
            Ds64Dir = Ds64Dir,
        };
        return InstanceLocator.ResolveDefaults(binding);
    }

    public static void PrintHelp()
    {
        Console.WriteLine(@"MagnetarConfig — configure and operate a Magnetar-managed SE1 Dedicated Server.

Usage: MagnetarConfig [options]

  -path <dir>      DS data directory (SpaceEngineers-Dedicated.cfg + Saves).
  -config <dir>    Magnetar config directory (config.xml, logs, magnetar.pid).
  -magnetar <file> Magnetar launcher executable to start/stop.
  -ds64 <dir>      DedicatedServer64 folder (for world templates).
  -netdriver       Force Terminal.Gui's NetDriver (portable fallback).
  -help, -h        Show this help.

With no -path/-config the tool opens an interactive instance picker.");
    }
}
