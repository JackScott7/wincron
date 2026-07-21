namespace WinCron.Configuration;

public interface ICronConfigurationWatcher : IDisposable
{
    Task WaitForChangeAsync(CancellationToken cancellationToken);
}
