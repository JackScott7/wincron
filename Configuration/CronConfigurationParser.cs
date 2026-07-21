using System.Text.RegularExpressions;
using WinCron.Domain;

namespace WinCron.Configuration;

public sealed partial class CronConfigurationParser
{
    public const string WorkingDirectoryVariableName = "WINCRON_WORKING_DIRECTORY";

    private static readonly CronFieldKind[] FieldKinds =
    [
        CronFieldKind.Minute,
        CronFieldKind.Hour,
        CronFieldKind.DayOfMonth,
        CronFieldKind.Month,
        CronFieldKind.DayOfWeek
    ];

    private readonly string defaultWorkingDirectory;

    public CronConfigurationParser(string? defaultWorkingDirectory = null)
    {
        this.defaultWorkingDirectory = defaultWorkingDirectory
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(this.defaultWorkingDirectory))
        {
            this.defaultWorkingDirectory = AppContext.BaseDirectory;
        }
    }

    public CronConfigurationParseResult Parse(string configurationText)
    {
        ArgumentNullException.ThrowIfNull(configurationText);

        var jobs = new List<CronJobDefinition>();
        var errors = new List<CronParseError>();
        var environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = configurationText.ReplaceLineEndings("\n").Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var lineNumber = lineIndex + 1;
            var line = lines[lineIndex];
            var trimmedLine = line.Trim();

            if (trimmedLine.Length == 0 || trimmedLine.StartsWith('#'))
            {
                continue;
            }

            var environmentMatch = EnvironmentAssignmentRegex().Match(line);
            if (environmentMatch.Success)
            {
                environmentVariables[environmentMatch.Groups["key"].Value] =
                    environmentMatch.Groups["value"].Value;
                continue;
            }

            ParseJobLine(line, lineNumber, environmentVariables, jobs, errors);
        }

        return new CronConfigurationParseResult(new CronConfiguration(jobs.AsReadOnly()), errors.AsReadOnly());
    }

    private void ParseJobLine(
        string line,
        int lineNumber,
        Dictionary<string, string> environmentVariables,
        List<CronJobDefinition> jobs,
        List<CronParseError> errors)
    {
        var fieldMatches = NonWhitespaceTokenRegex().Matches(line);
        if (fieldMatches.Count < 6)
        {
            errors.Add(new CronParseError(
                lineNumber,
                "Expected five schedule fields followed by a command.",
                line));
            return;
        }

        var fields = new CronField[FieldKinds.Length];
        for (var fieldIndex = 0; fieldIndex < FieldKinds.Length; fieldIndex++)
        {
            if (!CronField.TryParse(
                    FieldKinds[fieldIndex],
                    fieldMatches[fieldIndex].Value,
                    out var field,
                    out var error))
            {
                errors.Add(new CronParseError(lineNumber, error!, line));
                return;
            }

            fields[fieldIndex] = field!;
        }

        var commandStartIndex = fieldMatches[5].Index;
        var commandText = line[commandStartIndex..];
        if (string.IsNullOrWhiteSpace(commandText))
        {
            errors.Add(new CronParseError(lineNumber, "The command cannot be empty.", line));
            return;
        }

        var schedule = new CronExpression(fields[0], fields[1], fields[2], fields[3], fields[4]);
        if (!schedule.HasPossibleOccurrence())
        {
            errors.Add(new CronParseError(
                lineNumber,
                "The schedule can never produce a calendar occurrence.",
                line));
            return;
        }

        var workingDirectory = environmentVariables.TryGetValue(WorkingDirectoryVariableName, out var configuredDirectory)
            && !string.IsNullOrWhiteSpace(configuredDirectory)
                ? Environment.ExpandEnvironmentVariables(configuredDirectory)
                : defaultWorkingDirectory;

        jobs.Add(new CronJobDefinition(
            schedule,
            commandText,
            environmentVariables,
            workingDirectory,
            lineNumber));
    }

    [GeneratedRegex(@"^\s*(?<key>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*(?<value>.*)$", RegexOptions.CultureInvariant)]
    private static partial Regex EnvironmentAssignmentRegex();

    [GeneratedRegex(@"\S+", RegexOptions.CultureInvariant)]
    private static partial Regex NonWhitespaceTokenRegex();
}
