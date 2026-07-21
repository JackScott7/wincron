namespace WinCron.Execution;

public sealed class CompositeJobExecutionLogger : IJobExecutionLogger
{
    private readonly IReadOnlyList<IJobExecutionLogger> loggers;

    public CompositeJobExecutionLogger(params IJobExecutionLogger[] loggers)
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
    }

    public Task WriteAsync(JobExecutionResult result, CancellationToken cancellationToken = default) =>
        Task.WhenAll(loggers.Select(logger => logger.WriteAsync(result, cancellationToken)));
}
