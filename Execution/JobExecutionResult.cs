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
    string? ErrorMessage)
{
    public TimeSpan Duration => CompletedAtUtc - StartedAtUtc;

    public bool Succeeded => ExitCode == 0 && !WasCanceled && ErrorMessage is null;
}
