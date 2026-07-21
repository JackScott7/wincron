using System.Text;
using Microsoft.Extensions.Logging;

namespace WinCron.Hosting;

public sealed class LoggerTextWriter(ILogger logger, LogLevel logLevel) : TextWriter
{
    private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly Action<ILogger, string, Exception?> writeLog = LoggerMessage.Define<string>(
        logLevel,
        new EventId(1000, "WinCronOutput"),
        "{WinCronMessage}");

    public override Encoding Encoding => Encoding.UTF8;

    public override Task WriteLineAsync(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            writeLog(logger, value, null);
        }

        return Task.CompletedTask;
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return WriteLineAsync(new string(buffer, index, count));
    }

    public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default) =>
        WriteLineAsync(buffer.ToString());
}
