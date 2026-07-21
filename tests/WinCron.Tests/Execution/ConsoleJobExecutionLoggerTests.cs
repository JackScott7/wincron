using WinCron.Execution;

namespace WinCron.Tests.Execution;

public sealed class ConsoleJobExecutionLoggerTests
{
    [Fact]
    public async Task WriteAsyncPrintsMetadataStandardOutputAndStandardError()
    {
        using var standardOutput = new StringWriter();
        using var standardError = new StringWriter();
        using var logger = new ConsoleJobExecutionLogger(
            standardOutput,
            standardError,
            TimeZoneInfo.Utc);
        var startedAt = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);
        var result = new JobExecutionResult(
            "line-1",
            "echo test",
            startedAt,
            startedAt,
            startedAt.AddMilliseconds(25),
            0,
            "visible output",
            "visible error",
            false,
            null);

        await logger.WriteAsync(result, TestContext.Current.CancellationToken);

        Assert.Contains("line-1 exited with code 0 in 25 ms: echo test", standardOutput.ToString());
        Assert.Contains("visible output", standardOutput.ToString());
        Assert.Contains("visible error", standardError.ToString());
    }

    [Fact]
    public async Task CompositeLoggerWritesResultToEveryConfiguredLogger()
    {
        var firstLogger = new RecordingLogger();
        var secondLogger = new RecordingLogger();
        var compositeLogger = new CompositeJobExecutionLogger(firstLogger, secondLogger);
        var timestamp = DateTimeOffset.UtcNow;
        var result = new JobExecutionResult(
            "line-1",
            "echo test",
            timestamp,
            timestamp,
            timestamp,
            0,
            string.Empty,
            string.Empty,
            false,
            null);

        await compositeLogger.WriteAsync(result, TestContext.Current.CancellationToken);

        Assert.Same(result, Assert.Single(firstLogger.Results));
        Assert.Same(result, Assert.Single(secondLogger.Results));
    }

    private sealed class RecordingLogger : IJobExecutionLogger
    {
        public List<JobExecutionResult> Results { get; } = [];

        public Task WriteAsync(JobExecutionResult result, CancellationToken cancellationToken = default)
        {
            Results.Add(result);
            return Task.CompletedTask;
        }
    }
}
