using System.Text.Json;
using WinCron.Execution;

namespace WinCron.Tests.Execution;

public sealed class JsonFileJobExecutionLoggerTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"wincron-log-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task WriteAsync_AppendsOneStructuredJsonRecordPerRun()
    {
        using var logger = new JsonFileJobExecutionLogger(temporaryDirectory);
        var startedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var result = new JobExecutionResult(
            "line-1",
            "echo test",
            startedAt,
            startedAt,
            startedAt.AddSeconds(2),
            0,
            "output",
            string.Empty,
            false,
            null);

        await logger.WriteAsync(result, TestContext.Current.CancellationToken);

        var line = Assert.Single(await File.ReadAllLinesAsync(
            logger.LogFilePath,
            TestContext.Current.CancellationToken));
        using var document = JsonDocument.Parse(line);
        Assert.Equal("line-1", document.RootElement.GetProperty("jobId").GetString());
        Assert.Equal(0, document.RootElement.GetProperty("exitCode").GetInt32());
        Assert.Equal("00:00:02", document.RootElement.GetProperty("duration").GetString());
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
