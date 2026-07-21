namespace WinCron.Execution;

public sealed class TextWriterJobOutputObserver(
    TextWriter standardOutput,
    TextWriter standardError) : IJobOutputObserver, IDisposable
{
    private readonly TextWriter standardOutput = standardOutput ?? throw new ArgumentNullException(nameof(standardOutput));
    private readonly TextWriter standardError = standardError ?? throw new ArgumentNullException(nameof(standardError));
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public async Task WriteAsync(
        string jobId,
        JobOutputChannel channel,
        ReadOnlyMemory<char> output,
        CancellationToken cancellationToken)
    {
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            var writer = channel == JobOutputChannel.StandardOutput ? standardOutput : standardError;
            await writer.WriteAsync(output, cancellationToken);
            await writer.FlushAsync(cancellationToken);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public void Dispose() => writeLock.Dispose();
}
