namespace WinCron.Domain;

public sealed record JobExecutionOptions
{
    public static JobExecutionOptions Default { get; } = new();

    public JobOverlapPolicy OverlapPolicy { get; init; } = JobOverlapPolicy.Skip;

    public TimeSpan Timeout { get; init; } = TimeSpan.FromHours(1);

    public int MaximumCapturedCharactersPerStream { get; init; } = 1_048_576;

    public void Validate()
    {
        if (Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout), "The job timeout must be positive.");
        }

        if (MaximumCapturedCharactersPerStream <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaximumCapturedCharactersPerStream),
                "The maximum captured output must be positive.");
        }
    }
}
