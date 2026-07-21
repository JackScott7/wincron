using WinCron.Configuration;

namespace WinCron.Tests.Configuration;

public sealed class CronConfigurationWatcherTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"wincron-watcher-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task WaitForChangeAsyncSignalsAfterConfigurationWrite()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var configurationPath = Path.Combine(temporaryDirectory, "config.wc");
        await File.WriteAllTextAsync(configurationPath, string.Empty, TestContext.Current.CancellationToken);
        using var watcher = new CronConfigurationWatcher(configurationPath, TimeSpan.FromMilliseconds(10));

        var changeTask = watcher.WaitForChangeAsync(TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(
            configurationPath,
            "* * * * * echo changed",
            TestContext.Current.CancellationToken);

        await changeTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
