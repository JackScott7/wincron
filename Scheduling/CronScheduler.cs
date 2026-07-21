using System.Collections.Concurrent;
using WinCron.Domain;

namespace WinCron.Scheduling;

public sealed class CronScheduler
{
    private readonly IReadOnlyList<CronJobDefinition> jobs;
    private readonly IJobDispatcher jobDispatcher;
    private readonly TimeProvider timeProvider;
    private readonly ISchedulerDelay schedulerDelay;
    private readonly CronSchedulerOptions options;
    private readonly ConcurrentDictionary<long, Task> activeDispatches = new();
    private readonly Dictionary<string, DateTimeOffset> lastDispatchedOccurrences = [];
    private long dispatchSequence;

    public CronScheduler(
        IReadOnlyList<CronJobDefinition> jobs,
        IJobDispatcher jobDispatcher,
        CronSchedulerOptions? options = null,
        TimeProvider? timeProvider = null,
        ISchedulerDelay? schedulerDelay = null)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(jobDispatcher);

        this.jobs = jobs;
        this.jobDispatcher = jobDispatcher;
        this.options = options ?? new CronSchedulerOptions();
        this.options.Validate();
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.schedulerDelay = schedulerDelay ?? new TimeProviderSchedulerDelay(this.timeProvider);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var scheduleQueue = CreateInitialSchedule(timeProvider.GetUtcNow());

        try
        {
            while (!cancellationToken.IsCancellationRequested && scheduleQueue.Count > 0)
            {
                scheduleQueue.TryPeek(out _, out var nextOccurrenceUtc);
                var delay = nextOccurrenceUtc - timeProvider.GetUtcNow();

                if (delay > TimeSpan.Zero)
                {
                    await schedulerDelay.DelayAsync(delay, cancellationToken);
                }

                DispatchDueJobs(scheduleQueue, timeProvider.GetUtcNow(), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        finally
        {
            await AwaitActiveDispatchesAsync();
        }
    }

    private PriorityQueue<ScheduledJob, DateTimeOffset> CreateInitialSchedule(DateTimeOffset nowUtc)
    {
        var queue = new PriorityQueue<ScheduledJob, DateTimeOffset>();

        foreach (var job in jobs)
        {
            EnqueueNextOccurrence(queue, job, nowUtc);
        }

        return queue;
    }

    private void DispatchDueJobs(
        PriorityQueue<ScheduledJob, DateTimeOffset> queue,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        while (queue.TryPeek(out _, out var dueAtUtc) && dueAtUtc <= nowUtc)
        {
            var scheduledJob = queue.Dequeue();
            var isWithinGracePeriod = nowUtc - scheduledJob.OccurrenceUtc <= options.MisfireGracePeriod;

            if (isWithinGracePeriod && IsNewOccurrence(scheduledJob))
            {
                TrackDispatch(jobDispatcher.DispatchAsync(
                    scheduledJob.Job,
                    scheduledJob.OccurrenceUtc,
                    cancellationToken));
            }

            var nextSearchStart = isWithinGracePeriod ? scheduledJob.OccurrenceUtc : nowUtc;
            EnqueueNextOccurrence(queue, scheduledJob.Job, nextSearchStart);
        }
    }

    private bool IsNewOccurrence(ScheduledJob scheduledJob)
    {
        if (lastDispatchedOccurrences.TryGetValue(scheduledJob.Job.Id, out var lastOccurrence)
            && lastOccurrence == scheduledJob.OccurrenceUtc)
        {
            return false;
        }

        lastDispatchedOccurrences[scheduledJob.Job.Id] = scheduledJob.OccurrenceUtc;
        return true;
    }

    private void EnqueueNextOccurrence(
        PriorityQueue<ScheduledJob, DateTimeOffset> queue,
        CronJobDefinition job,
        DateTimeOffset afterUtc)
    {
        var nextOccurrence = CronOccurrenceCalculator.GetNextOccurrence(job.Schedule, afterUtc, options.TimeZone);
        if (nextOccurrence is not null)
        {
            queue.Enqueue(new ScheduledJob(job, nextOccurrence.Value), nextOccurrence.Value);
        }
    }

    private void TrackDispatch(Task dispatchTask)
    {
        var sequence = Interlocked.Increment(ref dispatchSequence);
        activeDispatches[sequence] = dispatchTask;

        _ = dispatchTask.ContinueWith(
            completedTask =>
            {
                activeDispatches.TryRemove(sequence, out _);
                if (completedTask.IsFaulted)
                {
                    Console.Error.WriteLine($"Scheduled job dispatch failed: {completedTask.Exception?.GetBaseException().Message}");
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private async Task AwaitActiveDispatchesAsync()
    {
        var dispatches = activeDispatches.Values.ToArray();
        if (dispatches.Length == 0)
        {
            return;
        }

        try
        {
            await Task.WhenAll(dispatches);
        }
        catch
        {
            // Dispatch failures were reported by their continuations.
        }
    }

    private sealed record ScheduledJob(CronJobDefinition Job, DateTimeOffset OccurrenceUtc);
}
