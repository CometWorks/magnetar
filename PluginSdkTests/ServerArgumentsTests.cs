using System;
using Magnetar.Legacy.Arguments;
using Xunit;

namespace PluginSdkTests;

public class ServerArgumentsTests
{
    [Fact]
    public void ServerValuesAreParsedWhileLoaderAndGameArgumentsStaySeparate()
    {
        string[] original = ["--PREPAREMANAGED", "/export with spaces", "/config", "/bare", "-ds64", "/game",
            "-profile", "/debug", "-path", "/daemon", "-port", "28000", "-session:/world", "--safeMode", "-consent", "deny"];
        string[] copy = (string[])original.Clone();
        var parsed = ServerArguments.Parse(original);
        Assert.Equal("/export with spaces", parsed.PreparationDirectory);
        Assert.Equal("/bare", parsed.ConfigDirectory);
        Assert.Equal("/game", parsed.DedicatedServerDirectory);
        Assert.False(parsed.Daemon);
        Assert.Equal(ConsentChoice.Deny, parsed.Consent);
        Assert.Equal(new[] { "--safeMode", "-profile=/debug", "-noSplash", "-noPrompt", "-lazySteam" }, parsed.PulsarArguments);
        Assert.Equal(copy, original);
    }

    [Theory]
    [InlineData("-prepareManaged")]
    [InlineData("-config")]
    [InlineData("-ds64")]
    [InlineData("-profile")]
    public void MissingValuesFailBeforeServerStartup(string option)
    {
        Assert.Throws<ArgumentException>(() => ServerArguments.Parse([option]));
        Assert.Throws<ArgumentException>(() => ServerArguments.Parse([option, "-debug"]));
        Assert.Throws<ArgumentException>(() => ServerArguments.Parse([option + "="]));
    }

    [Theory]
    [InlineData("accept", ConsentChoice.Accept)]
    [InlineData("DENY", ConsentChoice.Deny)]
    [InlineData("withdraw", ConsentChoice.Withdraw)]
    public void ConsentUsesValidatedValues(string value, ConsentChoice expected) =>
        Assert.Equal(expected, ServerArguments.Parse(["-consent", value]).Consent);

    [Fact]
    public void InvalidConsentAndRetiredSecretsNeverReachSharedArguments()
    {
        Assert.Throws<ArgumentException>(() => ServerArguments.Parse(["-consent", "invalid"]));
        var parsed = ServerArguments.Parse(["-github-token", "secret", "-consent", "-noconsent"]);
        Assert.Equal(ConsentChoice.Deny, parsed.Consent);
        Assert.DoesNotContain("secret", string.Join(" ", parsed.PulsarArguments));
        Assert.DoesNotContain("secret", string.Join(" ", parsed.Warnings));
        var error = Assert.Throws<ArgumentException>(() => ServerArguments.Parse(["-github-token", "first-secret", "-github-token", "second-secret"]));
        Assert.DoesNotContain("secret", error.Message);
    }

    [Theory]
    [InlineData("--HELP", true, false)]
    [InlineData("/h", true, false)]
    [InlineData("-?", true, false)]
    [InlineData("/VERSION", false, true)]
    [InlineData("-v", false, true)]
    public void HelpAndVersionAliasesAreLibraryParsed(string option, bool help, bool version)
    {
        var parsed = ServerArguments.Parse([option]);
        Assert.Equal(help, parsed.Help);
        Assert.Equal(version, parsed.Version);
    }

    [Fact]
    public void InlineValuesRemainLiteralAndDuplicateValuesAreRejected()
    {
        var parsed = ServerArguments.Parse(["--prepareManaged=/output", "-profile=/folder/a=b.xml", "-daemon", "-noimplicitmod"]);
        Assert.Equal("/output", parsed.PreparationDirectory);
        Assert.Contains("-profile=/folder/a=b.xml", parsed.PulsarArguments);
        Assert.True(parsed.Daemon);
        Assert.True(parsed.NoImplicitMod);
        Assert.Throws<ArgumentException>(() => ServerArguments.Parse(["-prepareManaged", "/one", "-prepareManaged", "/two"]));
    }
}
