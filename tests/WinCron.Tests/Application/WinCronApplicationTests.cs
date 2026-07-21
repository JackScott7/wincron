using WinCron.Application;

namespace WinCron.Tests.Application;

public sealed class WinCronApplicationTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"wincron-cli-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task RunAsyncPrintsHelpAndReturnsSuccess()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new WinCronApplication(output, error);

        var exitCode = await application.RunAsync(["--help"], TestContext.Current.CancellationToken);

        Assert.Equal(WinCronApplication.SuccessExitCode, exitCode);
        Assert.Contains("Usage:", output.ToString());
        Assert.Contains("--config <path>", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task RunAsyncPrintsSemanticVersionAndReturnsSuccess()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new WinCronApplication(output, error);

        var exitCode = await application.RunAsync(["-V"], TestContext.Current.CancellationToken);

        Assert.Equal(WinCronApplication.SuccessExitCode, exitCode);
        Assert.Equal($"WinCron 1.2.1{Environment.NewLine}", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task RunAsyncListsSelectedConfigurationAndReturnsSuccess()
    {
        const string configuration = "*/5 * * * * echo listed";
        var configurationPath = await CreateConfigurationAsync(configuration);
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new WinCronApplication(output, error);

        var exitCode = await application.RunAsync(
            ["--list", "--config", configurationPath],
            TestContext.Current.CancellationToken);

        Assert.Equal(WinCronApplication.SuccessExitCode, exitCode);
        Assert.Equal(configuration, output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task RunAsyncTestsValidConfigurationWithoutRunningJobs()
    {
        var configurationPath = await CreateConfigurationAsync("* * * * * echo valid");
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new WinCronApplication(output, error);

        var exitCode = await application.RunAsync(
            ["--test", $"--config={configurationPath}"],
            TestContext.Current.CancellationToken);

        Assert.Equal(WinCronApplication.SuccessExitCode, exitCode);
        Assert.Contains("Configuration is valid. Loaded 1 job(s)", output.ToString());
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task RunAsyncReportsEveryInvalidConfigurationLineAndReturnsRuntimeError()
    {
        var configurationPath = await CreateConfigurationAsync("60 * * * * echo bad\n* * *");
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new WinCronApplication(output, error);

        var exitCode = await application.RunAsync(
            ["-T", "--config", configurationPath],
            TestContext.Current.CancellationToken);

        Assert.Equal(WinCronApplication.RuntimeErrorExitCode, exitCode);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains("Line 1:", error.ToString());
        Assert.Contains("Line 2:", error.ToString());
    }

    [Theory]
    [InlineData("--test")]
    [InlineData("--list")]
    public async Task ReadOnlyModeReportsMissingCustomConfigurationWithoutCreatingIt(string mode)
    {
        var configurationPath = Path.Combine(temporaryDirectory, "missing", "config.wc");
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new WinCronApplication(output, error);

        var exitCode = await application.RunAsync(
            [mode, "--config", configurationPath],
            TestContext.Current.CancellationToken);

        Assert.Equal(WinCronApplication.RuntimeErrorExitCode, exitCode);
        Assert.Contains("does not exist", error.ToString());
        Assert.False(File.Exists(configurationPath));
        Assert.False(Directory.Exists(Path.GetDirectoryName(configurationPath)));
    }

    [Fact]
    public async Task RunAsyncReportsUsageAndReturnsUsageErrorForUnknownArgument()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var application = new WinCronApplication(output, error);

        var exitCode = await application.RunAsync(["--unknown"], TestContext.Current.CancellationToken);

        Assert.Equal(WinCronApplication.UsageErrorExitCode, exitCode);
        Assert.Equal(string.Empty, output.ToString());
        Assert.Contains("Unknown argument", error.ToString());
        Assert.Contains("Usage:", error.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private async Task<string> CreateConfigurationAsync(string contents)
    {
        Directory.CreateDirectory(temporaryDirectory);
        var configurationPath = Path.Combine(temporaryDirectory, $"{Guid.NewGuid():N}.wc");
        await File.WriteAllTextAsync(
            configurationPath,
            contents,
            TestContext.Current.CancellationToken);
        return configurationPath;
    }
}
