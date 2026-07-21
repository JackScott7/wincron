using WinCron.Configuration;
using WinCron.Execution;
using WinCron.Scheduling;

using var shutdownSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdownSource.Cancel();
};

var configurationLoader = new CronConfigurationLoader();
var configurationText = await configurationLoader.LoadAsync(shutdownSource.Token);
var configurationParser = new CronConfigurationParser();
var parseResult = configurationParser.Parse(configurationText);

if (!parseResult.IsValid)
{
    foreach (var error in parseResult.Errors)
    {
        Console.Error.WriteLine(error);
    }

    Environment.ExitCode = 1;
    return;
}

Console.WriteLine($"WinCron loaded {parseResult.Configuration.Jobs.Count} job(s) from {configurationLoader.ConfigurationPath}.");
Console.WriteLine($"Schedules use the '{TimeZoneInfo.Local.DisplayName}' time zone. Press Ctrl+C to stop.");

using var executionLogger = new JsonFileJobExecutionLogger();
var jobExecutor = new JobExecutor(executionLogger);
var scheduler = new CronScheduler(
    parseResult.Configuration.Jobs,
    jobExecutor,
    new CronSchedulerOptions
    {
        TimeZone = TimeZoneInfo.Local,
        MisfireGracePeriod = TimeSpan.FromMinutes(1)
    });

await scheduler.RunAsync(shutdownSource.Token);
