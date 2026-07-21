namespace WinCron.Configuration;

public sealed class CronConfigurationWatcher : ICronConfigurationWatcher
{
    private readonly FileSystemWatcher watcher;
    private readonly TimeSpan debounceDelay;
    private readonly object syncRoot = new();
    private TaskCompletionSource? changeSignal;

    public CronConfigurationWatcher(string configurationPath, TimeSpan? debounceDelay = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationPath);

        var fullPath = Path.GetFullPath(configurationPath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("The configuration path must include a directory.", nameof(configurationPath));
        var fileName = Path.GetFileName(fullPath);
        this.debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(300);

        watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.FileName
                | NotifyFilters.LastWrite
                | NotifyFilters.CreationTime
                | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        watcher.Changed += HandleChange;
        watcher.Created += HandleChange;
        watcher.Deleted += HandleChange;
        watcher.Renamed += HandleRename;
        watcher.Error += HandleError;
    }

    public async Task WaitForChangeAsync(CancellationToken cancellationToken)
    {
        Task signalTask;
        lock (syncRoot)
        {
            changeSignal ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            signalTask = changeSignal.Task;
        }

        await signalTask.WaitAsync(cancellationToken);
        await Task.Delay(debounceDelay, cancellationToken);

        lock (syncRoot)
        {
            if (ReferenceEquals(changeSignal?.Task, signalTask))
            {
                changeSignal = null;
            }
        }
    }

    public void Dispose()
    {
        watcher.EnableRaisingEvents = false;
        watcher.Changed -= HandleChange;
        watcher.Created -= HandleChange;
        watcher.Deleted -= HandleChange;
        watcher.Renamed -= HandleRename;
        watcher.Error -= HandleError;
        watcher.Dispose();
    }

    private void HandleChange(object sender, FileSystemEventArgs eventArgs) => SignalChange();

    private void HandleRename(object sender, RenamedEventArgs eventArgs) => SignalChange();

    private void HandleError(object sender, ErrorEventArgs eventArgs) => SignalChange();

    private void SignalChange()
    {
        lock (syncRoot)
        {
            changeSignal ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            changeSignal.TrySetResult();
        }
    }
}
