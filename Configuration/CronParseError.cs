namespace WinCron.Configuration;

public sealed record CronParseError(int LineNumber, string Message, string LineText)
{
    public override string ToString() => $"Line {LineNumber}: {Message}";
}
