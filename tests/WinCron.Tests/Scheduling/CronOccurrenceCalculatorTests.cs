using WinCron.Domain;
using WinCron.Scheduling;

namespace WinCron.Tests.Scheduling;

public sealed class CronOccurrenceCalculatorTests
{
    private readonly CronOccurrenceCalculator calculator = new();

    [Fact]
    public void GetNextOccurrence_WithUtcSchedule_ReturnsNextMatchingMinute()
    {
        var expression = CreateExpression("*/15", "*", "*", "*", "*");
        var after = new DateTimeOffset(2026, 1, 1, 10, 7, 30, TimeSpan.Zero);

        var occurrence = calculator.GetNextOccurrence(expression, after, TimeZoneInfo.Utc);

        Assert.Equal(new DateTimeOffset(2026, 1, 1, 10, 15, 0, TimeSpan.Zero), occurrence);
    }

    [Fact]
    public void GetNextOccurrence_DuringSpringForward_SkipsNonexistentLocalTime()
    {
        var easternTime = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var expression = CreateExpression("30", "2", "8", "MAR", "*");
        var after = new DateTimeOffset(2026, 3, 8, 5, 0, 0, TimeSpan.Zero);

        var occurrence = calculator.GetNextOccurrence(expression, after, easternTime);

        Assert.Equal(new DateTimeOffset(2027, 3, 8, 7, 30, 0, TimeSpan.Zero), occurrence);
    }

    [Fact]
    public void GetNextOccurrence_DuringFallBack_ReturnsBothRepeatedLocalMinutes()
    {
        var easternTime = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        var expression = CreateExpression("30", "1", "1", "NOV", "*");
        var beforeFirstOccurrence = new DateTimeOffset(2026, 11, 1, 5, 29, 0, TimeSpan.Zero);

        var firstOccurrence = calculator.GetNextOccurrence(expression, beforeFirstOccurrence, easternTime);
        var secondOccurrence = calculator.GetNextOccurrence(expression, firstOccurrence!.Value, easternTime);

        Assert.Equal(new DateTimeOffset(2026, 11, 1, 5, 30, 0, TimeSpan.Zero), firstOccurrence);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 6, 30, 0, TimeSpan.Zero), secondOccurrence);
    }

    private static CronExpression CreateExpression(
        string minute,
        string hour,
        string dayOfMonth,
        string month,
        string dayOfWeek) =>
        new(
            Parse(CronFieldKind.Minute, minute),
            Parse(CronFieldKind.Hour, hour),
            Parse(CronFieldKind.DayOfMonth, dayOfMonth),
            Parse(CronFieldKind.Month, month),
            Parse(CronFieldKind.DayOfWeek, dayOfWeek));

    private static CronField Parse(CronFieldKind kind, string expression)
    {
        Assert.True(CronField.TryParse(kind, expression, out var field, out var error), error);
        return field!;
    }
}
