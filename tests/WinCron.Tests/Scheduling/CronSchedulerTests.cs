using WinCron.Domain;
using WinCron.Scheduling;

namespace WinCron.Tests.Scheduling;

public sealed class CronSchedulerTests
{
    [Fact]
    public async Task RunAsync_WhenOccurrenceBecomesDue_DispatchesItOnce()
    {
        var cancellationSource = new CancellationTokenSource();
        var timeProvider = new ManualTimeProvider(
            new DateTimeOffset(2026, 1, 1, 0, 0, 30, TimeSpan.Zero));
        var delay = new AdvancingSchedulerDelay(timeProvider);
        var dispatcher = new RecordingJobDispatcher(cancellationSource);
        var job = CreateJob("*", "*", "*", "*", "*");
        var scheduler = new CronScheduler(
            [job],
            dispatcher,
            new CronSchedulerOptions { TimeZone = TimeZoneInfo.Utc },
            timeProvider,
            delay);

        await scheduler.RunAsync(cancellationSource.Token);

        var dispatch = Assert.Single(dispatcher.Dispatches);
        Assert.Same(job, dispatch.Job);
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 0, 1, 0, TimeSpan.Zero), dispatch.OccurrenceUtc);
        Assert.Equal(1, delay.DelayCount);
    }

    [Fact]
    public async Task RunAsync_WhenClockJumpsPastGracePeriod_SkipsMissedOccurrence()
    {
        var cancellationSource = new CancellationTokenSource();
        var timeProvider = new ManualTimeProvider(
            new DateTimeOffset(2026, 1, 1, 0, 0, 30, TimeSpan.Zero));
        var delay = new AdvancingSchedulerDelay(
            timeProvider,
            requestedDelay => requestedDelay + TimeSpan.FromMinutes(5),
            cancellationSource,
            cancelAfterDelayCount: 2);
        var dispatcher = new RecordingJobDispatcher();
        var scheduler = new CronScheduler(
            [CreateJob("*", "*", "*", "*", "*")],
            dispatcher,
            new CronSchedulerOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                MisfireGracePeriod = TimeSpan.FromSeconds(30)
            },
            timeProvider,
            delay);

        await scheduler.RunAsync(cancellationSource.Token);

        Assert.Empty(dispatcher.Dispatches);
    }

    private static CronJobDefinition CreateJob(
        string minute,
        string hour,
        string dayOfMonth,
        string month,
        string dayOfWeek)
    {
        var expression = new CronExpression(
            Parse(CronFieldKind.Minute, minute),
            Parse(CronFieldKind.Hour, hour),
            Parse(CronFieldKind.DayOfMonth, dayOfMonth),
            Parse(CronFieldKind.Month, month),
            Parse(CronFieldKind.DayOfWeek, dayOfWeek));

        return new CronJobDefinition(expression, "echo test", new Dictionary<string, string>(), @"C:\", 1);
    }

    private static CronField Parse(CronFieldKind kind, string expression)
    {
        Assert.True(CronField.TryParse(kind, expression, out var field, out var error), error);
        return field!;
    }

    private sealed class ManualTimeProvider(DateTimeOffset initialTime) : TimeProvider
    {
        private DateTimeOffset utcNow = initialTime;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan duration) => utcNow += duration;
    }

    private sealed class AdvancingSchedulerDelay(
        ManualTimeProvider timeProvider,
        Func<TimeSpan, TimeSpan>? durationSelector = null,
        CancellationTokenSource? cancellationSource = null,
        int cancelAfterDelayCount = int.MaxValue) : ISchedulerDelay
    {
        public int DelayCount { get; private set; }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DelayCount++;
            timeProvider.Advance(durationSelector?.Invoke(delay) ?? delay);

            if (DelayCount >= cancelAfterDelayCount)
            {
                cancellationSource?.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Task.CompletedTask;
        }
    }

    private sealed class RecordingJobDispatcher(CancellationTokenSource? cancellationSource = null) : IJobDispatcher
    {
        public List<(CronJobDefinition Job, DateTimeOffset OccurrenceUtc)> Dispatches { get; } = [];

        public Task DispatchAsync(
            CronJobDefinition job,
            DateTimeOffset scheduledOccurrenceUtc,
            CancellationToken cancellationToken)
        {
            Dispatches.Add((job, scheduledOccurrenceUtc));
            cancellationSource?.Cancel();
            return Task.CompletedTask;
        }
    }
}
