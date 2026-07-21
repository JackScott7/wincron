using WinCron.Configuration;

namespace WinCron.Tests.Configuration;

public sealed class CronConfigurationLoaderTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"wincron-config-tests-{Guid.NewGuid():N}");

    [Fact]
    public void ConstructorUsesWinCronDirectoryInUserProfileByDefault()
    {
        var loader = new CronConfigurationLoader();

        Assert.Equal(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "wincron",
                "config.wc"),
            loader.ConfigurationPath);
    }

    [Fact]
    public async Task LoadAsyncCreatesEmptyFileWhenConfigurationIsMissing()
    {
        var configurationPath = Path.Combine(temporaryDirectory, "nested", "config.wc");
        var loader = new CronConfigurationLoader(configurationPath);

        var configuration = await loader.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(string.Empty, configuration);
        Assert.True(File.Exists(configurationPath));
    }

    [Fact]
    public async Task LoadAsyncReturnsContentWhenConfigurationExists()
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

    [Fact]
    public async Task ReadAsyncReturnsContentWithoutModifyingExistingConfiguration()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var configurationPath = Path.Combine(temporaryDirectory, "config.wc");
        const string expectedConfiguration = "* * * * * echo test";
        await File.WriteAllTextAsync(
            configurationPath,
            expectedConfiguration,
            TestContext.Current.CancellationToken);
        var lastWriteTime = File.GetLastWriteTimeUtc(configurationPath);
        var loader = new CronConfigurationLoader(configurationPath);

        var configuration = await loader.ReadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expectedConfiguration, configuration);
        Assert.Equal(lastWriteTime, File.GetLastWriteTimeUtc(configurationPath));
    }

    [Fact]
    public async Task ReadAsyncThrowsAndDoesNotCreateMissingConfiguration()
    {
        var configurationPath = Path.Combine(temporaryDirectory, "nested", "config.wc");
        var loader = new CronConfigurationLoader(configurationPath);

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => loader.ReadAsync(TestContext.Current.CancellationToken));

        Assert.Equal(configurationPath, exception.FileName);
        Assert.False(File.Exists(configurationPath));
        Assert.False(Directory.Exists(Path.GetDirectoryName(configurationPath)));
    }

    [Fact]
    public async Task LoadAsyncCopiesLegacyConfigurationWhenNewPathIsMissing()
    {
        Directory.CreateDirectory(temporaryDirectory);
        var legacyConfigurationPath = Path.Combine(temporaryDirectory, "config.wc");
        var configurationPath = Path.Combine(temporaryDirectory, "wincron", "config.wc");
        const string expectedConfiguration = "*/1 * * * * echo migrated";
        await File.WriteAllTextAsync(
            legacyConfigurationPath,
            expectedConfiguration,
            TestContext.Current.CancellationToken);
        var loader = new CronConfigurationLoader(configurationPath, legacyConfigurationPath);

        var configuration = await loader.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expectedConfiguration, configuration);
        Assert.Equal(expectedConfiguration, await File.ReadAllTextAsync(
            configurationPath,
            TestContext.Current.CancellationToken));
        Assert.True(File.Exists(legacyConfigurationPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
