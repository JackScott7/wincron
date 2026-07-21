namespace WinCron.Execution;

public sealed class CompositeJobExecutionLogger : IJobExecutionLogger
{
    private readonly IReadOnlyList<IJobExecutionLogger> loggers;
    private readonly IExecutionLogFailureReporter? failureReporter;

    public CompositeJobExecutionLogger(params IJobExecutionLogger[] loggers)
        : this(null, loggers)
    {
    }

    public CompositeJobExecutionLogger(
        IExecutionLogFailureReporter? failureReporter,
        params IJobExecutionLogger[] loggers)
    {
        ArgumentNullException.ThrowIfNull(loggers);

        if (loggers.Length == 0)
        {
            throw new ArgumentException("At least one execution logger is required.", nameof(loggers));
        }

        if (loggers.Any(logger => logger is null))
        {
            throw new ArgumentException("Execution loggers cannot contain null values.", nameof(loggers));
        }

        this.loggers = Array.AsReadOnly(loggers);
        this.failureReporter = failureReporter;
    }

    public Task WriteAsync(JobExecutionResult result, CancellationToken cancellationToken = default) =>
        Task.WhenAll(loggers.Select(logger => WriteIsolatedAsync(logger, result, cancellationToken)));

    private async Task WriteIsolatedAsync(
        IJobExecutionLogger logger,
        JobExecutionResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            await logger.WriteAsync(result, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (failureReporter is not null)
            {
                await failureReporter.ReportAsync(exception, cancellationToken);
            }
        }
    }
}
