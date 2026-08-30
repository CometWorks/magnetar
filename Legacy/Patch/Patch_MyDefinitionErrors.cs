using System.IO;
using HarmonyLib;
using Pulsar.Shared;
using VRage.FileSystem;
using VRage.Game;

namespace Pulsar.Legacy.Patch;

[HarmonyPatchCategory("Early")]
[HarmonyPatch(typeof(MyDefinitionErrors), "Add")]
public static class Patch_MyDefinitionErrors
{
    public static bool PulsarLog = false;

    public static bool Prefix(MyModContext context, string message)
    {
        if (!PulsarLog || !message.Contains("Compilation"))
            return true;

        // SE formats the prefix as "Compilation of <ModsPath><sep><ModId>_"
        // where <sep> is Path.DirectorySeparatorChar (backslash on Windows,
        // forward slash on Linux).
        string prefix =
            $"Compilation of {MyFileSystem.ModsPath}{Path.DirectorySeparatorChar}{context.ModId}_";
        string[] trim = [prefix, " failed:"];
        string name = Tools.RemoveAll(message, trim);

        LogFile.Error($"Failed to build {name}:");
        foreach (string diagnostic in Patch_Compile.Diagnostics)
            LogFile.Error(diagnostic);

        return false;
    }

    public static void RedirectModLogging(bool enabled)
    {
        PulsarLog = enabled;
        Patch_Compile.PulsarLog = enabled;
    }
}
