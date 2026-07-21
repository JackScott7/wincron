using WinCron.Configuration;

namespace WinCron.Scheduling;

public sealed class ReloadingCronScheduler(
    CronConfigurationLoader configurationLoader,
    CronConfigurationParser configurationParser,
    ICronConfigurationWatcher configurationWatcher,
    IJobDispatcher jobDispatcher,
    TextWriter standardOutput,
    TextWriter standardError,
    CronSchedulerOptions? schedulerOptions = null)
{
    private readonly CronConfigurationLoader configurationLoader = configurationLoader
        ?? throw new ArgumentNullException(nameof(configurationLoader));
    private readonly CronConfigurationParser configurationParser = configurationParser
        ?? throw new ArgumentNullException(nameof(configurationParser));
    private readonly ICronConfigurationWatcher configurationWatcher = configurationWatcher
        ?? throw new ArgumentNullException(nameof(configurationWatcher));
    private readonly IJobDispatcher jobDispatcher = jobDispatcher
        ?? throw new ArgumentNullException(nameof(jobDispatcher));
    private readonly TextWriter standardOutput = standardOutput
        ?? throw new ArgumentNullException(nameof(standardOutput));
    private readonly TextWriter standardError = standardError
        ?? throw new ArgumentNullException(nameof(standardError));
    private readonly CronSchedulerOptions schedulerOptions = schedulerOptions ?? new CronSchedulerOptions();

    public async Task RunAsync(
        CronConfiguration initialConfiguration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(initialConfiguration);

        var activeConfiguration = initialConfiguration;
        while (!cancellationToken.IsCancellationRequested)
        {
            using var schedulerCancellationSource = new CancellationTokenSource();
            var scheduler = new CronScheduler(
                activeConfiguration.Jobs,
                jobDispatcher,
                schedulerOptions);
            var schedulerTask = scheduler.RunAsync(
                schedulerCancellationSource.Token,
                cancellationToken);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await configurationWatcher.WaitForChangeAsync(cancellationToken);
                    var reloadResult = await TryReloadAsync(cancellationToken);
                    if (reloadResult is null)
                    {
                        continue;
                    }

                    activeConfiguration = reloadResult;
                    schedulerCancellationSource.Cancel();
                    await schedulerTask;
                    await standardOutput.WriteLineAsync(
                        $"Reloaded {activeConfiguration.Jobs.Count} job(s) from {configurationLoader.ConfigurationPath}.");
                    break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                schedulerCancellationSource.Cancel();
                await schedulerTask;
                return;
            }
        }
    }

    private async Task<CronConfiguration?> TryReloadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var configurationText = await configurationLoader.ReadAsync(cancellationToken);
            var parseResult = configurationParser.Parse(configurationText);
            if (parseResult.IsValid)
            {
                return parseResult.Configuration;
            }

            await standardError.WriteLineAsync(
                "WinCron rejected a configuration reload and kept the last valid configuration:");
            foreach (var error in parseResult.Errors)
            {
                await standardError.WriteLineAsync(error.ToString());
            }
        }
        catch (IOException exception)
        {
            await standardError.WriteLineAsync(
                $"WinCron could not reload the configuration and kept the last valid configuration: {exception.Message}");
        }

        return null;
    }
}
