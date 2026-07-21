using WinCron.Domain;

namespace WinCron.Scheduling;

public interface IJobDispatcher
{
    Task DispatchAsync(
        CronJobDefinition job,
        DateTimeOffset scheduledOccurrenceUtc,
        CancellationToken cancellationToken);
}
