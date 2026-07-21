using WinCron.Configuration;
using WinCron.Domain;
using WinCron.Scheduling;

namespace WinCron.Tests.Scheduling;

public sealed class ReloadingCronSchedulerTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"wincron-reload-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task RunAsyncActivatesCompleteValidReplacementConfiguration()
    {
        var configurationPath = await CreateConfigurationAsync("* * * * * echo reloaded");
        var watcher = new ControllableWatcher();
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var cancellationSource = new CancellationTokenSource();
        var scheduler = CreateScheduler(configurationPath, watcher, output, error);

        var schedulerTask = scheduler.RunAsync(new CronConfiguration([]), cancellationSource.Token);
        watcher.SignalChange();
        await WaitForTextAsync(output, "Reloaded 1 job(s)");
        await cancellationSource.CancelAsync();
        await schedulerTask;

        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task RunAsyncRejectsInvalidReplacementAndKeepsActiveConfiguration()
    {
        var configurationPath = await CreateConfigurationAsync("invalid");
        var watcher = new ControllableWatcher();
        using var output = new StringWriter();
        using var error = new StringWriter();
        using var cancellationSource = new CancellationTokenSource();
        var scheduler = CreateScheduler(configurationPath, watcher, output, error);

        var schedulerTask = scheduler.RunAsync(new CronConfiguration([]), cancellationSource.Token);
        watcher.SignalChange();
        await WaitForTextAsync(error, "kept the last valid configuration");
        await cancellationSource.CancelAsync();
        await schedulerTask;

        Assert.DoesNotContain("Reloaded", output.ToString());
        Assert.Contains("Line 1", error.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static ReloadingCronScheduler CreateScheduler(
        string configurationPath,
        ICronConfigurationWatcher watcher,
        TextWriter output,
        TextWriter error) =>
        new(
            new CronConfigurationLoader(configurationPath),
            new CronConfigurationParser(),
            watcher,
            new NoOpDispatcher(),
            output,
            error,
            new CronSchedulerOptions { TimeZone = TimeZoneInfo.Utc });

    private async Task<string> CreateConfigurationAsync(string contents)
    {
        Directory.CreateDirectory(temporaryDirectory);
        var configurationPath = Path.Combine(temporaryDirectory, "config.wc");
        await File.WriteAllTextAsync(configurationPath, contents, TestContext.Current.CancellationToken);
        return configurationPath;
    }

    private static async Task WaitForTextAsync(StringWriter writer, string expectedText)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!writer.ToString().Contains(expectedText, StringComparison.Ordinal))
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException($"Timed out waiting for '{expectedText}'.");
            }

            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private sealed class ControllableWatcher : ICronConfigurationWatcher
    {
        private readonly System.Threading.Channels.Channel<bool> changes =
            System.Threading.Channels.Channel.CreateUnbounded<bool>();

        public Task WaitForChangeAsync(CancellationToken cancellationToken) =>
            changes.Reader.ReadAsync(cancellationToken).AsTask();

        public void SignalChange() => changes.Writer.TryWrite(true);

        public void Dispose() => changes.Writer.TryComplete();
    }

    private sealed class NoOpDispatcher : IJobDispatcher
    {
        public Task DispatchAsync(
            CronJobDefinition job,
            DateTimeOffset scheduledOccurrenceUtc,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
