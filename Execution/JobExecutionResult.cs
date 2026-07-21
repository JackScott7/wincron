namespace WinCron.Execution;

public sealed record JobExecutionResult(
    string JobId,
    string CommandText,
    DateTimeOffset ScheduledOccurrenceUtc,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool WasCanceled,
    string? ErrorMessage,
    bool TimedOut = false,
    bool StandardOutputTruncated = false,
    bool StandardErrorTruncated = false)
{
    public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;

    public bool Succeeded => ExitCode == 0 && !WasCanceled && !TimedOut && ErrorMessage is null;
}
