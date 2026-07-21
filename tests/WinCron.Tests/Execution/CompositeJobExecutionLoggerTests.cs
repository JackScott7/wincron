using WinCron.Execution;

namespace WinCron.Tests.Execution;

public sealed class CompositeJobExecutionLoggerTests
{
    [Fact]
    public async Task WriteAsyncIsolatesFailedSinkAndReportsFailure()
    {
        var successfulLogger = new RecordingLogger();
        var reporter = new RecordingFailureReporter();
        var compositeLogger = new CompositeJobExecutionLogger(
            reporter,
            new ThrowingLogger(),
            successfulLogger);
        var timestamp = DateTimeOffset.UtcNow;
        var result = new JobExecutionResult(
            "line-1", "echo", timestamp, timestamp, timestamp, 0, "", "", false, null);

        await compositeLogger.WriteAsync(result, TestContext.Current.CancellationToken);

        Assert.Same(result, successfulLogger.Result);
        Assert.IsType<IOException>(reporter.Exception);
    }

    private sealed class ThrowingLogger : IJobExecutionLogger
    {
        public Task WriteAsync(JobExecutionResult result, CancellationToken cancellationToken = default) =>
            throw new IOException("unavailable");
    }

    private sealed class RecordingLogger : IJobExecutionLogger
    {
        public JobExecutionResult? Result { get; private set; }

        public Task WriteAsync(JobExecutionResult result, CancellationToken cancellationToken = default)
        {
            Result = result;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingFailureReporter : IExecutionLogFailureReporter
    {
        public Exception? Exception { get; private set; }

        public Task ReportAsync(Exception exception, CancellationToken cancellationToken)
        {
            Exception = exception;
            return Task.CompletedTask;
        }
    }
}
