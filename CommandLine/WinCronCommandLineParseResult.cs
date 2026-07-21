namespace WinCron.CommandLine;

public sealed class WinCronCommandLineParseResult
{
    public WinCronCommandLineParseResult(
        WinCronCommandLineOptions? options,
        IReadOnlyList<string> errors)
    {
        Options = options;
        Errors = errors;
    }

    public WinCronCommandLineOptions? Options { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Options is not null && Errors.Count == 0;
}
