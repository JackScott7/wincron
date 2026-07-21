using WinCron.Domain;

namespace WinCron.Configuration;

public sealed record CronConfiguration(IReadOnlyList<CronJobDefinition> Jobs);
