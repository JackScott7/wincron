using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinCron.Application;

namespace WinCron.Hosting;

public sealed class WinCronWindowsService(
    WindowsServiceArguments serviceArguments,
    ILogger<WinCronWindowsService> logger) : BackgroundService
{
    private readonly WindowsServiceArguments serviceArguments = serviceArguments
        ?? throw new ArgumentNullException(nameof(serviceArguments));
    private readonly ILogger<WinCronWindowsService> logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var standardOutput = new LoggerTextWriter(logger, LogLevel.Information);
        using var standardError = new LoggerTextWriter(logger, LogLevel.Error);
        var application = new WinCronApplication(standardOutput, standardError);
        var exitCode = await application.RunAsync(serviceArguments.Arguments, stoppingToken);
        if (exitCode != WinCronApplication.SuccessExitCode && !stoppingToken.IsCancellationRequested)
        {
            throw new InvalidOperationException($"WinCron stopped with exit code {exitCode}.");
        }
    }
}
