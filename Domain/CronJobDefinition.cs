using System.Collections.ObjectModel;

namespace WinCron.Domain;

public sealed class CronJobDefinition
{
    public CronJobDefinition(
        CronExpression schedule,
        string commandText,
        IReadOnlyDictionary<string, string> environmentVariables,
        string workingDirectory,
        int sourceLineNumber)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandText);
        ArgumentNullException.ThrowIfNull(environmentVariables);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        if (sourceLineNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceLineNumber), "The source line number must be positive.");
        }

        Schedule = schedule;
        CommandText = commandText;
        EnvironmentVariables = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(environmentVariables, StringComparer.OrdinalIgnoreCase));
        WorkingDirectory = workingDirectory;
        SourceLineNumber = sourceLineNumber;
        Id = $"line-{sourceLineNumber}";
    }

    public string Id { get; }

    public CronExpression Schedule { get; }

    public string CommandText { get; }

    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; }

    public string WorkingDirectory { get; }

    public int SourceLineNumber { get; }

    public override string ToString() => $"{Schedule} {CommandText}";
}
