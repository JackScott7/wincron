namespace WinCron.CommandLine;

public sealed record WinCronCommandLineOptions(
    WinCronCommandMode Mode,
    string? ConfigurationPath);
