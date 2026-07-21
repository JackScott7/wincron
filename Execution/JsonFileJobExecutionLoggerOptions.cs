namespace WinCron.Execution;

public sealed record JsonFileJobExecutionLoggerOptions
{
    public long MaximumFileSizeBytes { get; init; } = 10 * 1024 * 1024;

    public int RetainedFileCount { get; init; } = 5;

    public void Validate()
    {
        if (MaximumFileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumFileSizeBytes),
                "The maximum log file size must be positive.");
        }

        if (RetainedFileCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetainedFileCount),
                "At least one log file must be retained.");
        }
    }
}
