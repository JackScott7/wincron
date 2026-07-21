using WinCron.CommandLine;
using WinCron.Configuration;
using WinCron.Execution;
using WinCron.Scheduling;

namespace WinCron.Application;

public sealed class WinCronApplication
{
    public const int SuccessExitCode = 0;
    public const int RuntimeErrorExitCode = 1;
    public const int UsageErrorExitCode = 2;

    private readonly TextWriter standardOutput;
    private readonly TextWriter standardError;

    public WinCronApplication(TextWriter standardOutput, TextWriter standardError)
    {
        this.standardOutput = standardOutput ?? throw new ArgumentNullException(nameof(standardOutput));
        this.standardError = standardError ?? throw new ArgumentNullException(nameof(standardError));
    }

    public async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var commandLineResult = WinCronCommandLineParser.Parse(arguments);
        if (!commandLineResult.IsValid)
        {
            foreach (var error in commandLineResult.Errors)
            {
                await standardError.WriteLineAsync($"WinCron: {error}");
            }

            await standardError.WriteLineAsync();
            await standardError.WriteLineAsync(WinCronUsage.Text);
            return UsageErrorExitCode;
        }

        var options = commandLineResult.Options!;

        try
        {
            return options.Mode switch
            {
                WinCronCommandMode.Help => await PrintHelpAsync(),
                WinCronCommandMode.Version => await PrintVersionAsync(),
                WinCronCommandMode.Test => await TestConfigurationAsync(options, cancellationToken),
                WinCronCommandMode.List => await ListConfigurationAsync(options, cancellationToken),
                WinCronCommandMode.Run => await RunSchedulerAsync(options, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported command mode '{options.Mode}'.")
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return SuccessExitCode;
        }
        catch (Exception exception)
        {
            await standardError.WriteLineAsync($"WinCron: {exception.Message}");
            return RuntimeErrorExitCode;
        }
    }

    private async Task<int> PrintHelpAsync()
    {
        await standardOutput.WriteLineAsync(WinCronUsage.Text);
        return SuccessExitCode;
    }

    private async Task<int> PrintVersionAsync()
    {
        var assemblyVersion = typeof(WinCronApplication).Assembly.GetName().Version
            ?? throw new InvalidOperationException("The WinCron assembly version is unavailable.");
        var semanticVersion = $"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}";

        await standardOutput.WriteLineAsync($"WinCron {semanticVersion}");
        return SuccessExitCode;
    }

    private async Task<int> TestConfigurationAsync(
        WinCronCommandLineOptions options,
        CancellationToken cancellationToken)
    {
        var loader = CreateConfigurationLoader(options.ConfigurationPath);
        var parseResult = await ReadAndParseConfigurationAsync(loader, cancellationToken);
        if (!parseResult.IsValid)
        {
            await WriteConfigurationErrorsAsync(parseResult.Errors);
            return RuntimeErrorExitCode;
        }

        await standardOutput.WriteLineAsync(
            $"Configuration is valid. Loaded {parseResult.Configuration.Jobs.Count} job(s) from {loader.ConfigurationPath}.");
        return SuccessExitCode;
    }

    private async Task<int> ListConfigurationAsync(
        WinCronCommandLineOptions options,
        CancellationToken cancellationToken)
    {
        var loader = CreateConfigurationLoader(options.ConfigurationPath);
        var configurationText = await loader.ReadAsync(cancellationToken);
        await standardOutput.WriteAsync(configurationText);
        return SuccessExitCode;
    }

    private async Task<int> RunSchedulerAsync(
        WinCronCommandLineOptions options,
        CancellationToken cancellationToken)
    {
        var loader = CreateConfigurationLoader(options.ConfigurationPath);
        using var instanceLock = WinCronInstanceLock.Acquire(loader.ConfigurationPath);
        var configurationText = await loader.LoadAsync(cancellationToken);
        var parseResult = new CronConfigurationParser().Parse(configurationText);
        if (!parseResult.IsValid)
        {
            await WriteConfigurationErrorsAsync(parseResult.Errors);
            return RuntimeErrorExitCode;
        }

        await standardOutput.WriteLineAsync(
            $"WinCron loaded {parseResult.Configuration.Jobs.Count} job(s) from {loader.ConfigurationPath}.");
        if (parseResult.Configuration.Jobs.Count == 0)
        {
            await standardOutput.WriteLineAsync("No jobs are configured. WinCron will remain idle until stopped.");
        }

        await standardOutput.WriteLineAsync(
            $"Schedules use the '{TimeZoneInfo.Local.DisplayName}' time zone. Press Ctrl+C to stop.");

        var configurationDirectory = Path.GetDirectoryName(loader.ConfigurationPath);
        var logDirectory = string.IsNullOrWhiteSpace(configurationDirectory)
            ? null
            : Path.Combine(configurationDirectory, "output");
        using var fileExecutionLogger = new JsonFileJobExecutionLogger(logDirectory);
        using var consoleExecutionLogger = new ConsoleJobExecutionLogger(
            standardOutput,
            standardError,
            includeCapturedOutput: false);
        using var outputObserver = new TextWriterJobOutputObserver(standardOutput, standardError);
        var logFailureReporter = new TextWriterExecutionLogFailureReporter(standardError);
        var executionLogger = new CompositeJobExecutionLogger(
            logFailureReporter,
            consoleExecutionLogger,
            fileExecutionLogger);
        var jobExecutor = new JobExecutor(executionLogger, outputObserver: outputObserver);
        var jobDispatcher = new ConcurrencyControlledJobDispatcher(jobExecutor);
        var schedulerOptions = new CronSchedulerOptions
        {
            TimeZone = TimeZoneInfo.Local,
            MisfireGracePeriod = TimeSpan.FromMinutes(1)
        };
        using var configurationWatcher = new CronConfigurationWatcher(loader.ConfigurationPath);
        var scheduler = new ReloadingCronScheduler(
            loader,
            new CronConfigurationParser(),
            configurationWatcher,
            jobDispatcher,
            standardOutput,
            standardError,
            schedulerOptions);

        await scheduler.RunAsync(parseResult.Configuration, cancellationToken);
        return SuccessExitCode;
    }

    private static CronConfigurationLoader CreateConfigurationLoader(string? configurationPath) =>
        configurationPath is null
            ? new CronConfigurationLoader()
            : new CronConfigurationLoader(configurationPath);

    private static async Task<CronConfigurationParseResult> ReadAndParseConfigurationAsync(
        CronConfigurationLoader loader,
        CancellationToken cancellationToken)
    {
        var configurationText = await loader.ReadAsync(cancellationToken);
        var parser = new CronConfigurationParser();
        return parser.Parse(configurationText);
    }

    private async Task WriteConfigurationErrorsAsync(IReadOnlyList<CronParseError> errors)
    {
        foreach (var error in errors)
        {
            await standardError.WriteLineAsync(error.ToString());
        }
    }
}
