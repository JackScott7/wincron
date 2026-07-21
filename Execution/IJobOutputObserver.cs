namespace WinCron.Execution;

public interface IJobOutputObserver
{
    Task WriteAsync(
        string jobId,
        JobOutputChannel channel,
        ReadOnlyMemory<char> output,
        CancellationToken cancellationToken);
}
