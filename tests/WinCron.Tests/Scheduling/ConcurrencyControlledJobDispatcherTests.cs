using WinCron.Domain;
using WinCron.Scheduling;

namespace WinCron.Tests.Scheduling;

public sealed class ConcurrencyControlledJobDispatcherTests
{
    [Fact]
    public async Task SkipPolicyDoesNotStartOverlappingExecution()
    {
        var innerDispatcher = new BlockingDispatcher();
        var dispatcher = new ConcurrencyControlledJobDispatcher(innerDispatcher);
        var job = CreateJob(JobOverlapPolicy.Skip);

        var firstDispatch = dispatcher.DispatchAsync(job, DateTimeOffset.UtcNow, CancellationToken.None);
        await innerDispatcher.Started.Task;
        var secondDispatch = dispatcher.DispatchAsync(job, DateTimeOffset.UtcNow.AddMinutes(1), CancellationToken.None);

        Assert.True(secondDispatch.IsCompletedSuccessfully);
        Assert.Equal(1, innerDispatcher.DispatchCount);
        innerDispatcher.Release.TrySetResult();
        await firstDispatch;
    }

    [Fact]
    public async Task QueueOnePolicyKeepsOnlyLatestPendingExecution()
    {
        var innerDispatcher = new BlockingDispatcher();
        var dispatcher = new ConcurrencyControlledJobDispatcher(innerDispatcher);
        var job = CreateJob(JobOverlapPolicy.QueueOne);
        var firstOccurrence = DateTimeOffset.UtcNow;
        var latestOccurrence = firstOccurrence.AddMinutes(2);

        var firstDispatch = dispatcher.DispatchAsync(job, firstOccurrence, CancellationToken.None);
        await innerDispatcher.Started.Task;
        _ = dispatcher.DispatchAsync(job, firstOccurrence.AddMinutes(1), CancellationToken.None);
        _ = dispatcher.DispatchAsync(job, latestOccurrence, CancellationToken.None);
        innerDispatcher.Release.TrySetResult();
        await firstDispatch;

        Assert.Equal(2, innerDispatcher.DispatchCount);
        Assert.Equal(latestOccurrence, innerDispatcher.Occurrences[^1]);
    }

    [Fact]
    public async Task AllowPolicyStartsConcurrentExecutions()
    {
        var innerDispatcher = new BlockingDispatcher();
        var dispatcher = new ConcurrencyControlledJobDispatcher(innerDispatcher);
        var job = CreateJob(JobOverlapPolicy.Allow);

        var firstDispatch = dispatcher.DispatchAsync(job, DateTimeOffset.UtcNow, CancellationToken.None);
        await innerDispatcher.Started.Task;
        var secondDispatch = dispatcher.DispatchAsync(job, DateTimeOffset.UtcNow.AddMinutes(1), CancellationToken.None);

        Assert.Equal(2, innerDispatcher.DispatchCount);
        innerDispatcher.Release.TrySetResult();
        await Task.WhenAll(firstDispatch, secondDispatch);
    }

    [Fact]
    public async Task TerminatePreviousPolicyCancelsPreviousBeforeStartingReplacement()
    {
        var innerDispatcher = new CancellationRecordingDispatcher();
        var dispatcher = new ConcurrencyControlledJobDispatcher(innerDispatcher);
        var job = CreateJob(JobOverlapPolicy.TerminatePrevious);

        var firstDispatch = dispatcher.DispatchAsync(job, DateTimeOffset.UtcNow, CancellationToken.None);
        await innerDispatcher.FirstStarted.Task;
        var replacementDispatch = dispatcher.DispatchAsync(
            job,
            DateTimeOffset.UtcNow.AddMinutes(1),
            CancellationToken.None);
        await replacementDispatch;

        Assert.True(innerDispatcher.FirstWasCanceled);
        Assert.Equal(2, innerDispatcher.DispatchCount);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstDispatch);
    }

    private static CronJobDefinition CreateJob(JobOverlapPolicy overlapPolicy)
    {
        var schedule = new CronExpression(
            Parse(CronFieldKind.Minute),
            Parse(CronFieldKind.Hour),
            Parse(CronFieldKind.DayOfMonth),
            Parse(CronFieldKind.Month),
            Parse(CronFieldKind.DayOfWeek));
        return new CronJobDefinition(
            schedule,
            "echo test",
            new Dictionary<string, string>(),
            @"C:\",
            1,
            new JobExecutionOptions { OverlapPolicy = overlapPolicy });
    }

    private static CronField Parse(CronFieldKind kind)
    {
        Assert.True(CronField.TryParse(kind, "*", out var field, out var error), error);
        return field!;
    }

    private sealed class BlockingDispatcher : IJobDispatcher
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<DateTimeOffset> Occurrences { get; } = [];

        public int DispatchCount => Occurrences.Count;

        public async Task DispatchAsync(
            CronJobDefinition job,
            DateTimeOffset scheduledOccurrenceUtc,
            CancellationToken cancellationToken)
        {
            Occurrences.Add(scheduledOccurrenceUtc);
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class CancellationRecordingDispatcher : IJobDispatcher
    {
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DispatchCount { get; private set; }

        public bool FirstWasCanceled { get; private set; }

        public async Task DispatchAsync(
            CronJobDefinition job,
            DateTimeOffset scheduledOccurrenceUtc,
            CancellationToken cancellationToken)
        {
            DispatchCount++;
            if (DispatchCount > 1)
            {
                return;
            }

            FirstStarted.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                FirstWasCanceled = true;
                throw;
            }
        }
    }
}
