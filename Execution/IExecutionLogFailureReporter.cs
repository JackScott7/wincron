namespace WinCron.Execution;

public interface IExecutionLogFailureReporter
{
    Task ReportAsync(Exception exception, CancellationToken cancellationToken);
}
