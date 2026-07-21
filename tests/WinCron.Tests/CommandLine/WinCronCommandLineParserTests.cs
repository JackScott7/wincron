using WinCron.CommandLine;

namespace WinCron.Tests.CommandLine;

public sealed class WinCronCommandLineParserTests
{
    [Fact]
    public void ParseUsesRunModeAndDefaultConfigurationWhenArgumentsAreEmpty()
    {
        var result = WinCronCommandLineParser.Parse([]);

        Assert.True(result.IsValid);
        Assert.Equal(WinCronCommandMode.Run, result.Options!.Mode);
        Assert.Null(result.Options.ConfigurationPath);
    }

    [Theory]
    [InlineData("-T", WinCronCommandMode.Test)]
    [InlineData("--test", WinCronCommandMode.Test)]
    [InlineData("-l", WinCronCommandMode.List)]
    [InlineData("--list", WinCronCommandMode.List)]
    [InlineData("-V", WinCronCommandMode.Version)]
    [InlineData("--version", WinCronCommandMode.Version)]
    [InlineData("-h", WinCronCommandMode.Help)]
    [InlineData("--help", WinCronCommandMode.Help)]
    public void ParseRecognizesEverySupportedMode(string argument, WinCronCommandMode expectedMode)
    {
        var result = WinCronCommandLineParser.Parse([argument]);

        Assert.True(result.IsValid);
        Assert.Equal(expectedMode, result.Options!.Mode);
    }

    [Theory]
    [InlineData("--config", @"C:\jobs\custom.wc")]
    [InlineData(@"--config=C:\jobs\custom.wc", null)]
    public void ParseAcceptsConfigurationPathInBothSupportedForms(string argument, string? separateValue)
    {
        var arguments = separateValue is null ? new[] { argument } : new[] { argument, separateValue };

        var result = WinCronCommandLineParser.Parse(arguments);

        Assert.True(result.IsValid);
        Assert.Equal(@"C:\jobs\custom.wc", result.Options!.ConfigurationPath);
    }

    [Fact]
    public void ParseAllowsConfigurationPathWithTestAndListModes()
    {
        var testResult = WinCronCommandLineParser.Parse(["--config", "test.wc", "--test"]);
        var listResult = WinCronCommandLineParser.Parse(["--list", "--config=list.wc"]);

        Assert.Equal(WinCronCommandMode.Test, testResult.Options!.Mode);
        Assert.Equal("test.wc", testResult.Options.ConfigurationPath);
        Assert.Equal(WinCronCommandMode.List, listResult.Options!.Mode);
        Assert.Equal("list.wc", listResult.Options.ConfigurationPath);
    }

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--config")]
    [InlineData("--config=")]
    public void ParseRejectsUnknownOrIncompleteArguments(string argument)
    {
        var result = WinCronCommandLineParser.Parse([argument]);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ParseRejectsMultipleModes()
    {
        var result = WinCronCommandLineParser.Parse(["--test", "--list"]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Only one", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseRejectsDuplicateConfigurationOptions()
    {
        var result = WinCronCommandLineParser.Parse(["--config", "first.wc", "--config=second.wc"]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("only be specified once", StringComparison.Ordinal));
    }

    [Fact]
    public void ParseDoesNotConsumeAnotherOptionAsConfigurationPath()
    {
        var result = WinCronCommandLineParser.Parse(["--config", "--test"]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("requires a file path", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--version")]
    public void ParseRejectsConfigurationPathForModesThatDoNotReadConfiguration(string mode)
    {
        var result = WinCronCommandLineParser.Parse([mode, "--config", "unused.wc"]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("can only be used", StringComparison.Ordinal));
    }
}
