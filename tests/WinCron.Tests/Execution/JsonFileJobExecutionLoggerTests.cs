using System.Text.Json;
using WinCron.Execution;

namespace WinCron.Tests.Execution;

public sealed class JsonFileJobExecutionLoggerTests : IDisposable
{
    private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath(), $"wincron-log-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task WriteAsyncAppendsOneStructuredJsonRecordPerRun()
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

    [Fact]
    public async Task WriteAsyncRotatesLogsAtConfiguredSizeAndRetainsCompleteRecords()
    {
        using var logger = new JsonFileJobExecutionLogger(
            temporaryDirectory,
            new JsonFileJobExecutionLoggerOptions
            {
                MaximumFileSizeBytes = 1,
                RetainedFileCount = 2
            });
        var timestamp = DateTimeOffset.UtcNow;
        var result = new JobExecutionResult(
            "line-1",
            "echo test",
            timestamp,
            timestamp,
            timestamp,
            0,
            "output",
            string.Empty,
            false,
            null);

        await logger.WriteAsync(result, TestContext.Current.CancellationToken);
        await logger.WriteAsync(result, TestContext.Current.CancellationToken);
        await logger.WriteAsync(result, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(temporaryDirectory, "runs.1.jsonl")));
        Assert.True(File.Exists(Path.Combine(temporaryDirectory, "runs.2.jsonl")));
        Assert.Single(await File.ReadAllLinesAsync(logger.LogFilePath, TestContext.Current.CancellationToken));
        Assert.Single(await File.ReadAllLinesAsync(
            Path.Combine(temporaryDirectory, "runs.1.jsonl"),
            TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(temporaryDirectory, recursive: true);
        }
    }
}
