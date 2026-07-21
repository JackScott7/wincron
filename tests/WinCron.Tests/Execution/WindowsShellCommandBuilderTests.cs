using WinCron.Domain;
using WinCron.Execution;

namespace WinCron.Tests.Execution;

public sealed class WindowsShellCommandBuilderTests
{
    [Fact]
    public void CreateStartInfo_ConfiguresShellEnvironmentWorkingDirectoryAndExactCommand()
    {
        const string command = "echo first  second & echo done";
        var job = CreateJob(command, new Dictionary<string, string> { ["WINCRON_VALUE"] = "42" }, @"C:\jobs");
        var builder = new WindowsShellCommandBuilder(@"C:\Windows\System32\cmd.exe");

        var startInfo = builder.CreateStartInfo(job);

        Assert.Equal(@"C:\Windows\System32\cmd.exe", startInfo.FileName);
        Assert.Equal(@"C:\jobs", startInfo.WorkingDirectory);
        Assert.False(startInfo.UseShellExecute);
        Assert.True(startInfo.RedirectStandardOutput);
        Assert.True(startInfo.RedirectStandardError);
        Assert.Equal(["/D", "/S", "/C", command], startInfo.ArgumentList);
        Assert.Equal("42", startInfo.Environment["WINCRON_VALUE"]);
    }

    private static CronJobDefinition CreateJob(
        string command,
        IReadOnlyDictionary<string, string> environment,
        string workingDirectory)
    {
        var expression = new CronExpression(
            Parse(CronFieldKind.Minute),
            Parse(CronFieldKind.Hour),
            Parse(CronFieldKind.DayOfMonth),
            Parse(CronFieldKind.Month),
            Parse(CronFieldKind.DayOfWeek));
        return new CronJobDefinition(expression, command, environment, workingDirectory, 1);
    }

    private static CronField Parse(CronFieldKind kind)
    {
        Assert.True(CronField.TryParse(kind, "*", out var field, out var error), error);
        return field!;
    }
}
