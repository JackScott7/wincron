namespace WinCron.Scheduling;

public interface ISchedulerDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
