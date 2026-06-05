using System;
using System.Collections.Generic;
using System.Linq;

namespace Pulsar.Shared;

public enum UpdateType
{
    None,
    Standard,
    Tester,
}

public static class Flags
{
    public static UpdateType UpdateType { get; private set; }
    public static bool ExternalDebug { get; private set; }
    public static bool DebugMenu { get; private set; }
    public static bool CustomSources { get; private set; }
    public static bool ContinueGame { get; private set; }
    public static bool CheckAllPlugins { get; private set; }
    public static bool GameIntroVideo { get; private set; }
    public static bool MakeCheckFile { get; private set; }
    public static bool TrustedMods { get; private set; }
    public static bool Daemon { get; private set; }

    static Flags()
    {
        if (HasArg("noupdate"))
            UpdateType = UpdateType.None;
        else if (HasArg("prerelease"))
            UpdateType = UpdateType.Tester;
        else
            UpdateType = UpdateType.Standard;

        ExternalDebug = HasArg("debug");
        DebugMenu = HasArg("f12menu");
        CustomSources = HasArg("sources");
        ContinueGame = HasArg("continue");
        CheckAllPlugins = HasArg("debugCompileAll");
        GameIntroVideo = HasArg("keepintro");
        MakeCheckFile = HasArg("mkcheck");
        TrustedMods = HasArg("hardened");
        Daemon = HasArg("daemon");
    }

    public static void LogFlags()
    {
        List<string> changed = [];

        if (UpdateType == UpdateType.None)
            changed.Add("NoUpdates");
        else if (UpdateType == UpdateType.Tester)
            changed.Add("EarlyUpdates");

        if (ExternalDebug)
            changed.Add("ExternalDebug");
        if (DebugMenu)
            changed.Add("DebugMenu");
        if (CustomSources)
            changed.Add("CustomSources");
        if (ContinueGame)
            changed.Add("ContinueGame");
        if (CheckAllPlugins)
            changed.Add("CheckAllPlugins");
        if (GameIntroVideo)
            changed.Add("GameIntroVideo");
        if (MakeCheckFile)
            changed.Add("MakeCheckFile");
        if (TrustedMods)
            changed.Add("TrustedMods");
        if (Daemon)
            changed.Add("Daemon");

        if (changed.Count > 0)
            LogFile.WriteLine($"Enabled flags: {string.Join(" ", changed)}");
    }

    private static bool HasArg(string argument) =>
        Environment
            .GetCommandLineArgs()
            .Any(arg => arg.Equals($"-{argument}", StringComparison.OrdinalIgnoreCase));
}
