using System.Globalization;

namespace WinCron.Execution;

public sealed class ConsoleJobExecutionLogger : IJobExecutionLogger, IDisposable
{
    private readonly TextWriter standardOutputWriter;
    private readonly TextWriter standardErrorWriter;
    private readonly TimeZoneInfo displayTimeZone;
    private readonly bool includeCapturedOutput;
    private readonly SemaphoreSlim writeLock = new(1, 1);
    private bool isDisposed;

    public ConsoleJobExecutionLogger(
        TextWriter? standardOutputWriter = null,
        TextWriter? standardErrorWriter = null,
        TimeZoneInfo? displayTimeZone = null,
        bool includeCapturedOutput = true)
    {
        this.standardOutputWriter = standardOutputWriter ?? Console.Out;
        this.standardErrorWriter = standardErrorWriter ?? Console.Error;
        this.displayTimeZone = displayTimeZone ?? TimeZoneInfo.Local;
        this.includeCapturedOutput = includeCapturedOutput;
    }

    public async Task WriteAsync(JobExecutionResult result, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(isDisposed, this);
        ArgumentNullException.ThrowIfNull(result);

        await writeLock.WaitAsync(cancellationToken);
        try
        {
            var completedAtLocal = TimeZoneInfo.ConvertTime(result.CompletedAtUtc, displayTimeZone);
            var outcome = GetOutcome(result);
            var durationMilliseconds = result.Duration.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture);

            await standardOutputWriter.WriteLineAsync(
                $"[{completedAtLocal:yyyy-MM-dd HH:mm:ss zzz}] {result.JobId} {outcome} in {durationMilliseconds} ms: {result.CommandText}");

            if (includeCapturedOutput && !string.IsNullOrEmpty(result.StandardOutput))
            {
                await standardOutputWriter.WriteAsync(result.StandardOutput);
                await WriteTrailingNewLineIfNeededAsync(standardOutputWriter, result.StandardOutput);
            }

            if (includeCapturedOutput && !string.IsNullOrEmpty(result.StandardError))
            {
                await standardErrorWriter.WriteAsync(result.StandardError);
                await WriteTrailingNewLineIfNeededAsync(standardErrorWriter, result.StandardError);
            }

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                await standardErrorWriter.WriteLineAsync($"WinCron execution error: {result.ErrorMessage}");
            }
        }
        finally
        {
            writeLock.Release();
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        writeLock.Dispose();
        isDisposed = true;
    }

    private static string GetOutcome(JobExecutionResult result)
    {
        if (result.WasCanceled)
        {
            return "was canceled";
        }

        if (result.TimedOut)
        {
            return "timed out";
        }

        if (result.ErrorMessage is not null)
        {
            return "failed to start";
        }

        return $"exited with code {result.ExitCode}";
    }

    private static Task WriteTrailingNewLineIfNeededAsync(TextWriter writer, string text) =>
        text.EndsWith('\n') ? Task.CompletedTask : writer.WriteLineAsync();
}
