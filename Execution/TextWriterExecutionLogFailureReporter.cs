namespace WinCron.Execution;

public sealed class TextWriterExecutionLogFailureReporter(TextWriter writer) : IExecutionLogFailureReporter
{
    private readonly TextWriter writer = writer ?? throw new ArgumentNullException(nameof(writer));

    public Task ReportAsync(Exception exception, CancellationToken cancellationToken) =>
        writer.WriteLineAsync($"WinCron logging error: {exception.Message}");
}
