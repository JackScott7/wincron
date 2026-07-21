using System.Collections.Concurrent;
using WinCron.Domain;

namespace WinCron.Scheduling;

public sealed class ConcurrencyControlledJobDispatcher(IJobDispatcher innerDispatcher) : IJobDispatcher
{
    private readonly IJobDispatcher innerDispatcher = innerDispatcher
        ?? throw new ArgumentNullException(nameof(innerDispatcher));
    private readonly ConcurrentDictionary<string, JobDispatchState> jobStates = new();

    public Task DispatchAsync(
        CronJobDefinition job,
        DateTimeOffset scheduledOccurrenceUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.ExecutionOptions.OverlapPolicy == JobOverlapPolicy.Allow)
        {
            return innerDispatcher.DispatchAsync(job, scheduledOccurrenceUtc, cancellationToken);
        }

        var state = jobStates.GetOrAdd(job.Id, static _ => new JobDispatchState());
        var request = new DispatchRequest(job, scheduledOccurrenceUtc, cancellationToken);

        return job.ExecutionOptions.OverlapPolicy switch
        {
            JobOverlapPolicy.Skip => DispatchOrSkip(state, request),
            JobOverlapPolicy.QueueOne => DispatchOrQueue(state, request),
            JobOverlapPolicy.TerminatePrevious => DispatchAndTerminatePrevious(state, request),
            _ => throw new ArgumentOutOfRangeException(
                nameof(job),
                job.ExecutionOptions.OverlapPolicy,
                "Unsupported overlap policy.")
        };
    }

    private Task DispatchOrSkip(JobDispatchState state, DispatchRequest request)
    {
        lock (state.SyncRoot)
        {
            if (state.ActiveTask is { IsCompleted: false })
            {
                return Task.CompletedTask;
            }

            state.ActiveTask = RunSingleAsync(state, request);
            return state.ActiveTask;
        }
    }

    private Task DispatchOrQueue(JobDispatchState state, DispatchRequest request)
    {
        lock (state.SyncRoot)
        {
            if (state.ActiveTask is { IsCompleted: false })
            {
                state.PendingRequest = request;
                return state.ActiveTask;
            }

            state.ActiveTask = RunQueueAsync(state, request);
            return state.ActiveTask;
        }
    }

    private Task DispatchAndTerminatePrevious(JobDispatchState state, DispatchRequest request)
    {
        lock (state.SyncRoot)
        {
            var previousTask = state.ActiveTask ?? Task.CompletedTask;
            if (!previousTask.IsCompleted)
            {
                state.ActiveCancellationSource?.Cancel();
            }

            state.ActiveCancellationSource?.Dispose();
            var replacementCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                request.CancellationToken);
            state.ActiveCancellationSource = replacementCancellationSource;
            var replacement = request with { CancellationToken = replacementCancellationSource.Token };
            state.ActiveTask = RunAfterPreviousAsync(
                previousTask,
                replacement,
                replacementCancellationSource);
            return state.ActiveTask;
        }
    }

    private async Task RunSingleAsync(JobDispatchState state, DispatchRequest request)
    {
        try
        {
            await innerDispatcher.DispatchAsync(
                request.Job,
                request.ScheduledOccurrenceUtc,
                request.CancellationToken);
        }
        finally
        {
            lock (state.SyncRoot)
            {
                state.ActiveTask = null;
            }
        }
    }

    private async Task RunQueueAsync(JobDispatchState state, DispatchRequest initialRequest)
    {
        var request = initialRequest;

        while (true)
        {
            await innerDispatcher.DispatchAsync(
                request.Job,
                request.ScheduledOccurrenceUtc,
                request.CancellationToken);

            lock (state.SyncRoot)
            {
                if (state.PendingRequest is null)
                {
                    state.ActiveTask = null;
                    return;
                }

                request = state.PendingRequest;
                state.PendingRequest = null;
            }
        }
    }

    private async Task RunAfterPreviousAsync(
        Task previousTask,
        DispatchRequest replacement,
        CancellationTokenSource replacementCancellationSource)
    {
        try
        {
            await previousTask;
        }
        catch (OperationCanceledException)
        {
            // The replacement intentionally canceled the previous execution.
        }
        catch
        {
            // The scheduler reports dispatch failures; a failed predecessor must not block its replacement.
        }

        try
        {
            await innerDispatcher.DispatchAsync(
                replacement.Job,
                replacement.ScheduledOccurrenceUtc,
                replacement.CancellationToken);
        }
        finally
        {
            replacementCancellationSource.Dispose();
        }
    }

    private sealed class JobDispatchState
    {
        public object SyncRoot { get; } = new();

        public Task? ActiveTask { get; set; }

        public DispatchRequest? PendingRequest { get; set; }

        public CancellationTokenSource? ActiveCancellationSource { get; set; }
    }

    private sealed record DispatchRequest(
        CronJobDefinition Job,
        DateTimeOffset ScheduledOccurrenceUtc,
        CancellationToken CancellationToken);
}
