using WinCron.Configuration;

namespace WinCron.Tests.Configuration;

public sealed class CronConfigurationParserTests
{
    private readonly CronConfigurationParser parser = new(@"C:\default");

    [Fact]
    public void ParseCreatesJobFromCommentsBlankLinesNamesRangesAndSteps()
    {
        const string configuration = """
            # weekdays at intervals

            */5 9-17/2 * JAN,MAR MON-FRI echo ready
            """;

        var result = parser.Parse(configuration);

        Assert.True(result.IsValid);
        var job = Assert.Single(result.Configuration.Jobs);
        Assert.Equal("echo ready", job.CommandText);
        Assert.Equal(3, job.SourceLineNumber);
    }

    [Fact]
    public void ParseAppliesEnvironmentSnapshotsOnlyToSubsequentJobs()
    {
        const string configuration = """
            MODE=first
            * * * * * echo first
            MODE=second
            WINCRON_WORKING_DIRECTORY=C:\jobs
            * * * * * echo second  with  spacing
            """;

        var result = parser.Parse(configuration);

        Assert.True(result.IsValid);
        Assert.Collection(
            result.Configuration.Jobs,
            firstJob =>
            {
                Assert.Equal("first", firstJob.EnvironmentVariables["MODE"]);
                Assert.Equal(@"C:\default", firstJob.WorkingDirectory);
            },
            secondJob =>
            {
                Assert.Equal("second", secondJob.EnvironmentVariables["MODE"]);
                Assert.Equal(@"C:\jobs", secondJob.WorkingDirectory);
                Assert.Equal("echo second  with  spacing", secondJob.CommandText);
            });
    }

    [Fact]
    public void ParseReturnsEveryErrorWithLineNumbersForMultipleInvalidLines()
    {
        const string configuration = """
            * * *
            60 * * * * echo invalid-minute
            * * * * * echo valid
            """;

        var result = parser.Parse(configuration);

        Assert.False(result.IsValid);
        Assert.Equal([1, 2], result.Errors.Select(error => error.LineNumber));
        Assert.Single(result.Configuration.Jobs);
    }

    [Fact]
    public void ParseDoesNotMistakeCommandContainingEqualsForEnvironmentAssignment()
    {
        var result = parser.Parse("* * * * * cmd.exe /c set VALUE=42");

        Assert.True(result.IsValid);
        Assert.Equal("cmd.exe /c set VALUE=42", Assert.Single(result.Configuration.Jobs).CommandText);
    }

    [Fact]
    public void ParseRejectsScheduleThatCanNeverProduceAnOccurrence()
    {
        var result = parser.Parse("0 0 31 FEB * echo impossible");

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(1, error.LineNumber);
        Assert.Contains("never produce", error.Message);
        Assert.Empty(result.Configuration.Jobs);
    }

    [Fact]
    public void ParseAcceptsRestrictedWeekdayThatMakesOtherwiseImpossibleDateValid()
    {
        var result = parser.Parse("0 0 31 FEB MON echo monday");

        Assert.True(result.IsValid);
        Assert.Single(result.Configuration.Jobs);
    }

    [Fact]
    public void ParseCreatesScopedExecutionOptions()
    {
        const string configuration = """
            WINCRON_OVERLAP_POLICY=QueueOne
            WINCRON_TIMEOUT_SECONDS=30.5
            WINCRON_MAX_OUTPUT_CHARACTERS=2048
            * * * * * echo controlled
            """;

        var result = parser.Parse(configuration);

        Assert.True(result.IsValid);
        var options = Assert.Single(result.Configuration.Jobs).ExecutionOptions;
        Assert.Equal(WinCron.Domain.JobOverlapPolicy.QueueOne, options.OverlapPolicy);
        Assert.Equal(TimeSpan.FromSeconds(30.5), options.Timeout);
        Assert.Equal(2048, options.MaximumCapturedCharactersPerStream);
    }

    [Theory]
    [InlineData("WINCRON_OVERLAP_POLICY=invalid")]
    [InlineData("WINCRON_TIMEOUT_SECONDS=0")]
    [InlineData("WINCRON_MAX_OUTPUT_CHARACTERS=-1")]
    public void ParseRejectsInvalidExecutionOption(string assignment)
    {
        var result = parser.Parse($"{assignment}\n* * * * * echo invalid");

        Assert.False(result.IsValid);
        Assert.Empty(result.Configuration.Jobs);
        Assert.Equal(2, Assert.Single(result.Errors).LineNumber);
    }
}
