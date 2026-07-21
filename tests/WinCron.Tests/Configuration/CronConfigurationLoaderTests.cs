using WinCron.Configuration;

namespace WinCron.Tests.Configuration;

public sealed class CronConfigurationLoaderTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"wincron-config-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task LoadAsync_WhenConfigurationIsMissing_CreatesEmptyFile()
    {
        var configurationPath = Path.Combine(temporaryDirectory, "nested", "config.wc");
        var loader = new CronConfigurationLoader(configurationPath);

        var configuration = await loader.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, configuration);
        Assert.True(File.Exists(configurationPath));
    }

    [Fact]
    public async Task LoadAsync_WhenConfigurationExists_ReturnsItsContent()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var configurationPath = Path.Combine(temporaryDirectory, "config.wc");
        await File.WriteAllTextAsync(
            configurationPath,
            "* * * * * echo test",
            TestContext.Current.CancellationToken);
        var loader = new CronConfigurationLoader(configurationPath);

        var configuration = await loader.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal("* * * * * echo test", configuration);
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
