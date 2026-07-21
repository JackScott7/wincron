namespace WinCron.Scheduling;

public sealed class CronSchedulerOptions
{
    public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Local;

    public TimeSpan MisfireGracePeriod { get; init; } = TimeSpan.FromMinutes(1);

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(TimeZone);

        if (MisfireGracePeriod < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MisfireGracePeriod),
                "The misfire grace period cannot be negative.");
        }
    }
}
