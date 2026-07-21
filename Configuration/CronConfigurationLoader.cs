namespace WinCron.Configuration;

public sealed class CronConfigurationLoader
{
    private readonly string? legacyConfigurationPath;

    public CronConfigurationLoader(
        string? configurationPath = null,
        string? legacyConfigurationPath = null)
    {
        var userProfileDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        ConfigurationPath = configurationPath ?? Path.Combine(
            userProfileDirectory,
            "wincron",
            "config.wc");

        this.legacyConfigurationPath = legacyConfigurationPath
            ?? (configurationPath is null ? Path.Combine(userProfileDirectory, "config.wc") : null);
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
            if (legacyConfigurationPath is not null && File.Exists(legacyConfigurationPath))
            {
                File.Copy(legacyConfigurationPath, ConfigurationPath);
            }
            else
            {
                await File.WriteAllTextAsync(ConfigurationPath, string.Empty, cancellationToken);
            }
        }

        return await File.ReadAllTextAsync(ConfigurationPath, cancellationToken);
    }

    public Task<string> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(ConfigurationPath))
        {
            throw new FileNotFoundException(
                $"The WinCron configuration file does not exist: {ConfigurationPath}",
                ConfigurationPath);
        }

        return File.ReadAllTextAsync(ConfigurationPath, cancellationToken);
    }
}
