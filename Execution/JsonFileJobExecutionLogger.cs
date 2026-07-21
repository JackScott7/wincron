using System.Text.Json;

namespace WinCron.Execution;

public sealed class JsonFileJobExecutionLogger : IJobExecutionLogger, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private bool isDisposed;

    public JsonFileJobExecutionLogger(string? logDirectory = null)
    {
        LogDirectory = logDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "wincron",
            "output");
        LogFilePath = Path.Combine(LogDirectory, "runs.jsonl");
    }

    public string LogDirectory { get; }

    public string LogFilePath { get; }

    public async Task WriteAsync(JobExecutionResult result, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentNullException.ThrowIfNull(result);

        Directory.CreateDirectory(LogDirectory);
        var serializedResult = JsonSerializer.Serialize(result, SerializerOptions) + Environment.NewLine;

        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(LogFilePath, serializedResult, cancellationToken);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        writeLock.Dispose();
        isDisposed = true;
    }
}
