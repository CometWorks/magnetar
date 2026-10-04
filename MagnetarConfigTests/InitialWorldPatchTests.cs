using System;
using System.IO;
using Xunit;

namespace MagnetarConfigTests;

public class InitialWorldPatchTests
{
    [Fact]
    public void ClientModScriptHookIsInstalledInEarlyPatchPhase()
    {
        string root = FindRepositoryRoot();
        string source = File.ReadAllText(
            Path.Combine(root, "Legacy", "Patch", "Patch_MyScriptManager.cs")
        );

        Assert.Contains("[HarmonyPatchCategory(\"Early\")]", source);
        Assert.DoesNotContain("[HarmonyPatchCategory(\"Late\")]", source);
        Assert.Contains("Patch_MyDefinitionErrors.RedirectModLogging(false);", source);
        Assert.Contains("conditionalSymbols.Remove(ConditionalSymbol);", source);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new(AppContext.BaseDirectory);
        while (true)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Magnetar.slnx")))
                return directory.FullName;
            if (directory.Parent is null)
                break;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Magnetar repository root.");
    }
}
