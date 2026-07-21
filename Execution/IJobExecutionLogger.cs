namespace WinCron.Execution;

public interface IJobExecutionLogger
{
    Task WriteAsync(JobExecutionResult result, CancellationToken cancellationToken = default);
}
