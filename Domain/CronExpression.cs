namespace WinCron.Domain;

public sealed class CronExpression
{
    public CronExpression(
        CronField minute,
        CronField hour,
        CronField dayOfMonth,
        CronField month,
        CronField dayOfWeek)
    {
        ArgumentNullException.ThrowIfNull(minute);
        ArgumentNullException.ThrowIfNull(hour);
        ArgumentNullException.ThrowIfNull(dayOfMonth);
        ArgumentNullException.ThrowIfNull(month);
        ArgumentNullException.ThrowIfNull(dayOfWeek);

        EnsureFieldKind(minute, CronFieldKind.Minute);
        EnsureFieldKind(hour, CronFieldKind.Hour);
        EnsureFieldKind(dayOfMonth, CronFieldKind.DayOfMonth);
        EnsureFieldKind(month, CronFieldKind.Month);
        EnsureFieldKind(dayOfWeek, CronFieldKind.DayOfWeek);

        Minute = minute;
        Hour = hour;
        DayOfMonth = dayOfMonth;
        Month = month;
        DayOfWeek = dayOfWeek;
    }

    public CronField Minute { get; }

    public CronField Hour { get; }

    public CronField DayOfMonth { get; }

    public CronField Month { get; }

    public CronField DayOfWeek { get; }

    public bool Matches(DateTime localTime)
    {
        if (!Minute.Matches(localTime.Minute)
            || !Hour.Matches(localTime.Hour)
            || !Month.Matches(localTime.Month))
        {
            return false;
        }

        var dayOfMonthMatches = DayOfMonth.Matches(localTime.Day);
        var dayOfWeekMatches = DayOfWeek.Matches((int)localTime.DayOfWeek);

        if (DayOfMonth.IsWildcard)
        {
            return dayOfWeekMatches;
        }

        if (DayOfWeek.IsWildcard)
        {
            return dayOfMonthMatches;
        }

        return dayOfMonthMatches || dayOfWeekMatches;
    }

    public override string ToString() =>
        $"{Minute.Source} {Hour.Source} {DayOfMonth.Source} {Month.Source} {DayOfWeek.Source}";

    private static void EnsureFieldKind(CronField field, CronFieldKind expectedKind)
    {
        if (field.Kind != expectedKind)
        {
            throw new ArgumentException(
                $"Expected a {expectedKind} field but received {field.Kind}.",
                nameof(field));
        }
    }
}
