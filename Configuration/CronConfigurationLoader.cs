namespace WinCron.Configuration;

public sealed class CronConfigurationLoader
{
    public CronConfigurationLoader(string? configurationPath = null)
    {
        ConfigurationPath = configurationPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "config.wc");
    }

    public string ConfigurationPath { get; }

    public async Task<string> LoadAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(ConfigurationPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(ConfigurationPath))
        {
            await File.WriteAllTextAsync(ConfigurationPath, string.Empty, cancellationToken);
        }

        return await File.ReadAllTextAsync(ConfigurationPath, cancellationToken);
    }
}
