namespace WinCron.CommandLine;

public static class WinCronCommandLineParser
{
    public static WinCronCommandLineParseResult Parse(IReadOnlyList<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var errors = new List<string>();
        var mode = WinCronCommandMode.Run;
        var hasExplicitMode = false;
        string? configurationPath = null;

        for (var argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
        {
            var argument = arguments[argumentIndex];

            if (argument == "--config")
            {
                if (argumentIndex + 1 >= arguments.Count
                    || arguments[argumentIndex + 1].StartsWith('-'))
                {
                    errors.Add("Option '--config' requires a file path.");
                    continue;
                }

                SetConfigurationPath(arguments[++argumentIndex], ref configurationPath, errors);
                continue;
            }

            if (argument.StartsWith("--config=", StringComparison.Ordinal))
            {
                SetConfigurationPath(argument["--config=".Length..], ref configurationPath, errors);
                continue;
            }

            var requestedMode = argument switch
            {
                "-T" or "--test" => WinCronCommandMode.Test,
                "-l" or "--list" => WinCronCommandMode.List,
                "-V" or "--version" => WinCronCommandMode.Version,
                "-h" or "--help" => WinCronCommandMode.Help,
                _ => (WinCronCommandMode?)null
            };

            if (requestedMode is null)
            {
                errors.Add($"Unknown argument '{argument}'.");
                continue;
            }

            if (hasExplicitMode)
            {
                errors.Add("Only one of --test, --list, --version, or --help can be specified.");
                continue;
            }

            mode = requestedMode.Value;
            hasExplicitMode = true;
        }

        if (configurationPath is not null
            && mode is WinCronCommandMode.Help or WinCronCommandMode.Version)
        {
            errors.Add("Option '--config' can only be used when running, testing, or listing a configuration.");
        }

        var options = errors.Count == 0
            ? new WinCronCommandLineOptions(mode, configurationPath)
            : null;

        return new WinCronCommandLineParseResult(options, errors.AsReadOnly());
    }

    private static void SetConfigurationPath(
        string value,
        ref string? configurationPath,
        List<string> errors)
    {
        if (configurationPath is not null)
        {
            errors.Add("Option '--config' can only be specified once.");
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add("Option '--config' requires a non-empty file path.");
            return;
        }

        configurationPath = value;
    }
}
