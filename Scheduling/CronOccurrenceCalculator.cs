using WinCron.Domain;

namespace WinCron.Scheduling;

public static class CronOccurrenceCalculator
{
    private static readonly TimeSpan SearchHorizon = TimeSpan.FromDays(366 * 8);

    public static DateTimeOffset? GetNextOccurrence(
        CronExpression expression,
        DateTimeOffset after,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(timeZone);

        var afterUtc = after.ToUniversalTime();
        var utcTicks = afterUtc.Ticks - (afterUtc.Ticks % TimeSpan.TicksPerMinute) + TimeSpan.TicksPerMinute;
        var candidateUtc = new DateTimeOffset(utcTicks, TimeSpan.Zero);
        var searchLimit = afterUtc + SearchHorizon;

        while (candidateUtc <= searchLimit)
        {
            var localCandidate = TimeZoneInfo.ConvertTime(candidateUtc, timeZone).DateTime;
            if (expression.Matches(localCandidate))
            {
                return candidateUtc;
            }

            candidateUtc = candidateUtc.AddMinutes(1);
        }

        return null;
    }
}
