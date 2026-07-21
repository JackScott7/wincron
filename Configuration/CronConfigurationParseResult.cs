namespace WinCron.Configuration;

public sealed class CronConfigurationParseResult
{
    public CronConfigurationParseResult(CronConfiguration configuration, IReadOnlyList<CronParseError> errors)
    {
        Configuration = configuration;
        Errors = errors;
    }

    public CronConfiguration Configuration { get; }

    public IReadOnlyList<CronParseError> Errors { get; }

    public bool IsValid => Errors.Count == 0;
}
