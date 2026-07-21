using WinCron.Domain;

namespace WinCron.Tests.Domain;

public sealed class CronFieldTests
{
    [Theory]
    [InlineData(CronFieldKind.Minute, "*/15", 0, true)]
    [InlineData(CronFieldKind.Minute, "*/15", 15, true)]
    [InlineData(CronFieldKind.Minute, "*/15", 16, false)]
    [InlineData(CronFieldKind.Hour, "1-10/2", 7, true)]
    [InlineData(CronFieldKind.Hour, "1-10/2", 8, false)]
    [InlineData(CronFieldKind.Month, "JAN,MAR-APR", 4, true)]
    [InlineData(CronFieldKind.DayOfWeek, "SUN,MON-FRI", 6, false)]
    [InlineData(CronFieldKind.DayOfWeek, "7", 0, true)]
    public void TryParseMatchesExpectedValueForValidExpression(
        CronFieldKind kind,
        string expression,
        int value,
        bool expectedMatch)
    {
        var parsed = CronField.TryParse(kind, expression, out var field, out var error);

        Assert.True(parsed, error);
        Assert.NotNull(field);
        Assert.Equal(expectedMatch, field.Matches(value));
    }

    [Theory]
    [InlineData(CronFieldKind.Minute, "60")]
    [InlineData(CronFieldKind.Hour, "24")]
    [InlineData(CronFieldKind.DayOfMonth, "0")]
    [InlineData(CronFieldKind.Month, "XYZ")]
    [InlineData(CronFieldKind.DayOfWeek, "8")]
    [InlineData(CronFieldKind.Minute, "10-1")]
    [InlineData(CronFieldKind.Minute, "*/0")]
    [InlineData(CronFieldKind.Minute, "1,,2")]
    [InlineData(CronFieldKind.Minute, "5/2")]
    public void TryParseReturnsValidationErrorForInvalidExpression(CronFieldKind kind, string expression)
    {
        var parsed = CronField.TryParse(kind, expression, out var field, out var error);

        Assert.False(parsed);
        Assert.Null(field);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
