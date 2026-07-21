using System.Text.Json;

namespace WinCron.Execution;

public sealed class JsonFileJobExecutionLogger : IJobExecutionLogger, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private readonly JsonFileJobExecutionLoggerOptions options;
    private bool isDisposed;

    public JsonFileJobExecutionLogger(
        string? logDirectory = null,
        JsonFileJobExecutionLoggerOptions? options = null)
    {
        this.options = options ?? new JsonFileJobExecutionLoggerOptions();
        this.options.Validate();
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
            RotateIfRequired(System.Text.Encoding.UTF8.GetByteCount(serializedResult));
            await File.AppendAllTextAsync(LogFilePath, serializedResult, cancellationToken);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private void RotateIfRequired(int pendingBytes)
    {
        if (!File.Exists(LogFilePath)
            || new FileInfo(LogFilePath).Length + pendingBytes <= options.MaximumFileSizeBytes)
        {
            return;
        }

        for (var index = options.RetainedFileCount - 1; index >= 1; index--)
        {
            var sourcePath = GetRotatedLogPath(index);
            var destinationPath = GetRotatedLogPath(index + 1);
            if (File.Exists(sourcePath))
            {
                File.Move(sourcePath, destinationPath, overwrite: true);
            }
        }

        File.Move(LogFilePath, GetRotatedLogPath(1), overwrite: true);
    }

    private string GetRotatedLogPath(int index) => Path.Combine(LogDirectory, $"runs.{index}.jsonl");

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
