using WinCron.Domain;

namespace WinCron.Tests.Domain;

public sealed class CronExpressionTests
{
    [Fact]
    public void MatchesUsesClassicCronOrSemanticsWhenBothDayFieldsAreRestricted()
    {
        var expression = CreateExpression("0", "12", "15", "*", "MON");

        Assert.True(expression.Matches(new DateTime(2026, 6, 15, 12, 0, 0)));
        Assert.True(expression.Matches(new DateTime(2026, 7, 15, 12, 0, 0)));
        Assert.False(expression.Matches(new DateTime(2026, 7, 14, 12, 0, 0)));
    }

    [Fact]
    public void ConstructorThrowsArgumentExceptionWhenFieldIsInWrongPosition()
    {
        var minute = Parse(CronFieldKind.Minute, "*");
        var hour = Parse(CronFieldKind.Hour, "*");
        var dayOfMonth = Parse(CronFieldKind.DayOfMonth, "*");
        var month = Parse(CronFieldKind.Month, "*");

        Assert.Throws<ArgumentException>(() =>
            new CronExpression(hour, minute, dayOfMonth, month, Parse(CronFieldKind.DayOfWeek, "*")));
    }

    [Fact]
    public void MatchesRequiresRestrictedDayOfWeekWhenDayOfMonthUsesWildcardStep()
    {
        var expression = CreateExpression("0", "12", "*/1", "*", "MON");

        Assert.True(expression.Matches(new DateTime(2026, 6, 15, 12, 0, 0)));
        Assert.False(expression.Matches(new DateTime(2026, 6, 16, 12, 0, 0)));
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

    private static CronField Parse(CronFieldKind kind, string value)
    {
        Assert.True(CronField.TryParse(kind, value, out var field, out var error), error);
        return field!;
    }
}
