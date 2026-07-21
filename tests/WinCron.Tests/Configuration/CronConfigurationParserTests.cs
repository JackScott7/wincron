using WinCron.Configuration;

namespace WinCron.Tests.Configuration;

public sealed class CronConfigurationParserTests
{
    private readonly CronConfigurationParser parser = new(@"C:\default");

    [Fact]
    public void Parse_CommentsBlankLinesNamesRangesAndSteps_CreatesJob()
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
    public void Parse_EnvironmentAssignments_AppliesSnapshotsOnlyToSubsequentJobs()
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
    public void Parse_MultipleInvalidLines_ReturnsEveryErrorWithLineNumbers()
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
    public void Parse_CommandContainingEquals_IsNotMistakenForEnvironmentAssignment()
    {
        var result = parser.Parse("* * * * * cmd.exe /c set VALUE=42");

        Assert.True(result.IsValid);
        Assert.Equal("cmd.exe /c set VALUE=42", Assert.Single(result.Configuration.Jobs).CommandText);
    }
}
