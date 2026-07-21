namespace WinCron.Execution;

public sealed class NullJobOutputObserver : IJobOutputObserver
{
    public static NullJobOutputObserver Instance { get; } = new();

    private NullJobOutputObserver()
    {
    }

    public Task WriteAsync(
        string jobId,
        JobOutputChannel channel,
        ReadOnlyMemory<char> output,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
