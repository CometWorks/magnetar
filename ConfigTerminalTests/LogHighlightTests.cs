using Magnetar.ConfigTerminal.Logs;
using Xunit;

namespace Magnetar.ConfigTerminal.Tests;

public class LogHighlightTests
{
    // Expected is passed as the enum name so this public test method does not expose
    // the internal LogHighlightKind in its signature (CS0051).
    [Theory]
    [InlineData("2026-07-14 12:00:00.000 - Thread:  1 -> Game ready...", "Ready")]
    [InlineData("Game ready", "Ready")]
    [InlineData("System.NullReferenceException: Object reference not set", "Exception")]
    [InlineData("  at Foo.Bar() threw an AggregateException", "Exception")]
    [InlineData("Loading world 'Red Ship'", "None")]
    [InlineData("", "None")]
    [InlineData(null, "None")]
    public void Classify_matches_markers(string line, string expected) =>
        Assert.Equal(expected, LogHighlight.Classify(line).ToString());

    [Fact]
    public void Exception_wins_over_ready_when_both_present() =>
        // A fault is the more urgent thing to surface.
        Assert.Equal("Exception",
            LogHighlight.Classify("Game ready... but then Exception was thrown").ToString());

    [Fact]
    public void Matching_is_case_sensitive_to_avoid_prose_false_positives()
    {
        // Lowercase prose must not trip the markers — only the DS's capitalised output.
        Assert.Equal("None", LogHighlight.Classify("no exception to the rule").ToString());
        Assert.Equal("None", LogHighlight.Classify("the game ready check").ToString());
    }
}
