namespace WinCron.CommandLine;

public static class WinCronUsage
{
    public const string Text = """
        Usage:
          wincron [--config <path>]
          wincron (-T | --test) [--config <path>]
          wincron (-l | --list) [--config <path>]
          wincron (-V | --version)
          wincron (-h | --help)

        Options:
          --config <path>  Use a configuration file instead of the default
                           %USERPROFILE%\wincron\config.wc.
          -T, --test       Validate the selected configuration and exit.
          -l, --list       Print the selected configuration and exit.
          -V, --version    Print the WinCron version and exit.
          -h, --help       Print this help and exit.
        """;
}
