using Microsoft.Extensions.Logging.Abstractions;
using WinCron.Hosting;

namespace WinCron.Tests.Hosting;

public sealed class WinCronWindowsServiceTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        $"wincron-service-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ServiceStartsSchedulerAndStopsCleanly()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var configurationPath = Path.Combine(temporaryDirectory, "config.wc");
        await File.WriteAllTextAsync(configurationPath, string.Empty, TestContext.Current.CancellationToken);
        var service = new WinCronWindowsService(
            new WindowsServiceArguments(["--config", configurationPath]),
            NullLogger<WinCronWindowsService>.Instance);

        await service.StartAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        await service.StopAsync(TestContext.Current.CancellationToken);

        service.Dispose();
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
