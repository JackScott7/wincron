using WinCron.Domain;
using WinCron.Execution;

namespace WinCron.Tests.Execution;

public sealed class JobExecutorIntegrationTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"wincron-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task DispatchAsyncExecutesThroughCommandShellAndCapturesCompleteResult()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var logger = new RecordingExecutionLogger();
        var executor = new JobExecutor(logger);
        var job = CreateJob(
            "echo %WINCRON_TEST_VALUE% & echo error-text 1>&2 & exit /b 7",
            new Dictionary<string, string> { ["WINCRON_TEST_VALUE"] = "environment-text" });
        var scheduledAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        await executor.DispatchAsync(job, scheduledAt, CancellationToken.None);

        var result = Assert.Single(logger.Results);
        Assert.Equal(7, result.ExitCode);
        Assert.Contains("environment-text", result.StandardOutput);
        Assert.Contains("error-text", result.StandardError);
        Assert.Equal(scheduledAt, result.ScheduledOccurrenceUtc);
        Assert.False(result.Succeeded);
        Assert.False(result.WasCanceled);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task DispatchAsyncUsesConfiguredWorkingDirectory()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var logger = new RecordingExecutionLogger();
        var executor = new JobExecutor(logger);
        var job = CreateJob("cd");

        await executor.DispatchAsync(job, DateTimeOffset.UtcNow, CancellationToken.None);

        var result = Assert.Single(logger.Results);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            Path.GetFullPath(temporaryDirectory).TrimEnd(Path.DirectorySeparatorChar),
            result.StandardOutput.Trim().TrimEnd(Path.DirectorySeparatorChar),
            ignoreCase: true);
    }

    [Fact]
    public async Task DispatchAsyncLogsProcessErrorWhenShellCannotStart()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var logger = new RecordingExecutionLogger();
        var commandBuilder = new WindowsShellCommandBuilder(Path.Combine(temporaryDirectory, "missing-shell.exe"));
        var executor = new JobExecutor(logger, commandBuilder);

        await executor.DispatchAsync(CreateJob("echo unreachable"), DateTimeOffset.UtcNow, CancellationToken.None);

        var result = Assert.Single(logger.Results);
        Assert.Null(result.ExitCode);
        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    [Fact]
    public async Task DispatchAsyncLimitsCapturedOutputWhileStreamingCompleteOutput()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var logger = new RecordingExecutionLogger();
        var observer = new RecordingOutputObserver();
        var executor = new JobExecutor(logger, outputObserver: observer);
        var job = CreateJob("echo 123456789", maximumOutputCharacters: 5);

        await executor.DispatchAsync(job, DateTimeOffset.UtcNow, CancellationToken.None);

        var result = Assert.Single(logger.Results);
        Assert.Equal("12345", result.StandardOutput);
        Assert.True(result.StandardOutputTruncated);
        Assert.Contains("123456789", observer.Output.ToString());
    }

    [Fact]
    public async Task DispatchAsyncTerminatesAndRecordsTimedOutCommand()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var logger = new RecordingExecutionLogger();
        var executor = new JobExecutor(logger);
        var job = CreateJob("ping.exe 127.0.0.1 -t", timeout: TimeSpan.FromMilliseconds(100));

        await executor.DispatchAsync(job, DateTimeOffset.UtcNow, CancellationToken.None);

        var result = Assert.Single(logger.Results);
        Assert.True(result.TimedOut);
        Assert.False(result.Succeeded);
        Assert.False(result.WasCanceled);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private CronJobDefinition CreateJob(
        string command,
        IReadOnlyDictionary<string, string>? environment = null,
        TimeSpan? timeout = null,
        int maximumOutputCharacters = 1_048_576)
    {
        var expression = new CronExpression(
            Parse(CronFieldKind.Minute),
            Parse(CronFieldKind.Hour),
            Parse(CronFieldKind.DayOfMonth),
            Parse(CronFieldKind.Month),
            Parse(CronFieldKind.DayOfWeek));
        return new CronJobDefinition(
            expression,
            command,
            environment ?? new Dictionary<string, string>(),
            temporaryDirectory,
            1,
            new JobExecutionOptions
            {
                Timeout = timeout ?? TimeSpan.FromHours(1),
                MaximumCapturedCharactersPerStream = maximumOutputCharacters
            });
    }

    private static CronField Parse(CronFieldKind kind)
    {
        Assert.True(CronField.TryParse(kind, "*", out var field, out var error), error);
        return field!;
    }

    private sealed class RecordingExecutionLogger : IJobExecutionLogger
    {
        public List<JobExecutionResult> Results { get; } = [];

        public Task WriteAsync(JobExecutionResult result, CancellationToken cancellationToken = default)
        {
            Results.Add(result);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingOutputObserver : IJobOutputObserver
    {
        public System.Text.StringBuilder Output { get; } = new();

        public Task WriteAsync(
            string jobId,
            JobOutputChannel channel,
            ReadOnlyMemory<char> output,
            CancellationToken cancellationToken)
        {
            Output.Append(output.Span);
            return Task.CompletedTask;
        }
    }
}
