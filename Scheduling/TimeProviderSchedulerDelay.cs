namespace WinCron.Scheduling;

public sealed class TimeProviderSchedulerDelay(TimeProvider timeProvider) : ISchedulerDelay
{
    private readonly TimeProvider timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, timeProvider, cancellationToken);
}
